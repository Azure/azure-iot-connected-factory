<#
.SYNOPSIS
    .
.DESCRIPTION
    .
.PARAMETER DeploymentName
    The name of the deployment to get the logs from. This is the resource group name.
.PARAMETER VmAdminUsername
    The username used to connect to the VM.
.PARAMETER VmAdminPassword
    The password of the user used to connect to the VM.
.EXAMPLE
    ./Get-SimulationLogs.ps1 mydeployment
    Downloads the logs from the VM of the deployment mydeployment to a subdirectory Logs of the current folder.
.NOTES
    .
#>
[CmdletBinding()]
Param(
[Parameter(Position=0, Mandatory=$false, HelpMessage="Specify the name of the deployment (this is the name used as the name for the VM and the resource group)")]
[string] $DeploymentName,
[Parameter(Position=1, Mandatory=$false, HelpMessage="Specify the name of the user in the VM.")]
[string] $VmAdminUsername="docker",
[Parameter(Position=2, Mandatory=$false, HelpMessage="Specify the password for the user.")]
[string] $VmAdminPassword
)

Function GetEnvSetting()
{
    Param(
        [Parameter(Mandatory=$true,Position=0)] [string] $settingName,
        [Parameter(Mandatory=$false,Position=1)] [switch] $errorOnNull = $true
    )

    $setting = $DeploymentSettingsXml.Environment.SelectSingleNode("//setting[@name = '$settingName']")

    if ($setting -eq $null)
    {
        if ($errorOnNull)
        {
            Write-Error -Category ObjectNotFound -Message ("$(Get-Date –f $TIME_STAMP_FORMAT) - Cannot locate setting '{0}' in deployment settings file {1}." -f $settingName, $script:DeploymentSettingsFile)
            throw ("Cannot locate setting '{0}' in deployment settings file {1}." -f $settingName, $script:DeploymentSettingsFile)
        }
    }
    return $setting.value
}


################################################################################################################################################################
#
# Start of script
#
################################################################################################################################################################

$VerbosePreference = "Continue"

# Local path
$LocalPath = ((Get-Location).Path) + "\Logs"
Remove-Item -Path $LocalPath -Force -Recurse -ErrorAction SilentlyContinue | Out-Null
New-Item -Path $LocalPath -ItemType Directory -ErrorAction SilentlyContinue | Out-Null

# Timeout for SSH operations
$ConnectionTimeout = 30000
$SshTimeout = 600

# Set variables
if ([string]::IsNullOrEmpty($script:DeploymentName))
{
    $script:DeploymentName = $script:SuiteName = $env:USERNAME + "ConnfactoryLocal";
}
$script:LocalDeployment = $false
if ($script:DeploymentName -match ($env:USERNAME + "ConnfactoryLocal"))
{
    $script:LocalDeployment = $true
}
$script:VmName = $DeploymentName
$script:ResourceGroupName = $DeploymentName
$script:SimulationFactoryPath = (Split-Path $MyInvocation.MyCommand.Path)

# Read the stored docker password.
if ([string]::IsNullOrEmpty($script:VmAdminPassword))
{
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - No VM password specified on command line. Trying to read from config.user file.")
    $script:IoTSuiteRootPath = $script:SimulationFactoryPath + "/../.."
    if ($script:LocalDeployment)
    {
        $script:DeploymentSettingsFile = "{0}/local.config.user" -f $script:IoTSuiteRootPath
    }
    else
    {
        $script:DeploymentSettingsFile = "{0}/{1}.config.user" -f $script:IoTSuiteRootPath, $script:DeploymentName
    }
    $script:DeploymentSettingsXml = [xml](Get-Content "$script:DeploymentSettingsFile")
    $script:VmAdminPassword = GetEnvSetting "VmAdminPassword"
}

# Find VM 
try 
{
    Get-AzureRmResource -ResourceName $script:VmName -ResourceType Microsoft.Compute/virtualMachines -ResourceGroupName $script:ResourceGroupName | Out-Null
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Found VM with name '{0}' in resource group '{1}'" -f $script:VmName, $script:ResourceGroupName)
}
catch 
{
    # Ensure user is logged in.
    try
    {
        $context = Get-AzureRmContext
        if ($context.Environment -eq $null)
        {
            throw;
        }
    }
    catch 
    {
        Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Please log in to Azure with Login-AzureRmAccount.")
        exit
    }
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Please make the resource group '{0}' exists and there is a VM with name '{1}' in this group." -f $script:ResourceGroupName, $script:VmName)
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Are you sure your current Azure environment and subscription are correct?")
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Please use Set-AzureRmEnvironment/Select-AzureRmSubscription to set thees. Otherwise make sure your cloud deployment worked without issues.")
    exit
}

$script:RemovePublicIp = $false
try
{
    $script:VmPublicIp = Get-AzureRmPublicIpAddress -Name $script:VmName -ResourceGroupName $script:ResourceGroupName -ErrorAction SilentlyContinue
}
catch {}

if ($script:VmPublicIp -eq $null)
{
    # Add a public IP address to the VM
    Invoke-Expression "$script:SimulationFactoryPath/Add-SimulationPublicIp.ps1 -DeploymentName $script:DeploymentName"
    $script:RemovePublicIp = $true
}

try
{
    # Enable SSH access to VM.
    Invoke-Expression "$script:SimulationFactoryPath/Enable-SimulationSshAccess.ps1 $script:DeploymentName"

    # Get IP address
    $script:VmPublicIp = Get-AzureRmPublicIpAddress -ResourceGroupName $DeploymentName
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - IP address of the VM is '{0}'" -f $script:VmPublicIp.IpAddress)

    # Create a PSCredential object for SSH
    $securePassword = ConvertTo-SecureString $VmAdminPassword -AsPlainText -Force
    $sshCredentials = New-Object System.Management.Automation.PSCredential ($VmAdminUsername, $securePassword)

    # Create SSH session
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Create SSH session to VM with IP address '{0}'" -f $script:VmPublicIp.IpAddress)
    $session = New-SSHSession $script:VmPublicIp.IpAddress -Credential $sshCredentials -AcceptKey -ConnectionTimeout $ConnectionTimeout
    if ($session -eq $null)
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Cannot create SSH session to VM '{0}'" -f  $script:VmPublicIp.IpAddress)
        throw ("Cannot create SSH session to VM '{0}'" -f  $script:VmPublicIp.IpAddress)
    }

    # Delete the old archive.
    $remoteFile = "Logs.tar.bz2"
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Delete the old archive '$remoteFile'")
    $vmCommand = "rm -f $sourceArchiveName"
    Invoke-SSHCommand -Sessionid $session.SessionId -TimeOut $script:SshTimeout -Command $vmCommand | Out-Null

    # Compress the simulation logs in the VM.
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Compress the log files")
    $vmCommand = "tar -cjvf Logs.tar.bz2 Logs *.log"
    $status = Invoke-SSHCommand -Sessionid $session.SessionId -TimeOut $script:SshTimeout -Command $vmCommand -ErrorAction SilentlyContinue

    # Copy the logs archive.
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Download the log archive")
    $localFile = "$LocalPath/Logs_" + (Get-Date  -Format FileDateTimeUniversal) + ".tar.bz2"
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Download Logs (in bzip2 format) from VM as $script:localFile")
    Get-SCPFile -RemoteFile "/home/docker/$remoteFile" -LocalFile $localFile  -ComputerName $script:VmPublicIp.IpAddress -Credential $sshCredentials -ConnectionTimeout $ConnectionTimeout
}
finally
{
    # Disable SSH access to the VM
    Invoke-Expression "$script:SimulationFactoryPath/Disable-SimulationSshAccess.ps1 $script:DeploymentName"

    # Remove the public IP address from the VM if we added it.
    if ($script:RemovePublicIp -eq $true)
    {
        Invoke-Expression "$script:SimulationFactoryPath/Remove-SimulationPublicIp.ps1 -DeploymentName $script:DeploymentName"
    }

    # Remove SSH session
    if ($session)
    {
        Remove-SSHSession $session.SessionId | Out-Null
    }
}