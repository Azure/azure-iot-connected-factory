<#
.SYNOPSIS
    .
.DESCRIPTION
    Removes the public IP address from the VM running the simulation.
.PARAMETER DeploymentName
    The name of the deployment to get the logs from. This is the resource group name.
.NOTES
    .
#>
[CmdletBinding()]
Param(
[Parameter(Position=0, Mandatory=$false, HelpMessage="Specify the name of the deployment (this is the name used as the name for the VM and the resource group)")]
[string] $DeploymentName
)

# Set variables
if ([string]::IsNullOrEmpty($script:DeploymentName))
{
    $script:DeploymentName = $script:SuiteName = $env:USERNAME + "ConnfactoryLocal";
}
$script:VmName = $DeploymentName
$script:ResourceGroupName = $DeploymentName

# Find VM 
try 
{
    $vmResource = Get-AzureRmResource -ResourceName $script:VmName -ResourceType Microsoft.Compute/virtualMachines -ResourceGroupName $script:ResourceGroupName
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Found VM with name '{0}' in resource group '{1}'" -f $script:VmName, $script:ResourceGroupName)
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

try
{
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Get the public IP address.")
    $vmPublicIp = Get-AzureRmPublicIpAddress -Name $script:VmName -ResourceGroupName $script:ResourceGroupName -ErrorAction SilentlyContinue
}
catch {}

if ($vmPublicIp -eq $null)
{
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - The VM '{0}' does not have a public IP address." -f $script:VmName)
    exit
}

# Remove the SSH allow rule from the network security group.
Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Remove the SSH allow rule from the network security group.")
$currentNsg = Get-AzureRmNetworkSecurityGroup -ResourceGroupName $script:ResourceGroupName -Name $script:ResourceGroupName
Remove-AzureRmNetworkSecurityRuleConfig -Name AllowSshInBound -NetworkSecurityGroup $currentNsg | Out-Null
Set-AzureRmNetworkSecurityGroup -NetworkSecurityGroup $currentNsg | Out-Null
# Disable SSH access to the VM
Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Disable SSH access to the VM.")
. "$(Split-Path $MyInvocation.MyCommand.Path)/Disable-SimulationSshAccess.ps1" $script:DeploymentName

# Remove the public IP address from the VM if we added it.
Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Remove the public IP address from network interface.")
$vmNetworkInterface = Get-AzureRmNetworkInterface -Name $script:VmName -ResourceGroupName $script:ResourceGroupName
$vmNetworkInterface.IpConfigurations[0].PublicIpAddress = $null
Set-AzureRmNetworkInterface -NetworkInterface $vmNetworkInterface | Out-Null
Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Delete the public IP address.")
Remove-AzureRmPublicIpAddress -Name $script:VmName -ResourceGroupName $script:ResourceGroupName -Force | Out-Null
