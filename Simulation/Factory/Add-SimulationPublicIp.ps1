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
    $script:VmResource = Get-AzureRmResource -ResourceName $script:VmName -ResourceType Microsoft.Compute/virtualMachines -ResourceGroupName $script:ResourceGroupName
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

try
{
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Get the public IP address.")
    $script:VmPublicIp = Get-AzureRmPublicIpAddress -Name $script:VmName -ResourceGroupName $script:ResourceGroupName -ErrorAction SilentlyContinue
}
catch {}

if ($script:VmPublicIp -eq $null)
{
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - The VM '{0}' does not have a public IP address. Adding one (will take a while)..." -f $script:VmName)
    # Add a public IP address to the VM
    try
    {
        # This might be inacurrate if there was a DNS name generated with random part.
        $dnsForPublicIpAddress = $script:ResourceGroupName.Substring(0, [System.Math]::Min(64, $resourceBaseName.Length)).ToLowerInvariant()
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Create a public IP address.")
        $script:VmPublicIp = New-AzureRmPublicIpAddress -Name $script:VmName -ResourceGroupName $script:ResourceGroupName -Location $script:VmResource.Location -AllocationMethod Dynamic -DomainNameLabel $dnsForPublicIpAddress
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Get the network interface.")
        $vmNetworkInterface = Get-AzureRmNetworkInterface -Name $script:VmName -ResourceGroupName $script:ResourceGroupName
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Set the public IP address on the network interface.")
        $vmNetworkInterface.IpConfigurations[0].PublicIpAddress = $script:VmPublicIp
        Set-AzureRmNetworkInterface -NetworkInterface $vmNetworkInterface | Out-Null
    }
    catch
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Unable to add a public IP address to VM '{0}' in resource group '{1}'" -f $script:VmName, $script:ResourceGroupName)
        throw ("Unable to add a public IP address to VM '{0}' in resource group '{1}'" -f $script:VmName, $script:ResourceGroupName)
    }
}

# Get IP address
$script:VmPublicIp = Get-AzureRmPublicIpAddress -ResourceGroupName $DeploymentName
Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - IP address of the VM is '{0}'" -f $script:VmPublicIp.IpAddress)
Write-Warning ("$(Get-Date –f $TIME_STAMP_FORMAT) - Your VM is now accessible from the public internet. Please make sure:")
Write-Warning ("$(Get-Date –f $TIME_STAMP_FORMAT) - > you have the latest security fixes applied by following the instructions here: https://wiki.ubuntu.com/Security/Upgrades")
Write-Warning ("$(Get-Date –f $TIME_STAMP_FORMAT) - > you have set the network security groups inbound and outbound rules as restricted as possible")
Write-Warning ("$(Get-Date –f $TIME_STAMP_FORMAT) - > you have set a strong password to access the VM")
