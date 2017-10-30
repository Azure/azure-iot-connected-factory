<#
.SYNOPSIS
    .
.DESCRIPTION
    Generates a new certificate and updates it in the website, VM and if needed in the local workstation.
.PARAMETER DeploymentName
    The name of the deployment to update the certificate for. This is the resource group name.
.PARAMETER VmAdminUsername
    The username used to connect to the VM.
.PARAMETER VmAdminPassword
    The password of the user used to connect to the VM.
.EXAMPLE
    ./Update-LocalSimulationCertificate.ps1 
    Creates an new certifiate, upload it to the simulation VM and imports it into the local hosts certificate store.
.NOTES
    .
#>
[CmdletBinding()]
Param(
[Parameter(Position=0, Mandatory=$true, HelpMessage="Specify the Azure account under which this operation should take place. Available account names could be seen by Get-AzureAccount.)")]
[string] $AccountName,
[Parameter(Position=1, Mandatory=$false, HelpMessage="Specify the name of the deployment (this is the name used as the name for the VM and the resource group)")]
[string] $DeploymentName,
[Parameter(Position=2, Mandatory=$false, HelpMessage="Specify the name of the user in the VM.")]
[string] $VmAdminUsername="docker",
[Parameter(Position=3, Mandatory=$false, HelpMessage="Specify the password for the user.")]
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


Import-Module Posh-SSH

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
$script:SshTimeout = 120

# Read the stored docker password.
if ([string]::IsNullOrEmpty($script:VmAdminPassword))
{
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - No VM password specified on command line. Trying to read from config.user file.")
    $script:IoTSuiteRootPath = (Split-Path $MyInvocation.MyCommand.Path) + "/../.."
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
    # Enable SSH access to the VM
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Enable SSH access to the VM.")
    . "$(Split-Path $MyInvocation.MyCommand.Path)/Enable-SimulationSshAccess.ps1" $script:DeploymentName
    Start-Sleep 5

    # Get IP address
    $ipAddress = Get-AzureRmPublicIpAddress -ResourceGroupName $DeploymentName
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - IP address of the VM is '{0}'" -f $ipAddress.IpAddress)

    # Create a PSCredential object for SSH
    $securePassword = ConvertTo-SecureString $script:VmAdminPassword -AsPlainText -Force
    $sshCredentials = New-Object System.Management.Automation.PSCredential ($VmAdminUsername, $securePassword)

    # Create SSH session
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Create SSH session to VM with IP address '{0}'" -f $ipAddress.IpAddress)
    $session = New-SSHSession $ipAddress.IpAddress -Credential $sshCredentials -AcceptKey  -ConnectionTimeout ($script:SshTimeout * 1000)
    if ($session -eq $null)
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Cannot create SSH session to VM '{0}'" -f  $ipAddress.IpAddress)
        throw ("Cannot create SSH session to VM '{0}'" -f  $ipAddress.IpAddress)
    }

    # Create a new certificate
    $script:FactoryPath = Split-Path $MyInvocation.MyCommand.Path
    $script:CreateCertsPath = "$script:FactoryPath/CreateCerts"
    $script:UaSecretBaseName = "UAWebClient"
    $script:UaSecretPassword = "password"
    $script:DockerRoot = "/home/$script:VmAdminUsername"
    $script:DockerSharedFolder = "Shared"
    $script:DockerCertsFolder = "$script:DockerSharedFolder/CertificateStores/UA Applications/certs"

    # Delete deployment certificates.
    Write-Output Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Delete existing certificates")
    Remove-Item -Recurse -Path "$script:CreateCertsPath/certs/$script:DeploymentName" -Force -ErrorAction SilentlyContinue | Out-Null
    Remove-Item -Recurse -Path "$script:CreateCertsPath/private/$script:DeploymentName" -Force -ErrorAction SilentlyContinue | Out-Null

    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Create certificate to secure OPC communication.");
    Invoke-Expression "dotnet run -p $script:CreateCertsPath $script:CreateCertsPath `"UA Web Client`" `"urn:localhost:Contoso:FactorySimulation:UA Web Client`""
    New-Item -Path "$script:CreateCertsPath/certs/$script:DeploymentName" -ItemType "Directory" -Force | Out-Null
    Move-Item "$script:CreateCertsPath/certs/$script:UaSecretBaseName.der" "$script:CreateCertsPath/certs/$script:DeploymentName/$script:UaSecretBaseName.der" -Force | Out-Null
    New-Item -Path "$script:CreateCertsPath/private/$script:DeploymentName" -ItemType "Directory" -Force | Out-Null
    Move-Item "$script:CreateCertsPath/private/$script:UaSecretBaseName.pfx" "$script:CreateCertsPath/private/$script:DeploymentName/$script:UaSecretBaseName.pfx" -Force | Out-Null

    $script:X509Collection = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2Collection
    $script:X509Collection.Import("$script:CreateCertsPath/private/$script:DeploymentName/$script:UaSecretBaseName.pfx", $script:UaSecretPassword, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable)
    $script:UaSecretThumbprint = $script:X509Collection.ThumbPrint
    Write-Output "$(Get-Date –f $TIME_STAMP_FORMAT) - X509 certificate for OPC UA communication has thumbprint: $script:UaSecretThumbprint"
    $script:UaSecretForWebsiteEncoded = [System.Convert]::ToBase64String($script:X509Collection.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pkcs12))
    $script:UaSecretForWebsiteEncodedSecure = ConvertTo-SecureString -String $script:UaSecretForWebsiteEncoded -AsPlainText -Force
    Set-AzureRmKeyVaultAccessPolicy -VaultName $script:DeploymentName -ObjectId (Get-AzureRmADUser -UserPrincipalName $script:AccountName).Id -PermissionsToSecrets all
    Set-AzureKeyVaultSecret -VaultName $script:DeploymentName -Name "$script:UaSecretBaseName-Website" -SecretValue $script:UaSecretForWebsiteEncodedSecure -ContentType 'application/x-pkcs12'

    if ($script:LocalDeployment)
    {
        # Install the pfx into our local cert store.
        Import-PfxCertificate -FilePath "$script:CreateCertsPath/private/$script:DeploymentName/$script:UaSecretBaseName.pfx" -CertStoreLocation cert:\CurrentUser\My -Password (ConvertTo-SecureString -String $script:UaSecretPassword -Force –AsPlainText)
    }

    # Copy public key to VM
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Copy certificate to VM")
    Set-SCPFile -LocalFile "$script:CreateCertsPath/certs/$script:DeploymentName/$script:UaSecretBaseName.der" -RemotePath "$script:DockerRoot" -ComputerName $ipAddress.IpAddress  -Credential $sshCredentials -NoProgress -OperationTimeout ($script:SshTimeout * 3)
    $vmCommand = "sudo mv  $script:DockerRoot/$script:UaSecretBaseName.der `"$script:DockerRoot/$script:DockerCertsFolder`""
    Invoke-SSHCommand -Sessionid $session.SessionId -Command $vmCommand -TimeOut $script:SshTimeout
    $vmCommand = "sudo chown root:root `"$script:DockerRoot/$script:DockerCertsFolder/$script:UaSecretBaseName.der`""
    Invoke-SSHCommand -Sessionid $session.SessionId -Command $vmCommand -TimeOut $script:SshTimeout
    $vmCommand = "sudo chmod u+x `"$script:DockerRoot/$script:DockerCertsFolder/$script:UaSecretBaseName.der`""
    Invoke-SSHCommand -Sessionid $session.SessionId -Command $vmCommand -TimeOut $script:SshTimeout
}
finally
{
    if ($session)
    {
        # Remove SSH session
        Remove-SSHSession $session.SessionId | Out-Null
    }

    # Disable SSH access to the VM
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Disable SSH access to the VM.")
    . "$(Split-Path $MyInvocation.MyCommand.Path)/Disable-SimulationSshAccess.ps1" $script:DeploymentName
}

# Remove the public IP address from the VM if we added it.
if ($removePublicIp -eq $true)
{
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Remove the public IP address from network interface.")
    $vmNetworkInterface.IpConfigurations[0].PublicIpAddress = $null
    Set-AzureRmNetworkInterface -NetworkInterface $vmNetworkInterface | Out-Null
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Delete the public IP address.")
    Remove-AzureRmPublicIpAddress -Name $script:VmName -ResourceGroupName $script:ResourceGroupName -Force | Out-Null
    # Remove the SSH allow rule from the network security group.
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Remove the SSH allow rule from the network security group.")
    $currentNsg = Get-AzureRmNetworkSecurityGroup -ResourceGroupName $script:ResourceGroupName -Name $script:ResourceGroupName
    Remove-AzureRmNetworkSecurityRuleConfig -Name AllowSshInBound -NetworkSecurityGroup $currentNsg | Out-Null
    Set-AzureRmNetworkSecurityGroup -NetworkSecurityGroup $currentNsg | Out-Null
}