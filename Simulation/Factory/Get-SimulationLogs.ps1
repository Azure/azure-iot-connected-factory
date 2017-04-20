<#
.SYNOPSIS
    .
.DESCRIPTION
    .
.PARAMETER DeploymentName
    The name of the deployment to get the logs from. This is the resource group name.
.PARAMETER DockerUsername
    The username used to connect to the VM.
.PARAMETER DockerPassword
    The password of the user used to connect to the VM.
.EXAMPLE
    ./Get-SimulationLogs.ps1 mydeployment
    Downloads the logs from the VM of the deployment mydeployment to a subdirectory Logs of the current folder.
.NOTES
    .
#>
[CmdletBinding()]
Param(
[Parameter(Position=0, Mandatory=$true, HelpMessage="Specify the name of the deployment (this is the name used as the name for the VM and the resource group)")]
[string] $DeploymentName,
[Parameter(Position=1, Mandatory=$false, HelpMessage="Specify the name of the user in the VM.")]
[string] $DockerUsername="docker",
[Parameter(Position=2, Mandatory=$false, HelpMessage="Specify the password for the user.")]
[string] $DockerPassword="Passw0rd"
)

# Local path
$LocalPath = ((Get-Location).Path) + "\Logs"
Remove-Item -Path $LocalPath -Force -Recurse -ErrorAction SilentlyContinue | Out-Null
New-Item -Path $LocalPath -ItemType Directory | Out-Null

# Timeout for SSH operations
$ConnectionTimeout = 30000

# Set variables
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

$removePublicIp = $false
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

try
{
# Get IP address
$ipAddress = Get-AzureRmPublicIpAddress -ResourceGroupName $DeploymentName
Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - IP address of the VM is '{0}'" -f $ipAddress.IpAddress)

# Create a PSCredential object for SSH
$securePassword = ConvertTo-SecureString $DockerPassword -AsPlainText -Force
$sshCredentials = New-Object System.Management.Automation.PSCredential ($DockerUsername, $securePassword)

# Create SSH session
Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Create SSH session to VM with IP address '{0}'" -f $ipAddress.IpAddress)
$session = New-SSHSession $ipAddress.IpAddress -Credential $sshCredentials -AcceptKey -ConnectionTimeout $ConnectionTimeout
if ($session -eq $null)
{
    Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Cannot create SSH session to VM '{0}'" -f  $ipAddress.IpAddress)
    throw ("Cannot create SSH session to VM '{0}'" -f  $ipAddress.IpAddress)
}

# Copy simulation binaries to VM
Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Download Logs folder from VM")
Get-SCPFolder -RemoteFolder "/home/docker/Logs" -LocalFolder "$LocalPath" -ComputerName $ipAddress.IpAddress -Credential $sshCredentials -NoProgress -ConnectionTimeout $ConnectionTimeout


}
finally
{
    # Remove SSH session
    Remove-SSHSession $session.SessionId | Out-Null
}

# Remove the public IP address from the VM if we added it.
if ($removePublicIp -eq $true)
{
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Remove the public IP address from network interface.")
    $vmNetworkInterface.IpConfigurations[0].PublicIpAddress = $null
    Set-AzureRmNetworkInterface -NetworkInterface $vmNetworkInterface | Out-Null
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Delete the public IP address.")
    Remove-AzureRmPublicIpAddress -Name $script:VmName -ResourceGroupName $script:ResourceGroupName -Force | Out-Null
}