<#
.SYNOPSIS
    .
.DESCRIPTION
    Adds the SSH allow rule from the network security group in the deployment.
.PARAMETER DeploymentName
    The name of the deployment to get the logs from. This is the resource group name.
.EXAMPLE
    ./Enable-SimulationSshAccess.ps1 mydeployment
    Enables SSH access to the simulation VM.
.NOTES
    .
#>
[CmdletBinding()]
Param(
[Parameter(Position=0, Mandatory=$true, HelpMessage="Specify the deployment name")]
[string] $DeploymentName
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
$script:ResourceGroupName = $DeploymentName

# Find the NSG
try 
{
    $currentNsg = Get-AzureRmNetworkSecurityGroup -ResourceGroupName $script:ResourceGroupName -Name $script:ResourceGroupName
    if ($currentNsg -eq $null)
    {
        throw
    }
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Found the network security group with name '{0}' in resource group '{1}'" -f $script:ResourceGroupName, $script:ResourceGroupName)
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
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Please make the resource group '{0}' exists." -f $script:ResourceGroupName)
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Are you sure your current Azure environment and subscription are correct?")
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Please use Set-AzureRmEnvironment/Select-AzureRmSubscription to set thees. Otherwise make sure your cloud deployment worked without issues.")
    exit
}

# Check if the AllowSshInBound rule already exists
$sshRule = $currentNsg.SecurityRules | Where-Object { $_.Name -eq "AllowSshInBound" }
if ($sshRule -eq $null)
{
    # Allow SSH inbound traffic
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Add the 'AllowSshInBound' rule to the network security group, which is allowing incoming network traffic on TCP port 22.")
    Add-AzureRmNetworkSecurityRuleConfig -Name AllowSshInBound -NetworkSecurityGroup $currentNsg -Protocol Tcp -SourcePortRange "*" -SourceAddressPrefix "*" -DestinationPortRange 22 -DestinationAddressPrefix "*" -Priority 100 -Direction Inbound -Access Allow | Out-Null
    Set-AzureRmNetworkSecurityGroup -NetworkSecurityGroup $currentNsg | Out-Null
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - SSH is now enabled, please use Disable-SimulationSshAccess.ps1 to disable it when you are done.")
}
else
{
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - SSH was already enabled. Please use Disable-SimulationSshAccess.ps1 to disable it when you are done.")
}
Write-Warning ("$(Get-Date –f $TIME_STAMP_FORMAT) - Your VM is now accessible via SSH. Please make sure:")
Write-Warning ("$(Get-Date –f $TIME_STAMP_FORMAT) - > you have the latest security fixes applied by following the instructions here: https://wiki.ubuntu.com/Security/Upgrades")
Write-Warning ("$(Get-Date –f $TIME_STAMP_FORMAT) - > you have set the network security groups inbound and outbound rules as restricted as possible")
Write-Warning ("$(Get-Date –f $TIME_STAMP_FORMAT) - > you have set a strong password to access the VM")