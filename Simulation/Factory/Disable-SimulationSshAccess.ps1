<#
.SYNOPSIS
    .
.DESCRIPTION
    Removes the SSH allow rule from the network security group in the deployment.
.PARAMETER DeploymentName
    The name of the deployment to get the logs from. This is the resource group name.
.EXAMPLE
    ./Disable-SimulationSshAccess.ps1 mydeployment
    Disables SSH access to the simulation VM.
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
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Found the network security group with name '{0}' in resource group '{1}'" -f $script:ResourceGroupName, $script:ResourceGroupName)
}
catch 
{
    # Ensure user is logged in.
    try
    {
        $context = Get-AzureRmContext
    }
    catch 
    {
        Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Please log in to Azure with Login-AzureRmAccount.")
        exit
    }
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Please make the resource group '{0}' exists and there is a VM with name '{1}' in this group." -f $script:ResourceGroupName, $script:VmName)
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Are you sure your current Azure environment and subscription are correct?")
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Please use Set-AzureRmEnvironment/Select-AzureRmSubscription to set thees. Otherwise make sure your cloud deployment worked without issues.")
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Can not update the simulation, because no VM with name '{0}' found in resource group '{1}'" -f $script:VmName, $script:ResourceGroupName)
    exit
}

$sshRule = $currentNsg.SecurityRules | Where-Object { $_.Name -eq "AllowSshInBound" }
if ($sshRule -eq $null)
{
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - SSH was already disabled.")
}
else
{
    # Remove the SSH allow rule from the network security group.
    Remove-AzureRmNetworkSecurityRuleConfig -Name AllowSshInBound -NetworkSecurityGroup $currentNsg | Out-Null
    Set-AzureRmNetworkSecurityGroup -NetworkSecurityGroup $currentNsg | Out-Null
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - SSH is now disabled.")
}