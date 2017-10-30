<#
.SYNOPSIS
    .
.DESCRIPTION
    Adds a public IP address to the VM the simulation is running in.
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
$script:VmName = $script:DeploymentName
$script:ResourceGroupName = $script:DeploymentName

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
catch 
{
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - The VM '{0}' does not have a public IP address." -f $script:VmName)
}

if ($vmPublicIp -eq $null)
{
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - The VM '{0}' does not have a public IP address. Adding one (will take a while)..." -f $script:VmName)
    # Add a public IP address to the VM
    try
    {
        # This might be inacurrate if there was a DNS name generated with random part.
        $dnsForPublicIpAddress = $script:ResourceGroupName.Substring(0, [System.Math]::Min(40, $resourceBaseName.Length)).ToLowerInvariant()
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Create a public IP address.")
        $vmPublicIp = New-AzureRmPublicIpAddress -Name $script:VmName -ResourceGroupName $script:ResourceGroupName -Location $vmResource.Location -AllocationMethod Dynamic -DomainNameLabel $dnsForPublicIpAddress
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Get the network interface.")
        $vmNetworkInterface = Get-AzureRmNetworkInterface -Name $script:VmName -ResourceGroupName $script:ResourceGroupName
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Set public IP address on network interface.")
        $vmNetworkInterface.IpConfigurations[0].PublicIpAddress = $vmPublicIp
        Set-AzureRmNetworkInterface -NetworkInterface $vmNetworkInterface | Out-Null
        $removePublicIp = $true
    }
    catch
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Unable to add a public IP address to VM '{0}' in resource group '{1}'" -f $script:VmName, $script:ResourceGroupName)
        throw ("Unable to add a public IP address to VM '{0}' in resource group '{1}'" -f $script:VmName, $script:ResourceGroupName)
    }
}

# Enable SSH access to the VM
Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Enable SSH access to the VM.")
. "$(Split-Path $MyInvocation.MyCommand.Path)/Enable-SimulationSshAccess.ps1" $script:DeploymentName

# Get IP address
$ipAddress = Get-AzureRmPublicIpAddress -ResourceGroupName $DeploymentName
Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - IP address of the VM is '{0}'" -f $ipAddress.IpAddress)
Write-Warning ("$(Get-Date –f $TIME_STAMP_FORMAT) - Your VM is now accessible from the public internet via SSH. Please make sure you have the latest security fixes applied by following the instructions here: https://wiki.ubuntu.com/Security/Upgrades")
