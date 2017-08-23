<#
.SYNOPSIS
    Builds an Azure IoT Connected factory solution preconfigured solution (CF PCS).
.DESCRIPTION
    This script allows to build all components of the CF PCS. It is able to create and delete all required Azure resources and deploy the 
    different components to the Azure cloud resources.
.PARAMETER Command
    The command to execute. Supported commands are: "build", "updatesimulation", "local", "cloud", "clean", "delete"
    build: Does build the simulation.
.PARAMETER Configuration
    The configuration to build.or deploy. Supported values are: "debug", "release"
.PARAMETER DeploymentName
    The name of your solution deployment. The name must match the following regex: "^(?![0-9]+$)(?!-)[a-zA-Z0-9-]{3,49}[a-zA-Z0-9]{1,1}$"
.PARAMETER AzureEnvironmentName
    The name of the Azure environment to deploy your solution to. Supported values are: "AzureCloud"
.PARAMETER LowCost
    Use low cost SKUs of the required Azure resources.
.PARAMETER Force
    Enforced deployment even there is already a deployment with the same name.
.PARAMETER PresetAzureAccountName
    The name of the user account to use. This prevents from entering the selection menu if the account is valid.
.PARAMETER PresetAzureSubscriptionName
    The name of the Azure subscription to use. This prevents from entering the selection menu if the subscription name is valid.
.PARAMETER PresetAzureLocationName
    The name of the Azure location to use. This prevents from entering the selection menu if the location name is valid.
.PARAMETER PresetAzureDirectoryName
    The name of the Azure directory to use. This prevents from entering the selection menu if the directory name is valid.
.PARAMETER VmAdminPassword
    The admin password of the VM used for the simulation.
.EXAMPLE
    ./build.ps1
    Builds the solution.
.EXAMPLE
    ./build.ps1 build
    Builds the solution.
.EXAMPLE
    ./build.ps1 clean
    Removes all build artifacts and build output.
.EXAMPLE
    ./build.ps1 local
    Allocates all cloud resources, but not deploy your solution to the cloud.
    This is to develop and test your solution locally.
.EXAMPLE
    ./build.ps1 cloud -Configuration release -DeploymentName mydeployment
    Build the release version of your solution and deploys it to the AzureCloud environment.
.EXAMPLE
    ./build.ps1 cloud -Configuration release -DeploymentName mydeployment
    Build the release version of your solution and deploys it to the AzureCloud environment.
.EXAMPLE
    ./build.ps1 cloud -Configuration release -DeploymentName mydeployment -LowCost
    Build the release version of your solution and deploys it to the AzureCloud environment. The deployment is using those SKUs of the required resources which generate lowest cost.
.EXAMPLE
    ./build.ps1 cloud -Configuration release -DeploymentName mydeployment -PresetAzureAccountName myname@mydomain.com -PresetAzureSubscriptionName myszuresubscription -PresetAzureLocationName "West Europe" -PresetAzureDirectoryName mydomain.com
    Build the release version of your solution and deploys it to the AzureCloud environment using the preset values. This allows you to run the script without
    selecting any values manually.
.EXAMPLE
    ./build.ps1 cloud -Configuration release -DeploymentName mydeployment -AzureEnvironmentname AzureGermanCloud
    Build the release version of your solution and deploys it to the AzureGermanCloud environment.
.EXAMPLE
    ./build.ps1 updatesimulation -DeploymentName mydeployment
    Updates the simulation in the VM of the resource group mydeployment.
.EXAMPLE
    ./build.ps1 delete -DeploymentName mydeployment
    Updates the web packages of the resource group mydeployment.
.NOTES
    This is the user deployment script of Azure IoT Suite Connected factory.
#>
[CmdletBinding()]
Param(

[Parameter(Position=0, Mandatory=$false, HelpMessage="Specify the command to execute.")]
[ValidateSet("build", "updatesimulation", "local", "cloud", "clean", "delete")]
[string] $Command = "build",
[Parameter(Mandatory=$false, HelpMessage="Specify the configuration to build.")]
[ValidateSet("debug", "release")]
[string] $Configuration = "debug",
[Parameter(Mandatory=$false, HelpMessage="Specify the name of the solution")]
[ValidatePattern("^(?![0-9]+$)(?!-)[a-zA-Z0-9-]{3,49}[a-zA-Z0-9]{1,1}$")]
[ValidateLength(3, 62)]
[string] $DeploymentName = "local",
[Parameter(Mandatory=$false, HelpMessage="Specify the name of the Azure environment to deploy your solution into.")]
[ValidateSet("AzureCloud")]
[string] $AzureEnvironmentName = "AzureCloud",
[Parameter(Mandatory=$false, HelpMessage="Specify a username to use for the Azure deployment.")]
[switch] $LowCost = $false,
[Parameter(Mandatory=$false, HelpMessage="Enforce redeployment.")]
[switch] $Force = $false,
[Parameter(Mandatory=$false, HelpMessage="Flag to use SKUs with lowest cost for all required resources.")]
[string] $PresetAzureAccountName,
[Parameter(Mandatory=$false, HelpMessage="Specify the Azure subscription to use for the Azure deployment.")]
[string] $PresetAzureSubscriptionName,
[Parameter(Mandatory=$false, HelpMessage="Specify the Azure location to use for the Azure deployment.")]
[string] $PresetAzureLocationName,
[Parameter(Mandatory=$false, HelpMessage="Specify the Azure AD name to use for the Azure deployment.")]
[string] $PresetAzureDirectoryName,
[Parameter(Mandatory=$false, HelpMessage="Specify the admin password to use for the simulation VM.")]
[string] $VmAdminPassword
)

Function CheckCommandAvailability()
{
    Param(
        [Parameter(Mandatory=$true,Position=0)] $command
    )

    try
    {
        Get-Command -Name "$command" -ErrorAction SilentlyContinue
    }
    catch
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - '{0}' not found. Is your path variable set correct or the PowerShell module implementing the cmdlet installed?" -f $command)
        throw ("'{0}' not found. Is your path variable set correct or the PowerShell module implementing the cmdlet installed?" -f $command)
    }
    return $true
}


function InstallNuget()
{
    $nugetPath = "{0}/.nuget" -f $script:IoTSuiteRootPath
    if (-not (Test-Path "$nugetPath")) 
    {
        New-Item -Path "$nugetPath" -ItemType "Directory" | Out-Null
    }
    if (-not (Test-Path "$nugetPath/nuget.exe"))
    {
        $sourceNugetExe = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"
        $targetFile = $nugetPath + "/nuget.exe"
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - 'nuget.exe' not found. Downloading latest from $sourceNugetExe ...")
        Invoke-WebRequest $sourceNugetExe -OutFile "$targetFile"
    }
}

Function CheckModuleVersion()
{
    Param(
        [Parameter(Mandatory=$True,Position=0)] $ModuleName,
        [Parameter(Mandatory=$True,Position=1)] $ExpectedVersion
    )
    
    Import-Module $ModuleName 4> $null
    $Module = Get-Module -Name $ModuleName
    if ($Module.Count -eq 0)
    {
        # If the script fails here, you need to Install-Module from the PSGallery in an Administrator shell or install via the 
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - $ModuleName module was not found. Please install version $ExpectedVersion of the module.")
        throw "$(Get-Date –f $TIME_STAMP_FORMAT) - $ModuleName module was not found. Please install version $ExpectedVersion of the module."
    }
    else 
    {
        $ExpectedVersionObject = New-Object System.Version($ExpectedVersion)
        $ComparisonResult = $ExpectedVersionObject.CompareTo($Module.Version)
        if ($ComparisonResult -eq 1)
        {
            Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - $ModuleName module version $($Module.Version.Major).$($Module.Version.Minor).$($Module.Version.Build) is installed; please update to $($ExpectedVersion) and run again.")
            throw "$ModuleName module version $($Module.Version.Major).$($Module.Version.Minor).$($Module.Version.Build) is installed; please update to $($ExpectedVersion) and run again."
        }
        elseif ($ComparisonResult -eq -1)
        {
            if ($Module.Version.Major -ne $ExpectedVersion.Major -and $Module.Version.Minor -ne $ExpectedVersion.Minor)
            {
                Write-Warning "$(Get-Date –f $TIME_STAMP_FORMAT) - This script was tested with $ModuleName module version $($ExpectedVersion)"
                Write-Warning "$(Get-Date –f $TIME_STAMP_FORMAT) - Found $ModuleName module version $($Module.Version.Major).$($Module.Version.Minor).$($Module.Version.Build) installed; continuing, but errors might occur"
            }
        }
    }
}

Function GetAuthenticationResult()
{
    Param
    (
        [Parameter(Mandatory=$true, Position=0)] [string] $tenant,
        [Parameter(Mandatory=$true, Position=1)] [string] $authUri,
        [Parameter(Mandatory=$true, Position=2)] [string] $resourceUri,
        [Parameter(Mandatory=$false, Position=3)] [string] $user = $null,
        [Parameter(Mandatory=$false)] [string] $prompt = "Auto"
    )
    $psAadClientId = "1950a258-227b-4e31-a9cf-717495945fc2"
    [Uri]$aadRedirectUri = "urn:ietf:wg:oauth:2.0:oob"
    $authority = "{0}{1}" -f $authUri, $tenant
    write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Authority: '{0}'" -f $authority)
    $authContext = New-Object "Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext" -ArgumentList $authority,$true
    $userId = [Microsoft.IdentityModel.Clients.ActiveDirectory.UserIdentifier]::AnyUser
    if (![string]::IsNullOrEmpty($user))
    {
        $userId = new-object Microsoft.IdentityModel.Clients.ActiveDirectory.UserIdentifier -ArgumentList $user, "OptionalDisplayableId"
    }
    write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - {0}, {1}, {2}, {3}" -f $resourceUri, $psAadClientId, $aadRedirectUri, $userId.Id)
    $authResult = $authContext.AcquireToken($resourceUri, $psAadClientId, $aadRedirectUri, $prompt, $userId)
    return $authResult
}

#
# Called if no Azure location is configured for the deployment to let the user chose one location from within the used Azure environment.
# Note: do not use Write-Output since return value is used
#
Function GetAzureLocation()
{
    $locations = @();
    $index = 1
    foreach ($location in $script:AzureLocations)
    {
        $newLocation = New-Object System.Object
        $newLocation | Add-Member -MemberType NoteProperty -Name "Option" -Value $index
        $newLocation | Add-Member -MemberType NoteProperty -Name "Location" -Value $location
        $locations += $newLocation
        $index += 1
    }

    Write-Host
    Write-Host ("Available locations in Azure environment '{0}':" -f $script:AzureEnvironment.Name)
    Write-Host
    $script:OptionIndex = 1
    Write-Host ($locations | Format-Table @{Name='Option';Expression={$script:OptionIndex;$script:OptionIndex+=1};Alignment='right'},@{Name="Location";Expression={$_.Location}} -AutoSize | Out-String).Trim()
    Write-Host
    $location = ""
    while ($location -eq "" -or !(ValidateLocation $location))
    {
        try 
        {
            [int]$script:OptionIndex = Read-Host 'Select an option from the above location list'
        }
        catch 
        {
            Write-Host "Must be a number"
            continue
        }
        
        if ($script:OptionIndex -lt 1 -or $script:OptionIndex -ge $index)
        {
            continue
        }
        $location = $script:AzureLocations[$script:OptionIndex - 1]
    }
    Write-Verbose "$(Get-Date –f $TIME_STAMP_FORMAT) - Azure location '$location' selected."
    # Workaround since errors pipe to the output stream
    $script:GetOrSetSettingValue = $location
}

Function ValidateLocation()
{
    Param (
        [Parameter(Mandatory=$true)] [string] $locationToValidate
    )
        
    foreach ($location in $script:AzureLocations)
    {
        if ($location.Replace(' ', '').ToLowerInvariant() -eq $location.Replace(' ', '').ToLowerInvariant())
        {
            return $true;
        }
    }
    Write-Warning ("$(Get-Date –f $TIME_STAMP_FORMAT) - Location '{0} is not available for this subscription.  Please chose a different location." -f $locationToValidate)
    Write-Warning "$(Get-Date –f $TIME_STAMP_FORMAT) - Available Locations:"
    foreach ($location in $script:AzureLocations)
    {
        Write-Warning $location
    }
    return $false
}

Function GetResourceGroup()
{
    $resourceGroup = Find-AzureRmResourceGroup -Tag @{"IotSuiteType" = $script:SuiteType} | ?{$_.Name -eq $script:SuiteName}
    if ($resourceGroup -eq $null)
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) No resource group found with name '{0}' and type '{1}'" -f $script:SuiteName, $script:SuiteType)
        # If the simulation should be updated, it is expected that the resource group exists
        if ($script:Command -ne "updatesimulation")
        {
            Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) GetResourceGroup Location: '{0}, IoTSuiteVersion: '{1}'" -f $script:AzureLocation, $script:IotSuiteVersion)
            return New-AzureRmResourceGroup -Name $script:SuiteName -Location $script:AzureLocation -Tag @{"IoTSuiteType" = $script:SuiteType ; "IoTSuiteVersion" = $script:IotSuiteVersion ; "IoTSuiteState" = "Created"}
        }
        else
        {
            return $null
        }
    }
    else
    {
        return Get-AzureRmResourceGroup -Name $script:SuiteName 
    }
}

Function UpdateResourceGroupState()
{
    Param(
        [Parameter(Mandatory=$true,Position=1)] [string] $state
    )

    $resourceGroup = Get-AzureRmResourceGroup -ResourceGroupName $script:ResourceGroupName
    if ($resourceGroup -ne $null)
    {
        $tags = $resourceGroup.Tags
        $updated = $false
        if ($tags.ContainsKey("IoTSuiteState"))
        {
            $tags.IoTSuiteState = $state
            $updated = $true
        }
        if ($tags.ContainsKey("IoTSuiteVersion") -and $tags.IoTSuiteVersion -ne $script:IotSuiteVersion)
        {
            $tags.IoTSuiteVersion = $script:IotSuiteVersion
            $updated = $true
        }
        if (!$updated)
        {
            $tags += @{"IoTSuiteState" = $state}
        }
        $resourceGroup = Set-AzureRmResourceGroup -Name $script:ResourceGroupName -Tag $tags
    }
}

Function ValidateResourceName()
{
    Param(
        [Parameter(Mandatory=$true,Position=0)] [string] $resourceBaseName,
        [Parameter(Mandatory=$true,Position=1)] [string] $resourceType
    )

    # Generate a unique name
    $resourceUrl = " "
    $allowNameReuse = $true
    switch ($resourceType.ToLowerInvariant())
    {
        "microsoft.devices/iothubs"
        {
            $resourceUrl = $script:IotHubSuffix
        }
        "microsoft.storage/storageaccounts"
        {
            $resourceUrl = "blob.{0}" -f $script:AzureEnvironment.StorageEndpointSuffix
            $resourceBaseName = $resourceBaseName.Substring(0, [System.Math]::Min(19, $resourceBaseName.Length))
        }
        "microsoft.web/sites"
        {
            $resourceUrl = $script:WebsiteSuffix
        }
        "microsoft.network/publicipaddresses"
        {
            $resourceBaseName = $resourceBaseName.Substring(0, [System.Math]::Min(40, $resourceBaseName.Length))
        }
        default {}
    }
    
    # Return name for existing resource if exists
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Check if Azure resource: '{0}' (type: '{1}') exists in resource group '{2}'" -f $resourceBaseName, $resourceType, $resourceGroupName)
    $resources = Find-AzureRmResource -ResourceGroupNameContains $script:ResourceGroupName -ResourceType $resourceType -ResourceNameContains $resourceBaseName
    if ($resources -ne $null -and $allowNameReuse)
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Found the resource. Validating exact naming.")
        foreach($resource in $resources)
        {
            if ($resource.ResourceGroupName -eq $script:ResourceGroupName -and $resource.Name.ToLowerInvariant().StartsWith($resourceBaseName.ToLowerInvariant()))
            {
                Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Resource with matching resource group name and name found.")
                return $resource.Name
            }
        }
    }
    
    return GetUniqueResourceName $resourceBaseName $resourceUrl
}

Function GetUniqueResourceName()
{
    Param(
        [Parameter(Mandatory=$true,Position=0)] [string] $resourceBaseName,
        [Parameter(Mandatory=$true,Position=1)] [string] $resourceUrl
    )

    # retry max 200 times if the random name already exists
    $max = 200
    $name = $resourceBaseName
    while (HostEntryExists ("{0}.{1}" -f $name, $resourceUrl))
    {
        $name = "{0}{1:x5}" -f $resourceBaseName, (get-random -max 1048575)
        if ($max-- -le 0)
        {
            Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Unable to create unique name for resource {0} for url {1}" -f $resourceBaseName, $resourceUrl)
            throw ("Unable to create unique name for resource {0} for url {1}" -f $resourceBaseName, $resourceUrl)
        }
    }
    ClearDnsCache
    return $name
}

Function GetAzureStorageAccount()
{
    $storageTempName = $script:SuiteName.ToLowerInvariant().Replace('-','')
    $storageAccountName = ValidateResourceName $storageTempName.Substring(0, [System.Math]::Min(19, $storageTempName.Length)) Microsoft.Storage/storageAccounts
    $storage = Get-AzureRmStorageAccount -ResourceGroupName $script:ResourceGroupName -Name $storageAccountName -ErrorAction SilentlyContinue
    if ($storage -eq $null)
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Creating new storage account: '{0}" -f $storageAccountName)
        $storage = New-AzureRmStorageAccount -ResourceGroupName $script:ResourceGroupName -StorageAccountName $storageAccountName -Location $script:AzureLocation -Type $script:StorageSkuName -Kind $script:StorageKind
    }
    return $storage
}

function GetDnsForPublicIpAddress()
{
    return (ValidateResourceName $script:SuiteName.ToLowerInvariant() Microsoft.Network/publicIPAddresses).ToLowerInvariant()
}

function GetAzureIotHubName()
{
    return ValidateResourceName $script:SuiteName Microsoft.Devices/iotHubs
}

function GetAzureVmName()
{
    return ValidateResourceName $script:SuiteName Microsoft.Compute/VirtualMachines
}

function GetAzureRdxName()
{
    return ValidateResourceName $script:SuiteName Microsoft.TimeSeriesInsights/environments
}

Function EnvSettingExists()
{
    Param(
        [Parameter(Mandatory=$true,Position=0)] $settingName
    )

    return ($script:DeploymentSettingsXml.Environment.SelectSingleNode("//setting[@name = '$settingName']") -ne $null);
}

Function GetOrSetEnvSetting()
{
    Param(
        [Parameter(Mandatory=$true,Position=0)] [string] $settingName,
        [Parameter(Mandatory=$true,Position=1)] [string] $function
    )

    $settingValue = GetEnvSetting $settingName $false
    if ([string]::IsNullOrEmpty($settingValue))
    {
        $script:GetOrSetSettingValue = $null
        $settingValue = &$function
        if ($script:GetOrSetSettingValue -ne $null)
        {
            $settingValue = $GetOrSetSettingValue
            $script:GetOrSetSettingValue = $null
        }
        PutEnvSetting $settingName $settingValue | Out-Null
    }
    return $settingValue
}

Function UpdateEnvSetting()
{
    Param(
        [Parameter(Mandatory=$true,Position=0)] $settingName,
        [Parameter(Mandatory=$true,Position=1)] [AllowEmptyString()] $settingValue
    )

    $currentValue = GetEnvSetting $settingName $false
    if ($currentValue -ne $settingValue)
    {
        PutEnvSetting $settingName $settingValue
    }
}

Function GetEnvSetting()
{
    Param(
        [Parameter(Mandatory=$true,Position=0)] [string] $settingName,
        [Parameter(Mandatory=$false,Position=1)] [switch] $errorOnNull = $true
    )

    $setting = $script:DeploymentSettingsXml.Environment.SelectSingleNode("//setting[@name = '$settingName']")

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

Function PutEnvSetting()
{
    Param(
        [Parameter(Mandatory=$True,Position=0)] [string] $settingName,
        [Parameter(Mandatory=$True,Position=1)] [AllowEmptyString()] [string] $settingValue
    )

    if (EnvSettingExists $settingName)
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - {0} changed to {1}" -f $settingName, $settingValue)
        $script:DeploymentSettingsXml.Environment.SelectSingleNode("//setting[@name = '$settingName']").value = $settingValue
    }
    else
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Added {0} with value {1}" -f $settingName, $settingValue)
        $node = $script:DeploymentSettingsXml.CreateElement("setting")
        $node.SetAttribute("name", $settingName) | Out-Null
        $node.SetAttribute("value", $settingValue) | Out-Null
        $script:DeploymentSettingsXml.Environment.AppendChild($node) | Out-Null
    }
    $script:DeploymentSettingsXml.Save((Get-Item $script:DeploymentSettingsFile).FullName)
}

#
# Called in case no account is configured to let user chose the account.
# Note: do not use Write-Output since return value is used
#
Function GetAzureAccountInfo()
{
    if ($script:PresetAzureAccountName -ne $null -and $script:PresetAzureAccountName -ne "")
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Using preset account name '{0}'" -f $script:PresetAzureAccountName)
        $account = Get-AzureAccount $script:PresetAzureAccountName

    }
    if ($account -eq $null)
    {
        $accounts = Get-AzureAccount
        if ($accounts -eq $null)
        {
            Write-Verbose "$(Get-Date –f $TIME_STAMP_FORMAT) - Add new Azure account"
            $account = Add-AzureAccount -Environment $script:AzureEnvironment.Name
        }
        else 
        {
            Write-Host "$(Get-Date –f $TIME_STAMP_FORMAT) - Select Azure account to use"
            $script:OptionIndex = 1
            Write-Host
            Write-Host ("Available accounts in Azure environment '{0}':" -f $script:AzureEnvironment.Name)
            Write-Host
            Write-Host (($accounts | Format-Table @{Name='Option';Expression={$script:OptionIndex;$script:OptionIndex+=1};Alignment='right'}, Id, Subscriptions -AutoSize) | Out-String).Trim()
            Write-Host (("{0}" -f $script:OptionIndex).PadLeft(6) + " Use another account")
            Write-Host
            $account = $null
            while ($account -eq $null)
            {
                try
                {
                    [int]$script:OptionIndex = Read-Host "Select an option from the above account list"
                }
                catch
                {
                    Write-Host "Must be a number"
                    continue
                }

                if ($script:OptionIndex -eq $accounts.length + 1)
                {
                    $account = Add-AzureAccount -Environment $script:AzureEnvironment.Name
                    break;
                }
                
                if ($script:OptionIndex -lt 1 -or $script:OptionIndex -gt $accounts.length)
                {
                    continue
                }
                
                $account = $accounts[$script:OptionIndex - 1]
            }
        }
    }
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Account Id to use is '{0}'" -f $account.Id)
    if ([string]::IsNullOrEmpty($account.Id))
    {
            Write-Error -("$(Get-Date –f $TIME_STAMP_FORMAT) - There was no account selected. Please check and try again.")
            throw ("There was no account selected. Please check and try again.")
    }
    # Workaround since errors pipe to the output stream
    $script:GetOrSetSettingValue = $account.Id
}

Function ValidateLoginCredentials()
{
    # Validate Azure account
    $account = Get-AzureAccount -Name $script:AzureAccountName
    if ($account -eq $null)
    {
        Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Account '{0}' is unknown in Azure environment '{1}'. Add it." -f $script:AzureAccountName, $script:AzureEnvironment.Name)
        $account = Add-AzureAccount -Environment $script:AzureEnvironment.Name
    }
    if ((Get-AzureSubscription -SubscriptionId ($account.Subscriptions -replace '(?:\r\n)',',').split(",")[0]) -eq $null)
    {
        Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - No subscriptions. Add account")
        Add-AzureAccount -Environment $script:AzureEnvironment.Name | Out-Null
    }
    
    # Validate Azure RM
    $profileFile = ($IotSuiteRootPath + "/$($script:AzureAccountName).user")
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Check for profile file '{0}'" -f $profileFile)
    if (Test-Path "$profileFile") 
    {
        Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Use saved profile from '{0}" -f $profileFile)
        if ($script:AzurePowershellVersionMajor -le 3)
        {
            $rmProfile = Select-AzureRmProfile -Path "$profileFile"
        }
        else 
        {
            $rmProfile = Import-AzureRmContext -Path "$profileFile"
        }
        $rmProfileLoaded = ($rmProfile -ne $null) -and ($rmProfile.Context -ne $null) -and ((Get-AzureRmSubscription) -ne $null)
    }
    if ($rmProfileLoaded -ne $true) {
        Write-Output "$(Get-Date –f $TIME_STAMP_FORMAT) - Logging in to your AzureRM account"
        try {
            Login-AzureRmAccount -EnvironmentName $script:AzureEnvironment.Name -ErrorAction Stop | Out-Null
        }
        catch
        {
            Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - The login to the Azure account was not successful. Please run the script again.")
            throw ("The login to the Azure account was not successful. Please run the script again.")
        }
        if ($script:AzurePowershellVersionMajor -le 3)
        {
            Save-AzureRmProfile -Path "$profileFile"
        }
        else 
        {
            Save-AzureRmContext -Path "$profileFile"
        }
    }
}

Function HostEntryExists()
{
    Param(
        [Parameter(Mandatory=$true,Position=0)] $hostName
    )

    try
    {
        if ([Net.Dns]::GetHostEntry($hostName) -ne $null)
        {
            Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Found hostname: {0}" -f $hostName)
            return $true
        }
    }
    catch {}
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Did not find hostname: {0}" -f $hostName)
    return $false
}

Function ClearDnsCache()
{
    if ($ClearDns -eq $null)
    {
        try 
        {
            $ClearDns = CheckCommandAvailability Clear-DnsClientCache
        }
        catch 
        {
            $ClearDns = $false
        }
    }
    if ($ClearDns)
    {
        Clear-DnsClientCache
    }
}

Function ReplaceFileParameters()
{
    Param(
        [Parameter(Mandatory=$true,Position=0)] [string] $filePath,
        [Parameter(Mandatory=$true,Position=1)] [array] $arguments
    )

    $fileContent = Get-Content "$filePath" | Out-String
    for ($i = 0; $i -lt $arguments.Count; $i++)
    {
        $fileContent = $fileContent.Replace("{$i}", $arguments[$i])
    }
    return $fileContent
}

# Generate a random password
# Usage: RandomPassword <length>
# For more information see
# https://blogs.technet.microsoft.com/heyscriptingguy/2013/06/03/generating-a-new-password-with-windows-powershell/
Function RandomPassword ($length = 15)
{
    $punc = 46..46
    $digits = 48..57
    $lcLetters = 65..90
    $ucLetters = 97..122

    $password = [char](Get-Random -Count 1 -InputObject ($lcLetters)) + [char](Get-Random -Count 1 -InputObject ($ucLetters)) + [char](Get-Random -Count 1 -InputObject ($digits)) + [char](Get-Random -Count 1 -InputObject ($punc))
    $password += get-random -Count ($length -4) -InputObject ($punc + $digits + $lcLetters + $ucLetters) | % -begin { $aa = $null } -process {$aa += [char]$_} -end {$aa}

    return $password
}

Function CreateAadClientSecret()
{
    $newPassword = RandomPassword
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - New Password: {0}" -f $newPassword)
    Remove-AzureRmADAppCredential -ApplicationId $script:AadClientId -All -Force
    # create new secret for web app, $secret is converted to PSAD type
    # keep $newPassword to be returned as a string
    $secret = $newPassword
    $startDate = Get-Date
    $secret = New-AzureRmADAppCredential -ApplicationId $script:AadClientId -StartDate $startDate -EndDate $startDate.AddYears(1) -Password $secret
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - New Secret Id: {0}" -f $secret.KeyId)
    return $newPassword
}

#
# Called when no configuration for the AAD tenant to use was found to let the user choose one.
# Note: do not use Write-Output since return value is used
#
Function GetAadTenant()
{
    $tenants = Get-AzureRmTenant
    if ($tenants.Count -eq 0)
    {
        Write-Error  ("$(Get-Date –f $TIME_STAMP_FORMAT) - No Active Directory domains found for '{0}'" -f $script:AzureAccountName)
        throw ("No Active Directory domains found for '{0}'" -f $script:AzureAccountName)
    }
    if ($tenants.Count -eq 1)
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Only one tenant found, use it. TenantId: '{0}' with IdentifierUri '{1}' exists in Azure environment '{2}'" -f $script:WebAppDisplayName , $script:WebAppIdentifierUri, $script:AzureEnvironment.Name)
        if ($script:AzurePowershellVersionMajor -le 3)
        {
            $tenantId = $tenants[0].TenantId
        }
        else
        {
            $tenantId = $tenants[0].Id
        }
    }
    else
    {
        # List Active directories associated with account
        $directories = @()
        $index = 1
        [int]$selectedIndex = -1
        foreach ($tenantObj in $tenants)
        {
            if ($script:AzurePowershellVersionMajor -le 3)
            {
                $tenant = $tenantObj.TenantId
            }
            else
            {
                $tenant = $tenantObj.Id
            }
            $uri = "{0}{1}/me?api-version=1.6" -f $script:AzureEnvironment.GraphUrl, $tenant
            $authResult = GetAuthenticationResult $tenant $script:AzureEnvironment.ActiveDirectoryAuthority $script:AzureEnvironment.GraphUrl $script:AzureAccountName -Prompt "Auto"
            $header = $authResult.CreateAuthorizationHeader()
            $result = Invoke-RestMethod -Method "GET" -Uri $uri -Headers @{"Authorization"=$header;"Content-Type"="application/json"}
            if ($result -ne $null)
            {
                $directory = New-Object System.Object
                $directory | Add-Member -MemberType NoteProperty -Name "Option" -Value $index
                $directory | Add-Member -MemberType NoteProperty -Name "Directory Name" -Value ($result.userPrincipalName.Split('@')[1])
                $directory | Add-Member -MemberType NoteProperty -Name "Tenant Id" -Value $tenant
                $directories += $directory
                if ($script:PresetAzureDirectoryName -ne $null -and $script:PresetAzureDirectoryName -eq ($result.userPrincipalName.Split('@')[1]))
                {
                    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Using preset directory name '{0}'" -f $script:PresetAzureDirectoryName)
                    $selectedIndex = $index
                    break
                }
                $index += 1
            }
        }

        if ($selectedIndex -eq -1)
        {
            Write-Host "Select an Active Directories to use"
            Write-Host
            Write-Host "Available Active Directories:"
            Write-Host
            Write-Host ($directories | Out-String) -NoNewline
            while ($selectedIndex -lt 1 -or $selectedIndex -ge $index)
            {
                try
                {
                    [int]$selectedIndex = Read-Host "Select an option from the above directory list"
                }
                catch
                {
                    Write-Host "Must be a number"
                }
            }
        }
        if ($script:AzurePowershellVersionMajor -le 3)
        {
            $tenantId = $tenants[$selectedIndex - 1].TenantId
        }
        else
        {
            $tenantId = $tenants[$selectedIndex - 1].Id
        }
    }

    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - AAD Tenant ID is '{0}'" -f $tenantId)
    # Workaround since errors pipe to the output stream
    $script:GetOrSetSettingValue = $tenantId -as [string]
}

Function UpdateAadApp($tenantId)
{
    # Check for application existence
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Check if application '{0}' with IdentifierUri '{1}' exists in Azure environment '{2}'" -f $script:WebAppDisplayName , $script:WebAppIdentifierUri, $script:AzureEnvironment.Name)
    $uri = "{0}{1}/applications?api-version=1.6" -f $script:AzureEnvironment.GraphUrl, $tenantId
    $searchUri = "{0}&`$filter=identifierUris/any(uri:uri%20eq%20'{1}')" -f $uri, [System.Web.HttpUtility]::UrlEncode($script:WebAppIdentifierUri)
    $authResult = GetAuthenticationResult $tenantId $script:AzureEnvironment.ActiveDirectoryAuthority $script:AzureEnvironment.GraphUrl $script:AzureAccountName
    $header = $authResult.CreateAuthorizationHeader()
    $result = Invoke-RestMethod -Method "GET" -Uri $searchUri -Headers @{"Authorization"=$header;"Content-Type"="application/json"}
    if ($result.value.Count -eq 0)
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Application '{0}' not found, create it with IdentifierUri '{1}'" -f $script:WebAppDisplayName, $script:WebAppIdentifierUri)
        $body = ReplaceFileParameters ("{0}/Application.json" -f $script:DeploymentConfigPath) -arguments @($script:WebAppHomepage, $script:WebAppDisplayName, $script:WebAppIdentifierUri)
        $result = Invoke-RestMethod -Method "POST" -Uri $uri -Headers @{"Authorization"=$header;"Content-Type"="application/json"} -Body $body -ErrorAction SilentlyContinue
        if ($result -eq $null)
        {
            Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Unable to create application '{0}' with IdentifierUri '{1}'" -f $script:WebAppDisplayName, $script:WebAppIdentifierUri)
            throw "Unable to create application '$script:WebAppDisplayName'"
        }
        if ([string]::IsNullOrEmpty($result.appId))
        {
            Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Unable to create application '{0}' with IdentifierUri '{1}', returned AppId is null." -f $script:WebAppDisplayName, $script:WebAppIdentifierUri)
            throw ("Unable to create application '{0}', returned AppId is null." -f $script:WebAppDisplayName)
        }
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Successfully created application '{0}' with Id '{1}' and IdentifierUri '{2}'" -f $result.displayName, $result.appId, $result.identifierUri)
        $applicationId = $result.appId
    }
    else
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Found application '{0}' with Id '{1}' and IdentifierUri '{2}'" -f $result.value[0].displayName, $result.value[0].appId, $result[0].identifierUri)
        $applicationId = $result.value[0].appId
    }

    $script:AadClientId = $applicationId
    UpdateEnvSetting "AadClientId" $applicationId

    # Check for ServicePrincipal
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Check for service principal in '{0}', tenant '{1}' for application '{2}' with appID '{3}'" -f  $script:AzureEnvironment.Name, $tenantId, $script:WebAppDisplayName, $applicationId)
    $uri = "{0}{1}/servicePrincipals?api-version=1.6" -f $script:AzureEnvironment.GraphUrl, $tenantId
    $searchUri = "{0}&`$filter=appId%20eq%20'{1}'" -f $uri, $applicationId
    $result = Invoke-RestMethod -Method "GET" -Uri $searchUri -Headers @{"Authorization"=$header;"Content-Type"="application/json"}
    if ($result.value.Count -eq 0)
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - No service principal found. Create one in '{0}', tenant '{1}' for application '{2}' with appID '{3}'" -f  $script:AzureEnvironment.Name, $tenantId, $script:WebAppDisplayName, $applicationId)
        $body = "{ `"appId`": `"$applicationId`" }"
        $result = Invoke-RestMethod -Method "POST" -Uri $uri -Headers @{"Authorization"=$header;"Content-Type"="application/json"} -Body $body -ErrorAction SilentlyContinue
        if ($result -eq $null)
        {
            Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Unable to create ServicePrincipal for application '{0}' with appID '{1}'" -f $script:WebAppDisplayName, $applicationId)
            throw ("Unable to create ServicePrincipal for application '{0}' with appID '{1}'" -f $script:WebAppDisplayName, $applicationId)
        }
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Successfully created service principal '{0}' with resource Id '{1}'" -f $result.displayName, $result.objectId)
        $resourceId = $result.objectId
        $roleId = ($result.appRoles| ?{$_.value -eq "admin"}).Id
    }
    else
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Found service principal '{0}' with resource Id '{1}'" -f $result.displayName, $result.value[0].objectId)
        $resourceId = $result.value[0].objectId
        $roleId = ($result.value[0].appRoles| ?{$_.value -eq "admin"}).Id
    }

    # Check for Assigned User
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Check for app role assigment in '{0}', tenant '{1}' for user with Id '{2}'" -f  $script:AzureEnvironment.Name, $tenantId, $authResult.UserInfo.UniqueId)
    $uri = "{0}{1}/users/{2}/appRoleAssignments?api-version=1.6" -f $script:AzureEnvironment.GraphUrl, $tenantId, $authResult.UserInfo.UniqueId
    $result = Invoke-RestMethod -Method "GET" -Uri $uri -Headers @{"Authorization"=$header;"Content-Type"="application/json"}
    if (($result.value | ?{$_.ResourceId -eq $resourceId}) -eq $null)
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Create app role assigment 'principalId' in '{0}', tenant '{1}' for user with Id '{2}'" -f  $script:AzureEnvironment.Name, $tenantId, $authResult.UserInfo.UniqueId)
        $body = "{ `"id`": `"$roleId`", `"principalId`": `"$($authResult.UserInfo.UniqueId)`", `"resourceId`": `"$resourceId`" }"
        $result = Invoke-RestMethod -Method "POST" -Uri $uri -Headers @{"Authorization"=$header;"Content-Type"="application/json"} -Body $body -ErrorAction SilentlyContinue
        if ($result -eq $null)
        {
            Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Unable to create app role assignment for application '{0}' with appID '{1}' for current user - will be Implicit Readonly" -f $script:WebAppDisplayName, $applicationId)
        }
        else
        {
            Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Successfully created 'Service Principal' app role assignment for user '{0}' for application '{1}' with appID '{2}''" -f $authResult.UserInfo.UniqueId,$result.resourceDisplayName, $applicationId)
        }
    }
    else
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - User with Id '{0}' has role 'Service Principal' already assigned for the application '{1}' with appID '{2}'" -f $authResult.UserInfo.UniqueId,$result.resourceDisplayName, $applicationId)
    }
}

Function InitializeDeploymentSettings()
{
    #
    # Initialize deployment settings
    #
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Using deployment settings filename {0}" -f $script:DeploymentSettingsFile)

    # read settings into XML variable
    if (!(Test-Path "$script:DeploymentSettingsFile"))
    {
        Copy-Item ("{0}/ConfigurationTemplate.config" -f $script:DeploymentConfigPath) $script:DeploymentSettingsFile | Out-Null
    }
    $script:DeploymentSettingsXml = [xml](Get-Content "$script:DeploymentSettingsFile")
}

Function InitializeEnvironment()
{
    #
    # Azure login
    #
    $script:AzureAccountName = GetOrSetEnvSetting "AzureAccountName" "GetAzureAccountInfo" 
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Validate Azure account '{0}'" -f $script:AzureAccountName)
    ValidateLoginCredentials

    if ($script:PresetAzureSubscriptionName -ne $null -and $script:PresetAzuresubscriptionName -ne "")
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Using preset subscription name '{0}'" -f $script:PresetAzureSubscriptionName)
        $subscriptionId = Get-AzureRmSubscription -SubscriptionName $script:PresetAzureSubscriptionName
    }

    #
    # Select Azure subscription to use
    #
    if ([string]::IsNullOrEmpty($subscriptionId))
    {
        $subscriptionId = GetEnvSetting "SubscriptionId"
        if ([string]::IsNullOrEmpty($subscriptionId))
        {
            Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Select an Azure subscription to use ")
            $subscriptions = Get-AzureRMSubscription
            if ($subscriptions.Count -eq 1)
            {
                if ($script:AzurePowershellVersionMajor -le 3)
                {
                    $subscriptionId = $subscriptions[0].SubscriptionId
                }
                else
                {
                    $subscriptionId = $subscriptions[0].Id
                }
            }
            else
            {
                $script:OptionIndex = 1
                Write-Host
                Write-Host ("Available subscriptions for account '{0}'" -f $script:AzureAccountName)
                Write-Host
                if ($script:AzurePowershellVersionMajor -le 3)
                {
                    Write-Host ($subscriptions | Format-Table @{Name='Option';Expression={$script:OptionIndex;$script:OptionIndex+=1};Alignment='right'},SubscriptionName, subscriptionId -AutoSize | Out-String).Trim() 
                }
                else
                {
                    Write-Host ($subscriptions | Format-Table @{Name='Option';Expression={$script:OptionIndex;$script:OptionIndex+=1};Alignment='right'},Name, Id -AutoSize | Out-String).Trim() 
                }
                Write-Host
                while ($true)
                {
                    if ($script:AzurePowershellVersionMajor -le 3)
                    {
                        if ($subscriptions.SubscriptionId.Contains($subscriptionId))
                        {
                            break;
                        }
                    }
                    else
                    {
                        if ($subscriptions.Id.Contains($subscriptionId))
                        {
                            break;
                        }
                    }

                    try
                    {
                        [int]$script:OptionIndex = Read-Host "Select an option from the above subscription list"
                    }
                    catch
                    {
                        Write-Host "Must be a number"
                        continue
                    }

                    if ($script:OptionIndex -lt 1 -or $script:OptionIndex -gt $subscriptions.length)
                    {
                        continue
                    }

                    if ($script:AzurePowershellVersionMajor -le 3)
                    {
                        $subscriptionId = $subscriptions[$script:OptionIndex - 1].SubscriptionId
                    }
                    else
                    {
                        $subscriptionId = $subscriptions[$script:OptionIndex - 1].Id
                    }
                }
            }
        }
    }
    UpdateEnvSetting "SubscriptionId" $subscriptionId
    $rmSubscription = Get-AzureRmSubscription -SubscriptionId $subscriptionId
    if ($script:AzurePowershellVersionMajor -le 3)
    {
        $subscriptionName = $rmSubscription.SubscriptionName
    }
    else
    {
        $subscriptionName = $rmSubscription.Name
    }
    $tenantId = $rmSubscription.TenantId
    Set-AzureRmContext -SubscriptionName $subscriptionName -TenantId $tenantId | Out-Null
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Selected Azure subscription {0} with ID {1}" -f $subscriptionName, $subscriptionId)

    # Initialize Tenant
    $script:AadTenant = GetOrSetEnvSetting "AadTenant" "GetAADTenant"
    if ($tenantId -ne $script:AadTenant)
    {
        throw ("Unable to use directory different than subscription tenant.")
    }

    #
    # Initialize location
    #
    if ($script:PresetAzureLocationName -ne $null -and $script:PresetAzureLocationName -ne "")
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Using preset allocation location name '{0}'" -f $script:PresetAzureLocationName)
        $script:AzureLocation = $script:PresetAzureLocationName
    }
    else
    {
        $script:AzureLocation = GetEnvSetting "AzureLocation"
        if ([string]::IsNullOrEmpty($script:AzureLocation))
        {
            Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Select Azure location to use")
            $script:AzureLocation = GetOrSetEnvSetting "AzureLocation" "GetAzureLocation"
        }
    }
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Azure location to use is '{0}'" -f $script:AzureLocation)
}

# Replace browser endpoint configuration file in WebApp
Function FixWebAppPackage()
{
    Param(
        [Parameter(Mandatory=$true,Position=0)] [string] $filePath
    )

    # Set path correct
    $browserEndpointsName = "OPC.Ua.Browser.Endpoints.xml"
    $browserEndpointsFullName = "$script:IoTSuiteRootPath/WebApp/$browserEndpointsName"
    $zipfile = Get-Item "$filePath"
    [System.IO.Compression.ZipArchive]$zipArchive = [System.IO.Compression.ZipFile]::Open($zipfile.FullName, "Update")

    $entries = $zipArchive.Entries | Where-Object { $_.FullName -match ".*$browserEndpointsName" } 
    foreach ($entry in $entries)
    { 
        $fullPath = $entry.FullName
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Found '{0}' in archive" -f $fullPath)
        $entry.Delete()
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zipArchive, $browserEndpointsFullName, $fullPath) | Out-Null
    }
    $zipArchive.Dispose()
}

Function ResourceObjectExists
 {
    Param(
        [Parameter(Mandatory=$true,Position=1)] [string] $resourceName,
        [Parameter(Mandatory=$true,Position=2)] [string] $type
    )

    return (GetResourceObject -resourceName $resourceName -type $type | Where-Object {$_.Name -eq $resourceName}) -ne $null
 }

Function GetResourceObject
 {
    Param(
         [Parameter(Mandatory=$true,Position=1)] [string] $resourceName,	
         [Parameter(Mandatory=$true,Position=2)] [string] $type
    )

    $result = $null
    try
    {
        $result = Get-AzureRmResource -ResourceName $resourceName -ResourceGroupName $script:ResourceGroupName -ResourceType $type
    }
    catch {}
    return $result
}

Function SimulationBuild
{
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Building Simulation for configuration '{0}'." -f $script:Configuration)

    # Check installation of required tools.
    CheckCommandAvailability "dotnet.exe" | Out-Null

    # call BuildSimulation.cmd
    Invoke-Expression "$script:SimulationPath/Factory/BuildSimulation.cmd -c --config $script:Configuration"

    # Provide other files
    Copy-Item -Force "$script:SimulationPath/Factory/Dockerfile" "$script:SimulationBuildOutputPath" | Out-Null
}

function CreateStationUrl
{
    Param(
        [Parameter(Mandatory=$true,Position=0)] $net,
        [Parameter(Mandatory=$true,Position=1)] $station
    )
    # Create a station uri from the station configuration
    $port = $station.Simulation.Port
    if ($port -eq $null) { $port = "51210" }
    $opcUrl = "opc.tcp://" + $station.Simulation.Id.ToLower() + "." + $net + ":" + $port + "/UA/" + $station.Simulation.Path
    return $opcUrl
}

function CreateProductionLineStationUrl
{
    Param(
        [Parameter(Mandatory=$true,Position=0)] $productionLine,
        [Parameter(Mandatory=$true,Position=1)] $type
    )
    $station = ($productionLine.Stations | where { $_.Simulation.Type -eq $type})
    return CreateStationUrl -net $productionLine.Simulation.Network.ToLowerInvariant() -station $station
}

function UpdateBrowserEndpoints()
{
    # Use a copy of the original to patch
    $originalFileName = "$script:IoTSuiteRootPath\WebApp\OPC.Ua.SampleClient.Endpoints.xml"
    $applicationFileName = "$script:IoTSuiteRootPath\WebApp\OPC.Ua.Browser.Endpoints.xml"
    Copy-Item $originalFileName $applicationFileName -Force
    # Patch the endpoint configuration file. Grab a node we import into the patched file
    $xml = [xml] (Get-Content $originalFileName)
    $configuredEndpoint = $xml.ConfiguredEndpointCollection.Endpoints.ChildNodes[0]

    $xml = [xml] (Get-Content $applicationFileName)
    $content = Get-Content -raw $script:TopologyDescription
    $json = ConvertFrom-Json -InputObject $content
    $productionLines = ($json | select -ExpandProperty Factories | select -ExpandProperty ProductionLines)
    foreach($productionLine in $productionLines)
    {
        $configuredEndpoint.Endpoint.EndpointUrl = CreateProductionLineStationUrl -productionLine $productionLine -type "Assembly"
        $child = $xml.ImportNode($configuredEndpoint, $true)
        $xml.ConfiguredEndpointCollection.Endpoints.AppendChild($child) | Out-Null

        $configuredEndpoint.Endpoint.EndpointUrl = CreateProductionLineStationUrl -productionLine $productionLine -type "Test"
        $child = $xml.ImportNode($configuredEndpoint, $true)
        $xml.ConfiguredEndpointCollection.Endpoints.AppendChild($child) | Out-Null

        $configuredEndpoint.Endpoint.EndpointUrl = CreateProductionLineStationUrl -productionLine $productionLine -type "Packaging"
        $child = $xml.ImportNode($configuredEndpoint, $true)
        $xml.ConfiguredEndpointCollection.Endpoints.AppendChild($child) | Out-Null
    }
 
    # Remove the entry with localhost (original template)
    $nodes = $xml.ConfiguredEndpointCollection.Endpoints.ChildNodes
    for ($i=0; $i -lt $nodes.Count; )
    {
        if ($nodes[$i].Endpoint.EndpointUrl -like "*localhost*")
        {
            $xml.ConfiguredEndpointCollection.Endpoints.RemoveChild($nodes[$i]) | Out-Null
        }
        else
        {
            $i++
        }
    }

    $xml.Save($applicationFileName)
}

Function Build()
{
    # Check installation of required tools.
    CheckCommandAvailability "msbuild.exe" | Out-Null

    # Restore packages.
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Restoring nuget packages for solution.")
    Invoke-Expression ".nuget/nuget restore ./Connectedfactory.sln"
    if ($LASTEXITCODE -ne 0)
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Restoring nuget packages for solution failed.")
        throw "Restoring nuget packages for solution failed."
    }
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Restoring dotnet packages for solution.")
    Invoke-Expression "dotnet restore"
    if ($LASTEXITCODE -ne 0)
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Restoring dotnet packages for solution failed.")
        throw "Restoring dotnet packages for solution failed."
    }

    # Enforce WebApp admin mode if requested via environment.
    if (-not [string]::IsNullOrEmpty($env:EnforceWebAppAdminMode))
    {
        $script:EnforceWebAppAdminMode = '/p:DefineConstants="GRANT_FULL_ACCESS_PERMISSIONS"'
    }

    # Build the solution.
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Building Connectedfactory.sln for configuration '{0}'." -f $script:Configuration)
    Invoke-Expression "msbuild Connectedfactory.sln /v:m /p:Configuration=$script:Configuration $script:EnforceWebAppAdminMode"
    if ($LASTEXITCODE -ne 0)
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Building Connectedfactory.sln failed.")
        throw "Building Connectedfactory.sln failed."
    }
}

Function Package()
{
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Packaging for configuration '{0}'." -f $script:Configuration)

    # Check installation of required tools.
    CheckCommandAvailability "msbuild.exe" | Out-Null

    Invoke-Expression "msbuild $script:IotSuiteRootPath/WebApp/WebApp.csproj /v:m /T:Package /p:Configuration=$script:Configuration"
    if ($LASTEXITCODE -ne 0)
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Building WebApp.csproj failed.")
        throw "Building Webapp.csproj failed."
    }

    $root = "$script:IotSuiteRootPath";
    $webPackage = "$root/WebApp/obj/$script:Configuration/package/WebApp.zip";
    $packageDir = "$root/Build_Output/$script:Configuration/package";

    Write-Host 'Cleaning up previously generated packages';
    if ((Test-Path "$packageDir/WebApp.zip") -eq $true) 
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Remove WebApp '{0}/WebApp.zip'" -f $packageDir)
        Remove-Item -Force "$packageDir/WebApp.zip" 2> $null
    }

    if ((Test-Path "$webPackage") -ne $true) 
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Failed to find WebApp package in directory '{0}'" -f $webPackage)
        throw "Failed to find package for the WebApp."
    }

    if (((Test-Path "$packageDir") -ne $true)) 
    {
        Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Creating package directory '{0}'" -f $packageDir)
        New-Item -Path "$packageDir" -ItemType Directory | Out-Null
    }

    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Copying packages to package directory '{0}'" -f $packageDir)
    Copy-Item $webPackage -Destination $packageDir | Out-Null
}

Function UploadFileToContainerBlob()
{
    Param(
        [Parameter(Mandatory=$true,Position=0)] [string] $filePath,
        [Parameter(Mandatory=$true,Position=1)] [string] $storageAccountName,
        [Parameter(Mandatory=$true,Position=2)] [string] $containerName,
        [Parameter(Mandatory=$true,Position=3)] [bool] $secure
    )

    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Upload file from '{0}' to storage account '{1}' in resource group '{2} as container '{3}' (secure: {4})" -f $filePath, $storageAccountName, $script:ResourceGroupName, $containerName, $secure)
    $containerName = $containerName.ToLowerInvariant()
    $file = Get-Item -Path "$filePath"
    $fileName = $file.Name
    
    $storageAccountKey = (Get-AzureRmStorageAccountKey -StorageAccountName $storageAccountName -ResourceGroupName $script:ResourceGroupName).Value[0]
    $context = New-AzureStorageContext -StorageAccountName $storageAccountName -StorageAccountKey $storageAccountKey
    $maxTries = $MAX_TRIES
    if (!(HostEntryExists $context.StorageAccount.BlobEndpoint.Host))
    {
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Waiting for storage account '{0} to resolve." -f $context.StorageAccount.BlobEndpoint.Host)
        while (!(HostEntryExists $context.StorageAccount.BlobEndpoint.Host) -and $maxTries-- -gt 0)
        {
            Write-Progress -Activity "Resolving storage account endpoint" -Status "Resolving" -SecondsRemaining ($maxTries*$SECONDS_TO_SLEEP)
            ClearDnsCache
            sleep $SECONDS_TO_SLEEP
        }
    }
    New-AzureStorageContainer $ContainerName -Permission Off -Context $context -ErrorAction SilentlyContinue | Out-Null
    # Upload the file
    Set-AzureStorageBlobContent -Blob $fileName -Container $ContainerName -File $file.FullName -Context $context -Force | Out-Null

    # Generate Uri with sas token
    $storageAccount = [Microsoft.WindowsAzure.Storage.CloudStorageAccount]::Parse(("DefaultEndpointsProtocol=https;EndpointSuffix={0};AccountName={1};AccountKey={2}" -f $script:AzureEnvironment.StorageEndpointSuffix, $storageAccountName, $storageAccountKey))
    $blobClient = $storageAccount.CreateCloudBlobClient()
    $container = $blobClient.GetContainerReference($containerName)
    if ($container -ne $null)
    {
        $maxTries = $MAX_TRIES
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Checking container '{0}'." -f $containerName) 
        while (!$container.Exists())
        {
            Write-Progress -Activity "Resolving storage account endpoint" -Status "Checking" -SecondsRemaining ($maxTries*$SECONDS_TO_SLEEP)
            sleep $SECONDS_TO_SLEEP
            if ($maxTries-- -le 0)
            {
                Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Timed out waiting for container: {0}" -f $ContainerName)
                throw ("Timed out waiting for container: {0}" -f $ContainerName)
            }
        }
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Checking blob '{0}'." -f $fileName) 
        $blob = $container.GetBlobReference($fileName)
        if ($blob -ne $null)
        {
            $maxTries = $MAX_TRIES
            while (!$blob.Exists())
            {
                Write-Progress -Activity "Checking Blob existence" -Status "Checking" -SecondsRemaining ($maxTries*$SECONDS_TO_SLEEP)
                sleep $SECONDS_TO_SLEEP
                if ($maxTries-- -le 0)
                {
                    Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Timed out waiting for blob '{0}'" -f $fileName)
                    throw ("Timed out waiting for blob: {0}" -f $fileName)
                }
            }
        }
        else
        {
            Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Cannot find blob for file with name '{0}'" -f $fileName)
        }
    }
    else
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Cannot find container with name '{0}'" -f $containerName)
    }

    if ($secure)
    {
        $sasPolicy = New-Object Microsoft.WindowsAzure.Storage.Blob.SharedAccessBlobPolicy
        $sasPolicy.SharedAccessStartTime = [System.DateTime]::Now.AddMinutes(-5)
        $sasPolicy.SharedAccessExpiryTime = [System.DateTime]::Now.AddHours(24)
        $sasPolicy.Permissions = [Microsoft.WindowsAzure.Storage.Blob.SharedAccessBlobPermissions]::Read
        $sasToken = $blob.GetSharedAccessSignature($sasPolicy)
    }
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Blob URI is '{0}'" -f $blob.Uri.ToString() + $sasToken)
    return $blob.Uri.ToString() + $sasToken
}

Function FinalizeWebPackages
{
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Uploading packages")

    # Set path correct
    $script:WebAppLocalPath = "$script:IoTSuiteRootPath/WebApp/obj/{0}/Package/WebApp.zip" -f $script:Configuration

    # Update browser endpoints
    UpdateBrowserEndpoints

    # Upload WebApp package
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Fix WebApp package")
    FixWebAppPackage $script:WebAppLocalPath
}

function RecordVmCommand
{
    Param(
        [Parameter(Mandatory=$true)] $command,
        [Switch] $initScript,
        [Switch] $deleteScript,
        [Switch] $startScript,
        [Switch] $stopScript
    )
    
    if ($initScript -eq $false -and $deleteScript -eq $false -and $startScript -eq $false-and $stopScript -eq $false)
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - No switch set. Please check usage.")
        throw ("No switch set. Please check usage.")
    }
    if ($initScript)
    {
        # Add this to the init script.
        Add-Content -path "$script:SimulationBuildOutputInitScript" -Value "$command `n" -NoNewline
    }
    if ($deleteScript)
    {
        # Add this to the delete script.
        Add-Content -path "$script:SimulationBuildOutputDeleteScript" -Value "$command `n" -NoNewline
    }
    if ($startScript)
    {
        # Add this to the start script.
        Add-Content -path "$script:SimulationBuildOutputStartScript" -Value "$command `n" -NoNewline
    } 
    if ($stopScript)
    {
        # Add this to the stop script.
        Add-Content -path "$script:SimulationBuildOutputStopScript" -Value "$command `n" -NoNewline
    } 
}

function StartStation
{
    Param(
        [Parameter(Mandatory=$true)] $net,
        [Parameter(Mandatory=$true)] $station
    )

    # Create the instance name
    $containerInstance = "Station." + $station.Simulation.Id + "." + $net

    # Create logs directory in the build output. They are copied to their final place in the VM by the init script
    # Each station needs a unique logs folder to avoid race conditions in the volume driver
    if (-not (Test-Path "$script:SimulationBuildOutputPath/$script:DockerLogsFolder/$containerInstance")) 
    {
        New-Item -Path "$script:SimulationBuildOutputPath/$script:DockerLogsFolder/$containerInstance" -ItemType "Directory" | Out-Null
    }

    # Set simulation variables.
    $hostName = $station.Simulation.Id.ToLower() + "." + $net
    $port = $station.Simulation.Port
    $defaultPort = 51210
    if ($port -eq $null)
    { 
        Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - For station '{0}' there was no port configured. Using default port 51210.." -f $containerInstance, $defaultPort)
        $port = "$defaultPort" 
    }

    # Disconnect from network on stop and delete.
    $vmCommand = "docker network disconnect -f $net $hostName"
    RecordVmCommand -command $vmCommand -stopScript -deleteScript

    # Start the station
    $stationUri = (CreateStationUrl -net $net -station $station)
    $commandLine = "../buildOutput/Station.dll " + $station.Simulation.Id + " " + $stationUri.ToLower() + " " + $station.Simulation.Args
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Start Docker container for station node $hostName ...")
    $volumes = "-v $script:DockerRoot/$($script:DockerSharedFolder):/app/$script:DockerSharedFolder "
    $volumes +="-v $script:DockerRoot/$script:DockerLogsFolder/$($containerInstance):/app/$script:DockerLogsFolder"
    $vmCommand = "docker run -itd $volumes -w /app/buildOutput --name $hostName -h $hostName --network $net --restart always --expose $port simulation:latest $commandLine"
    RecordVmCommand -command $vmCommand -startScript
    $vmCommand = "sleep 5s"
    RecordVmCommand -command $vmCommand -startScript
}

function StartMES
{
    Param(
        [Parameter(Mandatory=$true)] $net,
        [Parameter(Mandatory=$true)] $productionLine
    )

    # Create the instance name
    $containerInstance = "MES." + $productionLine.Simulation.Mes + "." + $net

    # Create config and logs directory in the build output. They are copied to their final place in the VM by the init script
    if (-not (Test-Path "$script:SimulationBuildOutputPath/$script:DockerConfigFolder/$containerInstance")) 
    {
        New-Item -Path "$script:SimulationBuildOutputPath/$script:DockerConfigFolder/$containerInstance" -ItemType "Directory" | Out-Null
    }
    if (-not (Test-Path "$script:SimulationBuildOutputPath/$script:DockerLogsFolder/$containerInstance")) 
    {
        New-Item -Path "$script:SimulationBuildOutputPath/$script:DockerLogsFolder/$containerInstance" -ItemType "Directory" | Out-Null
    }

    # Create unique configuration for this production line's MES
    $originalFileName = "$script:SimulationPath/Factory/MES/Opc.Ua.MES.Endpoints.xml"
    $applicationFileName = "$script:SimulationBuildOutputPath/Opc.Ua.MES.Endpoints.xml"
    Copy-Item $originalFileName $applicationFileName -Force

    # Patch the endpoint configuration file
    $xml = [xml] (Get-Content $applicationFileName)
    $configuredEndpoints = $xml.ConfiguredEndpointCollection.Endpoints.ChildNodes
    $configuredEndpoints[0].Endpoint.EndpointUrl = (CreateProductionLineStationUrl -productionLine $productionLine -type "Assembly")
    $configuredEndpoints[1].Endpoint.EndpointUrl = (CreateProductionLineStationUrl -productionLine $productionLine -type "Test")
    $configuredEndpoints[2].Endpoint.EndpointUrl = (CreateProductionLineStationUrl -productionLine $productionLine -type "Packaging")
    $xml.Save($applicationFileName)

    # Copy the endpoint configuration file
    Copy-Item -Path $applicationFileName -Destination "$script:SimulationBuildOutputPath/$script:DockerConfigFolder/$containerInstance"

    # Patch the application configuration file
    $originalFileName = "$script:SimulationPath/Factory/MES/Opc.Ua.MES.Config.xml"
    $applicationFileName = "$script:SimulationBuildOutputPath/Opc.Ua.MES.Config.xml"
    Copy-Item $originalFileName $applicationFileName -Force
    $xml = [xml] (Get-Content $applicationFileName)
    $xml.ApplicationConfiguration.SecurityConfiguration.ApplicationCertificate.SubjectName = $containerInstance
    $xml.Save($applicationFileName)

    # Copy the application configuration file
    Copy-Item -Path $applicationFileName -Destination "$script:SimulationBuildOutputPath/$script:DockerConfigFolder/$containerInstance"

    # Set MES hostname.
    $hostName = $productionLine.Simulation.Mes.ToLower() + "." + $net
    
    # Disconnect from network on stop and delete.
    $vmCommand = "docker network disconnect -f $net $hostName"
    RecordVmCommand -command $vmCommand -stopScript -deleteScript

    # Start MES.
    $commandLine = "../buildOutput/MES.dll"
    Write-Output("$(Get-Date –f $TIME_STAMP_FORMAT) - Start Docker container for MES node $hostName ...");
    $volumes = "-v $script:DockerRoot/$($script:DockerSharedFolder):/app/$script:DockerSharedFolder "
    $volumes += "-v $script:DockerRoot/$script:DockerLogsFolder/$($containerInstance):/app/$script:DockerLogsFolder "
    $volumes += "-v $script:DockerRoot/$script:DockerConfigFolder/$($containerInstance):/app/$script:DockerConfigFolder"
    $vmCommand = "docker run -itd $volumes -w /app/Config --name $hostName -h $hostName --network $net --restart always simulation:latest $commandLine"
    RecordVmCommand -command $vmCommand -startScript
    $vmCommand = "sleep 10s"
    RecordVmCommand -command $vmCommand -startScript
}

function StartProxy
{
    Param(
        [Parameter(Mandatory=$true)] $net
    )

    # Start proxy container in the VM and link to simulation container
    $hostName = "proxy." + $net

    # Disconnect from network on stop and delete.
    $vmCommand = "docker network disconnect -f $net $hostName"
    RecordVmCommand -command $vmCommand -stopScript -deleteScript

    # Start proxy.
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Start Docker container for Proxy node $hostName ...")
    $vmCommand = "docker run -itd -v $script:DockerRoot/$($script:DockerLogsFolder):/app/$script:DockerLogsFolder --name $hostName -h $hostName --network $net --restart always " + '$DOCKER_PROXY_REPO:$DOCKER_PROXY_VERSION ' + "-c " + '"$IOTHUB_CONNECTIONSTRING" ' + "-l /app/$script:DockerLogsFolder/proxy1.$net.log "
    RecordVmCommand -command $vmCommand -startScript
    $vmCommand = "sleep 5s"
    RecordVmCommand -command $vmCommand -startScript
}

function StartGWPublisher
{
    Param(
        [Parameter(Mandatory=$true)] $net,
        [Parameter(Mandatory=$true)] $topologyJson
    )

    # Create the instance name.
    $hostName = "publisher." + $net
    $port = "62222"
    
    # Create config and logs directory in the build output. They are copied to their final place in the VM by the init script.
    if (-not (Test-Path "$script:SimulationBuildOutputPath/$script:DockerConfigFolder/$hostName")) 
    {
        New-Item -Path "$script:SimulationBuildOutputPath/$script:DockerConfigFolder/$hostName" -ItemType "Directory" | Out-Null
    }
    if (-not (Test-Path "$script:SimulationBuildOutputPath/$script:DockerLogsFolder/$hostName")) 
    {
        New-Item -Path "$script:SimulationBuildOutputPath/$script:DockerLogsFolder/$hostName" -ItemType "Directory" | Out-Null
    }

    # Create the published nodes file for all production lines of the factory network the publisher is on (from the topology JSON file)
    New-Item "$script:SimulationBuildOutputPath/$script:DockerConfigFolder/$hostName/publishednodes.JSON" -type file -force | Out-Null
    $publishedNodesFileName = "$script:SimulationBuildOutputPath/$script:DockerConfigFolder/$hostName/publishednodes.JSON"
    $jsonOut = New-Object System.Collections.ArrayList($null)
    $factories = ($topologyJson | Select -ExpandProperty Factories)
    $productionLines = ($factories | Select -ExpandProperty ProductionLines)
    foreach($productionLine in $productionLines)
    {
        if ($productionLine.Simulation.Network -eq $net)
        {
            $stations = ($productionLine | Select -ExpandProperty Stations)
            foreach($station in $stations)
            {
                $url = CreateProductionLineStationUrl -productionLine $productionLine -type $station.Simulation.Type
                $opcnodes = ($station | Select -ExpandProperty OpcNodes)
                foreach($opcnode in $opcnodes)
                {
                    if((-not [string]::IsNullOrEmpty($opcnode.NodeId)))
                    {
                        $identifier = (New-Object PSObject | Add-Member -PassThru NoteProperty 'Identifier' $opcnode.NodeId)
                        $entry = (New-Object PSObject | Add-Member -PassThru NoteProperty 'EndpointUrl' $url)
                        $entry | Add-Member -PassThru NoteProperty 'NodeId' $identifier
                        $jsonOut.Add($entry)
                    }
                }
            }
        }
    }

    # Save the published nodes file
    $jsonOut | ConvertTo-Json -depth 100 | Out-File $publishedNodesFileName

    # Disconnect from network on stop and delete.
    $vmCommand = "docker network disconnect -f $net $hostName"
    RecordVmCommand -command $vmCommand -stopScript -deleteScript

    # Start GW Publisher container in the VM and link to simulation container
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Start Docker container for GW Publisher node $hostName ...")
    $volumes = "-v $script:DockerRoot/$($script:DockerSharedFolder):/app/$script:DockerSharedFolder "
    $volumes += "-v $script:DockerRoot/$script:DockerLogsFolder/$($hostName):/app/$script:DockerLogsFolder "
    $volumes += "-v $script:DockerRoot/$script:DockerConfigFolder/$($hostName):/app/$script:DockerConfigFolder"

    $vmCommand = "docker run -itd $volumes --name $hostName -h $hostName --network $net --expose $port --restart always -e _GW_PNFP=`'/app/$script:DockerConfigFolder/publishednodes.JSON`' -e _TPC_SP=`'/app/Shared/CertificateStores/UA Applications`' -e _GW_LOGP=`'/app/$script:DockerLogsFolder/$hostName.log.txt`' " + '$DOCKER_PUBLISHER_REPO:$DOCKER_PUBLISHER_VERSION ' + "$hostName " + '"$IOTHUB_CONNECTIONSTRING"'
    RecordVmCommand -command $vmCommand -startScript
}

function SimulationBuildScripts
{
    # Initialize init script
    Set-Content -Path "$script:SimulationBuildOutputInitScript" -Value "#!/bin/bash `n" -NoNewline
    # Unpack the simulation files
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "chmod +x simulation `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "tar -xjvf simulation -C $script:DockerRoot `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "rm simulation `n" -NoNewline
    # Put Config, Logs and Shared folders to final destination
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "cp -r $script:DockerRoot/buildOutput/$script:DockerConfigFolder $script:DockerRoot `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "cp -r $script:DockerRoot/buildOutput/$script:DockerLogsFolder $script:DockerRoot `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "cp -r $script:DockerRoot/buildOutput/$script:DockerSharedFolder $script:DockerRoot `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "cp $script:DockerRoot/buildOutput/startsimulation $script:DockerRoot `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "chmod +x $script:DockerRoot/startsimulation `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "cp $script:DockerRoot/buildOutput/deletesimulation $script:DockerRoot `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "chmod +x $script:DockerRoot/deletesimulation `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "cp $script:DockerRoot/buildOutput/stopsimulation $script:DockerRoot `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "chmod +x $script:DockerRoot/stopsimulation `n" -NoNewline
    # Bring the public key in place.
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "if [ `"`$2`" != `"`" ] && [ -e ../../../`$2.crt ] `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "then `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "    cp ../../../`$2.crt `"$script:DockerRoot/$script:DockerCertsFolder/$script:UaSecretBaseName.der`" `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "fi `n" -NoNewline

    # Initialize start script
    Set-Content -Path "$script:SimulationBuildOutputStartScript" -Value "#!/bin/bash `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputStartScript" -Value "cd $script:DockerRoot/buildOutput `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputStartScript" -Value "export DOCKER_PROXY_REPO=`"$script:DockerProxyRepo`" `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputStartScript" -Value "export DOCKER_PROXY_VERSION=`"$script:DockerProxyVersion`" `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputStartScript" -Value "export DOCKER_PUBLISHER_REPO=`"$script:DockerPublisherRepo`" `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputStartScript" -Value "export DOCKER_PUBLISHER_VERSION=`"$script:DockerPublisherVersion`" `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputStartScript" -Value "if [ `"`$IOTHUB_CONNECTIONSTRING`" == `"`" ] `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputStartScript" -Value "then `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputStartScript" -Value "    echo `"Please make sure that the environment variable IOTHUB_CONNECTIONSTRING is defined.`" `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputStartScript" -Value "    exit `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputStartScript" -Value "fi `n" -NoNewline

    # Initialize delete script
    Set-Content -Path "$script:SimulationBuildOutputDeleteScript" -Value "#!/bin/bash `n" -NoNewline

    # Initialize stop script
    Set-Content -Path "$script:SimulationBuildOutputStopScript" -Value "#!/bin/bash `n" -NoNewline

    # Create shared folder in the build output. It will copied to its final place in the VM by the init script
    if (-not (Test-Path "$script:SimulationBuildOutputPath/$script:DockerSharedFolder")) 
    {
        New-Item -Path "$script:SimulationBuildOutputPath/$script:DockerSharedFolder" -ItemType "Directory" | Out-Null
    }

    try
    {
        # The initialization created the buildOutput folder with all content.
        $vmCommand = "cd $script:DockerRoot/buildOutput"
        RecordVmCommand -command $vmCommand -initScript
        $vmCommand = 'docker build -t simulation:latest .'
        RecordVmCommand -command $vmCommand -initScript

        # Pull proxy image from docker hub.
        $vmCommand = "docker pull $script:DockerProxyRepo:$script:DockerProxyVersion"
        RecordVmCommand -command $vmCommand -initScript

        # Pull GW Publisher image from docker hub.
        $vmCommand = "docker pull $script:DockerPublisherRepo:$script:DockerPublisherVersion"
        RecordVmCommand -command $vmCommand -initScript

        # Put UA Web Client public cert in place
        if (-not (Test-Path "$script:SimulationBuildOutputPath/$script:DockerCertsFolder")) 
        {
            New-Item -Path "$script:SimulationBuildOutputPath/$script:DockerCertsFolder" -ItemType "Directory" -Force | Out-Null
        }

        # Create a cert if we do not have one from a previous build.
        if (-not (Test-Path "$script:CreateCertsPath/certs/$script:DeploymentName/$script:UaSecretBaseName.der"))
        {
            Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Create certificate to secure OPC communication.");
            Invoke-Expression "dotnet run -p $script:CreateCertsPath/CreateCerts.csproj $script:CreateCertsPath `"UA Web Client`" `"urn:localhost:Contoso:FactorySimulation:UA Web Client`""
            New-Item -Path "$script:CreateCertsPath/certs/$script:DeploymentName" -ItemType "Directory" -Force | Out-Null
            Move-Item "$script:CreateCertsPath/certs/$script:UaSecretBaseName.der" "$script:CreateCertsPath/certs/$script:DeploymentName/$script:UaSecretBaseName.der" -Force | Out-Null
            New-Item -Path "$script:CreateCertsPath/private/$script:DeploymentName" -ItemType "Directory" -Force | Out-Null
            Move-Item "$script:CreateCertsPath/private/$script:UaSecretBaseName.pfx" "$script:CreateCertsPath/private/$script:DeploymentName/$script:UaSecretBaseName.pfx" -Force | Out-Null
            
            if ($script:Command -eq "local")
            {
                # For a local build, we install the pfx into our local cert store.
                Import-PfxCertificate -FilePath "$script:CreateCertsPath/private/$script:DeploymentName/$script:UaSecretBaseName.pfx" -CertStoreLocation cert:\CurrentUser\My -Password (ConvertTo-SecureString -String $script:UaSecretPassword -Force –AsPlainText)
            }
        }
        else
        {
            Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Using existing certificate for deployment '{0}'" -f $script:DeploymentName);
        }
        Copy-Item "$script:CreateCertsPath/certs/$script:DeploymentName/$script:UaSecretBaseName.der" "$script:SimulationBuildOutputPath/$script:DockerCertsFolder" -Force | Out-Null

        # Start simulation based on topology configuration in ContosoTopologyDescription.json.
        $content = Get-Content -raw $script:TopologyDescription
        $json = ConvertFrom-Json -InputObject $content

        $factories = ($json | Select -ExpandProperty Factories)
        $productionLines = ($factories | select -ExpandProperty ProductionLines)
        $networks = ($productionLines | select -ExpandProperty Simulation | select net -ExpandProperty Network -Unique)
        foreach($network in $networks)
        {
            # Create bridge network in vm and start proxy.
            $net = $network.ToLower()
            Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Create network $net ...");
            $vmCommand = "docker network create -d bridge -o 'com.docker.network.bridge.enable_icc'='true' $net"
            RecordVmCommand -command $vmCommand -initScript
            StartProxy -net $net
        }

        foreach($productionLine in $productionLines)
        {
            # Start production lines.
            $net = $productionline.Simulation.Network.ToLower()

            Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Create production line " + $productionline.Simulation.Id + " on network $net ...")

            StartStation -net $net -station ($productionLine.Stations | where { $_.Simulation.Type -eq "Assembly"})
            StartStation -net $net -station ($productionLine.Stations | where { $_.Simulation.Type -eq "Test"})
            StartStation -net $net -station ($productionLine.Stations | where { $_.Simulation.Type -eq "Packaging"})

            StartMES -net $net -productionLine $productionLine
                
            Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Production line " + $productionline.Simulation.Id + " complete!")
        }

        foreach($network in $networks)
        {
            # Start publisher
            $net = $network.ToLower()
            StartGWPublisher -net $net -topologyJson $json
        }

        # Remove the networks on delete.
        foreach($network in $networks)
        {
            $net = $network.ToLower()
            Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Remove network $net ...");
            $vmCommand = "docker network rm $net"
            RecordVmCommand -command $vmCommand -deleteScript
        }
    }
    catch
    {
        throw $_
    }
    # Now the init script is recorded completely and a few last things to do is to fix ownership.
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "sudo chown -R docker:docker $script:DockerRoot `n" -NoNewline
    # The certs folder and all certs should be still owned by root.
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "sudo chown -R root:root `"$script:DockerRoot/$script:DockerCertsFolder`" `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "sudo chmod u+x `"$script:DockerRoot/$script:DockerCertsFolder/$script:UaSecretBaseName.der`" `n" -NoNewline
    # Start the simulation.
    Add-Content -Path "$script:SimulationBuildOutputInitScript" -Value "sudo bash -c `'export IOTHUB_CONNECTIONSTRING=`"`$0`"; $script:DockerRoot/startsimulation`' `$1 &`n" -NoNewline

    # To delete, we remove the build output. all mapped folders and stop all containers.
    Add-Content -Path "$script:SimulationBuildOutputDeleteScript" -Value "sudo rm -r $script:DockerRoot/$script:DockerSharedFolder `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputDeleteScript" -Value "sudo rm -r $script:DockerRoot/$script:DockerLogsFolder `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputDeleteScript" -Value "sudo rm -r $script:DockerRoot/$script:DockerConfigFolder `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputDeleteScript" -Value "sudo rm -r $script:DockerRoot/buildOutput `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputDeleteScript" -Value "if [ `$(docker ps -a -q | wc -l) -gt 0 ] `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputDeleteScript" -Value "then `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputDeleteScript" -Value "    docker stop `$(docker ps -a -q) `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputDeleteScript" -Value "    docker rm -f `$(docker ps -a -q) `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputDeleteScript" -Value "fi `n" -NoNewline

    # To stop, we just stop all docker containers.
    Add-Content -Path "$script:SimulationBuildOutputStopScript" -Value "if [ `$(docker ps -a -q | wc -l) -gt 0 ] `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputStopScript" -Value "then `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputStopScript" -Value "    docker stop `$(docker ps -a -q) `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputStopScript" -Value "    docker rm `$(docker ps -a -q) `n" -NoNewline
    Add-Content -Path "$script:SimulationBuildOutputStopScript" -Value "fi `n" -NoNewline
}

Function GetOwnerObjectId()
{
    $result = (Get-AzureRmADUser -UPN $script:AzureAccountName).Id
    if ([string]::IsNullOrEmpty($result))
    {
        # find owner in the subscription directory
        $searchuser = ($script:AzureAccountName -replace '@','_') + '#EXT#*'
        $result = (Get-AzureRmAdUser | Where-Object {($_.UserPrincipalName -like $searchuser)}).Id
        if ([string]::IsNullOrEmpty($result))
        {
            # not found, but fill with UPN to avoid deployment error
            $result = $script:AzureAccountName
            Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Owner {0} object id not found" -f $result);
        }
    }
    return $result
}

function SimulationUpdate
{
    # Check that we have a good config.
    $iotHubOwnerConnectionString = GetEnvSetting "IotHubOwnerConnectionString"
    if ([string]::IsNullOrEmpty($iotHubOwnerConnectionString))
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - The configuration file '{0}.config.user' does not contain a vaild 'IotHubOwnerConnectionString'. Pls check." -f $script:DeploymentName)
        throw ("The configuration file '{0}.config.user' does not contain a vaild 'IotHubOwnerConnectionString'. Pls check." -f $script:DeploymentName)
    }

    # Find VM 
    try 
    {
        $vmResource = Get-AzureRmResource -ResourceName $script:VmName -ResourceType Microsoft.Compute/virtualMachines -ResourceGroupName $script:ResourceGroupName
    }
    catch 
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Can not update the simulation, because no VM with name '{0}' found in resource group '{1}'" -f $script:VmName, $script:ResourceGroupName)
        throw ("Can not update the simulation, because no VM with name '{0}' found in resource group '{1}'" -f $script:VmName, $script:ResourceGroupName)
    }

    $removePublicIp = $false
    try
    {
        $vmPublicIp = Get-AzureRmPublicIpAddress -Name $script:VmName -ResourceGroupName $script:ResourceGroupName -ErrorAction SilentlyContinue
    }
    catch 
    {
    }

    if ($vmPublicIp -eq $null)
    {
        # Add a public IP address to the VM
        Invoke-Expression "$script:SimulationPath/Factory/Add-SimulationPublicIp -DeploymentName $script:DeploymentName"
        $removePublicIp = $true
    }

    try
    {
        # Create a PSCredential object for SSH
        $securePassword = ConvertTo-SecureString $script:VmAdminPassword -AsPlainText -Force
        $sshCredentials = New-Object System.Management.Automation.PSCredential ($script:VmAdminUsername, $securePassword)

        # Create SSH session
        $ipAddress = Get-AzureRmPublicIpAddress -Name $script:VmName -ResourceGroupName $script:ResourceGroupName
        Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - IP address of VM is '{0}'" -f $ipAddress.IpAddress)
        $session = New-SSHSession $ipAddress.IpAddress -Credential $sshCredentials -AcceptKey -ConnectionTimeout ($script:SshTimeout * 1000)
        if ($Session -eq $null)
        {
            Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Cannot create SSH session to VM '{0}'" -f  $ipAddress.IpAddress)
            throw ("Cannot create SSH session to VM '{0}'" -f  $ipAddress.IpAddress)
        }
        try
        {
            # Upload delete script and delete simulation.
            Set-SCPFile -LocalFile "$script:SimulationBuildOutputDeleteScript" -RemotePath $script:DockerRoot -ComputerName $ipAddress.IpAddress -Credential $sshCredentials -NoProgress -OperationTimeout ($script:SshTimeout * 3)
            Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Delete simulation.")
            $vmCommand = "chmod +x $script:DockerRoot/deletesimulation"
            $status = Invoke-SSHCommand -Sessionid $session.SessionId -TimeOut ($script:SshTimeout * 5) -Command $vmCommand
            if ($status.ExitStatus -ne 0)
            {
                Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Failed to run $vmCommand in VM : $status")
                throw ("Failed to run $vmCommand in VM : $status")
            }
            $vmCommand = "$script:DockerRoot/deletesimulation `&> $script:DockerRoot/deletesimulation.log"
            # Ignore the status of the delete script. Check the log file in the VM for details.
            Invoke-SSHCommand -Sessionid $session.SessionId -TimeOut $script:SshTimeout -Command $vmCommand | Out-Null

            # Copy compressed simulation binaries and scripts to VM
            Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Uupload simulation files to VM")
            Set-SCPFile -LocalFile "$script:SimulationPath/simulation" -RemotePath $script:DockerRoot -ComputerName $ipAddress.IpAddress -Credential $sshCredentials -NoProgress -OperationTimeout ($script:SshTimeout * 3)
            Set-SCPFile -LocalFile "$script:SimulationBuildOutputInitScript" -RemotePath $script:DockerRoot -ComputerName $ipAddress.IpAddress -Credential $sshCredentials -NoProgress -OperationTimeout ($script:SshTimeout * 3)
            $vmCommand = "chmod +x $script:DockerRoot/simulation"
            $status = Invoke-SSHCommand -Sessionid $session.SessionId -TimeOut $script:SshTimeout -Command $vmCommand
            if ($status.ExitStatus -ne 0)
            {
                Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Failed to run $vmCommand in VM : $status")
                throw ("Failed to run $vmCommand in VM : $status")
            }
            # Initialize and start simulation.
            Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Initialize and run simulation. This may take a while...")
            $vmCommand = "chmod +x $script:DockerRoot/initsimulation"
            $status = Invoke-SSHCommand -Sessionid $session.SessionId -TimeOut $script:SshTimeout -Command $vmCommand
            if ($status.ExitStatus -ne 0)
            {
                Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Failed to run $vmCommand in VM : $status")
                throw ("Failed to run $vmCommand in VM : $status")
            }
            $iotHubOwnerConnectionString = GetEnvSetting "IotHubOwnerConnectionString"
            $vmCommand = "$script:DockerRoot/initsimulation `"$iotHubOwnerConnectionString`" `&> $script:DockerRoot/initsimulation.log"
            $status = Invoke-SSHCommand -Sessionid $session.SessionId -TimeOut (3*$script:SshTimeout) -Command $vmCommand
            if ($status.ExitStatus -ne 0)
            {
                Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Failed to run $vmCommand in VM : $status")
                throw ("Failed to run $vmCommand in VM : $status")
            } 
        }
        catch
        {
            throw $_
        }
        finally
        {
            # Remove SSH session
            Remove-SSHSession $session.SessionId | Out-Null
        }
    }
    finally
    {
        # Remove the public IP address from the VM if we added it.
        if ($removePublicIp -eq $true)
        {
            Invoke-Expression "$script:SimulationPath/Factory/Remove-SimulationPublicIp -DeploymentName $script:DeploymentName"
        }
    }
}


################################################################################################################################################################
#
# Start of script
#
################################################################################################################################################################

$VerbosePreference = "Continue"

# Constant definitions
$MAX_TRIES = 20
$SECONDS_TO_SLEEP=3
# Timestamp format as specified on http://msdn.microsoft.com/library/system.globalization.datetimeformatinfo.aspx
# u is ISO 8601 standard for coordinated universal time
$TIME_STAMP_FORMAT = "u"
$EXPECTED_PSCX_MODULE_VERSION = "3.2.2"
$EXPECTED_POSHSSH_MODULE_VERSION = "1.7.7"

# Variable initialization
$script:IoTSuiteRootPath = Split-Path $MyInvocation.MyCommand.Path
$script:SimulationPath = "$script:IoTSuiteRootPath/Simulation"
$script:CreateCertsPath = "$script:SimulationPath/Factory/CreateCerts"
$script:WebAppPath = "$script:IoTSuiteRootPath/WebApp"
$script:DeploymentConfigPath = "$script:IoTSuiteRootPath/Deployment"
$script:IotSuiteVersion = Get-Content ("{0}/VERSION.txt" -f $script:IoTSuiteRootPath)
# OptionIndex is at script level because of its use in certain expression blocks
$script:OptionIndex = 0;
# Timeout in seconds for SSH operations
$script:SshTimeout = 120
$script:SimulationBuildOutputPath = "$script:SimulationPath/Factory/buildOutput"
$script:SimulationBuildOutputInitScript = "$script:SimulationBuildOutputPath/initsimulation"
$script:SimulationBuildOutputDeleteScript = "$script:SimulationBuildOutputPath/deletesimulation"
$script:SimulationBuildOutputStartScript = "$script:SimulationBuildOutputPath/startsimulation"
$script:SimulationBuildOutputStopScript = "$script:SimulationBuildOutputPath/stopsimulation"
$script:SimulationConfigPath = "$script:SimulationBuildOutputPath/Config"

# Import and check installed Azure cmdlet version
$script:AzurePowershellVersionMajor = (Get-Module -ListAvailable -Name Azure).Version.Major
CheckModuleVersion PSCX $EXPECTED_PSCX_MODULE_VERSION
CheckModuleVersion Posh-SSH $EXPECTED_POSHSSH_MODULE_VERSION

# Validate command line semantic
if ($script:Command -eq "cloud" -or $script:Command -eq "delete" -and $script:DeploymentName -eq "local")
{
    Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Command '{0}' requires a 'DeploymentName' parameter. 'local' is not allowed as value for the 'DeploymentName' parameter. Use the 'local' command to b" -f $script:Command)
    throw ("Command '{0}' requires a 'DeploymentName' parameter" -f $script:Command)
}
if ($($script:Command -eq "local") -and ($script:DeploymentName -ne "local"))
{
    Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Command 'local' does not support use of the '-DeploymentName' parameter, but use a default name for the deployment.")
    throw ("Command 'local' does not support use of the '-DeploymentName' parameter, but use a default name for the deployment.")
}

# Install nuget if not there
InstallNuget

# Set deployment name
$script:DeploymentName = $script:DeploymentName.ToLowerInvariant()
Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Name of the deployment is '{0}'" -f $script:DeploymentName)

# Initialize available Azure Cloud locations
switch($script:AzureEnvironmentName)
{
    "AzureCloud" {
        if ((Get-AzureRMEnvironment AzureCloud) -eq $null)
        {
            Write-Verbose  "$(Get-Date –f $TIME_STAMP_FORMAT) - Can not find AzureCloud environment. Adding it."
            Add-AzureRMEnvironment –Name AzureCloud -EnableAdfsAuthentication $False -ActiveDirectoryServiceEndpointResourceId https://management.core.windows.net/ -GalleryUrl https://gallery.azure.com/ -ServiceManagementUrl https://management.core.windows.net/ -SqlDatabaseDnsSuffix .database.windows.net -StorageEndpointSuffix core.windows.net -ActiveDirectoryAuthority https://login.microsoftonline.com/ -GraphUrl https://graph.windows.net/ -trafficManagerDnsSuffix trafficmanager.net -AzureKeyVaultDnsSuffix vault.azure.net -AzureKeyVaultServiceEndpointResourceId https://vault.azure.net -ResourceManagerUrl https://management.azure.com/ -ManagementPortalUrl http://go.microsoft.com/fwlink/?LinkId=254433
        }

        # Initialize public cloud suffixes.
        $script:IotHubSuffix = "azure-devices.net"
        $script:WebsiteSuffix = "azurewebsites.net"
        $script:RdxSuffix = "timeseries.azure.com"

        # Set locations were all resource are available. This might need to get updated if resources are deployed to more locations.
        $script:AzureLocations = @("West US", "North Europe", "West Europe")
    }
    default {throw ("'{0}' is not a supported Azure Cloud environment" -f $script:AzureEnvironmentName)}
}
$script:AzureEnvironment = Get-AzureEnvironment $script:AzureEnvironmentName

# Set environment specific variables.
if ($script:DeploymentName -eq "local")
{
    $script:SuiteName = $env:USERNAME + "ConnfactoryLocal"
    $script:SuiteType = "Connectedfactory"
    $script:WebAppHomepage = "https://localhost:44305/"
    $script:CloudDeploy = $false
}
else
{
    $script:SuiteName = $script:DeploymentName
    $script:SuiteType = "Connectedfactory"
    $script:WebAppHomepage = "https://{0}.{1}/" -f $script:DeploymentName, $script:WebsiteSuffix
    $script:CloudDeploy = $true
}
$script:WebAppIdentifierUri = $script:WebAppHomepage + $script:SuiteName
$script:WebAppDisplayName = $script:SuiteName + "-app"
$script:DeploymentTemplateFile = "$script:DeploymentConfigPath/ConnectedfactoryMapKey.json"
$script:DeploymentTemplateFileBingMaps = "$script:DeploymentConfigPath/Connectedfactory.json"
$script:VmDeploymentTemplateFile = "$script:DeploymentConfigPath/FactorySimulation.json"
$script:DeploymentSettingsFile = "{0}/{1}.config.user" -f $script:IoTSuiteRootPath, $script:DeploymentName

$script:TopologyDescription = "$script:WebAppPath/Contoso/Topology/ContosoTopologyDescription.json"
$script:VmAdminUsername = "docker"
$script:DockerRoot = "/home/$script:VmAdminUsername"
# Note: These folder names need to be in sync with paths specified as defaults in the simulation config.xml file
$script:DockerConfigFolder = "Config"
$script:DockerLogsFolder = "Logs"
$script:DockerSharedFolder = "Shared"
$script:DockerCertsFolder = "$script:DockerSharedFolder/CertificateStores/UA Applications/certs"
$script:DockerProxyRepo = "microsoft/iot-gateway-opc-ua-proxy"
$script:DockerProxyVersion = "1.0.2"
$script:DockerPublisherRepo = "microsoft/iot-gateway-opc-ua"
$script:DockerPublisherVersion = "2.0.0"
$script:UaSecretBaseName = "UAWebClient"
# Note: The password could only be changed if it is synced with the password used in CreateCerts.exe
$script:UaSecretPassword = "password"

# Load System.Web
Add-Type -AssemblyName System.Web
# Load System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression.FileSystem
# Load System.Security.Cryptography.X509Certificates
Add-Type -AssemblyName System.Security

# Handle commands
if ($script:Command -eq "clean")
{
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Cleaning up the project")
    Get-ChildItem -Recurse "$script:IoTSuiteRootPath/packages"  | ForEach-Object { Remove-Item -Force -Recurse -Path $_.FullName }
    Get-ChildItem -Recurse -Directory build_output | ForEach-Object { Remove-Item -Force -Recurse -Path $_.FullName }
    Get-ChildItem -Recurse -Directory obj | ForEach-Object { Remove-Item -Recurse -Force -Path $_.FullName }
    Get-ChildItem -Recurse -Directory bin | ForEach-Object { Remove-Item -Recurse -Force -Path $_.FullName }
    exit
}

# Build everything for build and updatesimulation commands
if ($script:Command -eq "build" -or $script:Command -eq "updatesimulation")
{
    # Build the solution
    Build

    # Package and upload solution WebPackages
    Package
    FinalizeWebPackages

    # Build the simulation
    SimulationBuild

    # Build simulation scripts
    SimulationBuildScripts

    # Compressed simulation binaries
    Write-Verbose "$(Get-Date –f $TIME_STAMP_FORMAT) - Build compressed archive"
    Write-Tar "$script:SimulationBuildOutputPath" -OutputPath "$script:SimulationPath/buildOutput.tar" -Quiet 4> $null | Out-Null
    Write-BZip2 -LiteralPath "$script:SimulationPath/buildOutput.tar" -OutputPath "$script:SimulationPath" -Quiet 4> $null | Out-Null
    Remove-Item "$script:SimulationPath/simulation" -ErrorAction SilentlyContinue | Out-Null
    Move-Item "$script:SimulationPath/buildOutput.tar.bz2" "$script:SimulationPath/simulation" | Out-Null

    # We are done in case of a build command
    if ($script:Command -eq "build")
    {
        exit
    }
}

if ($script:Command -eq "delete")
{
    # Remove the resource group.
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Check if resource group with name '{0}' exists." -f $script:SuiteName)
    $resourceGroup = Get-AzureRmResourceGroup -Name $script:SuiteName -ErrorAction SilentlyContinue
    if ($resourceGroup -ne $null)
    {
        Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Resource group found. Remove it. This may take a while.")
        Remove-AzureRmResourceGroup -Name $script:SuiteName -Force -ErrorAction SilentlyContinue | Out-Null
    }
    else
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Cannot find resource group name '{0}'. Have you selected the correct subscription by Select-AzureRmSubscription?" -f $script:SuiteName)
        throw ("Cannot find resource group name '{0}'. Do you have selected the correct subscription by Select-AzureRmSubscription?" -f $script:SuiteName)
    }

    # Remove the WebApp.
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Check if WebApp with the following IdentifierUri'{0}' exists" -f $script:WebAppIdentifierUri)
    $webApp = Get-AzureRmADApplication -IdentifierUri $script:WebAppIdentifierUri  -ErrorAction SilentlyContinue
    if ($webApp -ne $null)
    {
        Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - WebApp found. Remove it.")
        Remove-AzureRmADApplication -ObjectId $webApp.ObjectId -Force -ErrorAction SilentlyContinue
    }

    # Delete the deployment settings file.
    Write-Output Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Delete existing settings file '{0}'" -f $script:DeploymentSettingsFile)
    Remove-Item $script:DeploymentSettingsFile -Force -ErrorAction SilentlyContinue | Out-Null

    # Delete deployment certificates.
    Write-Output Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Delete existing certificates")
    Remove-Item -Recurse -Path "$script:CreateCertsPath/certs/$script:DeploymentName" -Force -ErrorAction SilentlyContinue | Out-Null
    Remove-Item -Recurse -Path "$script:CreateCertsPath/private/$script:DeploymentName" -Force -ErrorAction SilentlyContinue | Out-Null
    exit
}

# Initialize deployment settings.
Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - InitializeDeployment settings for'{0}'" -f $script:DeploymentName)
InitializeDeploymentSettings

# Initialize Azure environment, but not for updatesimulation command.
if ($script:Command -ne "updatesimulation")
{
    # Clear DNS
    ClearDnsCache

    # Sets Azure Account, Location, Name validation and AAD application.
    InitializeEnvironment 
}

# Generate and persist VM admin password.
if ([string]::IsNullOrEmpty($script:VmAdminPassword))
{
    $script:VmAdminPassword = GetOrSetEnvSetting "VmAdminPassword" "RandomPassword"
}
else
{
    PutEnvSetting "VmAdminPassword" $script:VmAdminPassword
}

# Initialize used SKUs
if ($script:LowCost)
{
    # Set SKU values to use the Azure assets generating the lowest costs.
    $script:StorageSkuName = "Standard_LRS"
    $script:StorageKind = "Storage"
    $script:IoTHubSkuName = "S1"
    $script:IotHubSkuCapacityUnits = 3
    $script:WebPlanSkuName = "F1"
    $script:WebPlanWorkerSize = 0
    $script:WebPlanWorkerCount = 1
    $script:WebPlanAlwaysOn = $false
    $script:VmSize = "Standard_D1_v2"
    $script:RdxEnvironmentSkuName = "S1"
    $script:KeyVaultSkuName = "Standard"
}
else
{
    # Set SKU values.
    $script:StorageSkuName = "Standard_LRS"
    $script:StorageKind = "Storage"
    $script:IoTHubSkuName = "S1"
    $script:IotHubSkuCapacityUnits = 3
    # For a local deployment we always use a low cost app service.
    if ($script:Command -eq "local")
    {
        $script:WebPlanSkuName = "F1"
        $script:WebPlanWorkerSize = 0
        $script:WebPlanWorkerCount = 1
        $script:WebPlanAlwaysOn = $false
    }
    else
    {
        $script:WebPlanSkuName = "S1"
        $script:WebPlanWorkerSize = 0
        $script:WebPlanWorkerCount = 1
        $script:WebPlanAlwaysOn = $true
    }
    $script:VmSize = "Standard_D1_v2"
    $script:RdxEnvironmentSkuName = "S1"
    $script:KeyVaultSkuName = "Standard"
}

# Initialize cloud related variables
$script:SuiteExists = (Find-AzureRmResourceGroup -Tag @{"IotSuiteType" = $script:SuiteType} | Where-Object {$_.name -eq $script:SuiteName -or $_.ResourceGroupName -eq $script:SuiteName}) -ne $null
Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Get resource group name for suiteName '{0}' and suiteType '{1}'" -f $script:SuiteName, $script:SuiteType)
$script:ResourceGroupName = (GetResourceGroup).ResourceGroupName
Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Resourcegroup name is '{0}'" -f $script:ResourceGroupName)
$script:StorageAccount = GetAzureStorageAccount
$script:StorageAccountBlobEndpoint = (Get-AzureRmStorageAccount  -ResourceGroupName $script:ResourceGroupName -Name $script:StorageAccount.StorageAccountName).PrimaryEndpoints.Blob
$script:IoTHubName = GetAzureIotHubName
$script:VmName = GetAzureVmName
$script:RdxEnvironmentName = GetAzureRdxName
$script:ArmParameter = @{}

# Update the simulation in the VM
if ($script:Command -eq "updatesimulation")
{
    # Check if resource group and VM exists.
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Validate resource group name '{0}' existence" -f $script:ResourceGroupName)
    $resourceGroup = Get-AzureRmResourceGroup -Name $script:ResourceGroupName -ErrorAction SilentlyContinue
    if ($resourceGroup -eq $null)
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Resource group {0} does not exist." -f $script:ResourceGroupName)
        throw ("Resource group {0} does not exist." -f $script:ResourceGroupName)
    }
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Check if VM exists in resource group '{0}'" -f $script:ResourceGroupName)
    $vmResource = Find-AzureRmResource -ResourceGroupNameContains $script:ResourceGroupName -ResourceType Microsoft.Compute/VirtualMachines -ResourceNameContains $script:ResourceGroupName -ErrorAction SilentlyContinue
    if ($vmResource -eq $null)
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - There is no VM '{0}' in resource group '{1}'." -f $script:ResourceGroupName, $script:ResourceGroupName)
        throw ("There is no VM with name '{0}' in resource group '{1}'." -f $script:ResourceGroupName, $script:ResourceGroupName)
    }

    # Update the simulation.
    Write-Output "$(Get-Date –f $TIME_STAMP_FORMAT) - Upload and start the simulation"
    SimulationUpdate
    UpdateBrowserEndpoints
    exit
}

# Respect existing Sku values
if ($script:SuiteExists)
{
    # Block redeployment
    if ($script:Command -eq "local" -or $script:Command -eq "cloud" -and $script:Force -eq $false)
    {
        Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - A deployment with name '{0}' already exists. Please use parameter -Force to enforce a redeployment." -f $script:SuiteName)
        throw ("An deployment with name '{0}' does already exists. Please use parameter -Force to enforce redeployment." -f $script:SuiteName)
    }

    try 
    {
        $storageResource = Get-AzureRmResource -ResourceName $script:StorageAccount.StorageAccountName -ResourceType Microsoft.´Storage/storageAccounts -ResourceGroupName $script:ResourceGroupName
        $script:StorageSkuName = $storageResource.Sku.name
        $script:StorageKind = $storageResource.Kind
    }
    catch {}

    try 
    {
        $iotHubResource = Get-AzureRmResource -ResourceName $script:IoTHubName -ResourceType Microsoft.Devices/IoTHubs -ResourceGroupName $script:ResourceGroupName
        $script:IoTHubSkuName = $iotHubResource.Sku.name
    }
    catch {}

    try 
    {
        $webResource = Get-AzureRmResource -ResourceName $script:WebsiteName -ResourceType Microsoft.Web/sites -ResourceGroupName $script:ResourceGroupName
        $webPlanResource = Get-AzureRmResource -ResourceId $webResource.Properties.serverFarmId
        $script:WebPlanSkuName = $webPlanResource.sku.name
        $script:WebWorkerSize = $webPlanResource.containerSize
        $script:WebWorkerCount = $webPlanResource.maxNumberOfWorkers
    }
    catch {}

    try 
    {
        $vmResource = Get-AzureRmResource -ResourceName $script:VmName -ResourceType Microsoft.Compute/virtualMachines -ResourceGroupName $script:ResourceGroupName
        $script:VmSize = $vmResource.Properties.hardwareProfile.vmSize
    }
    catch {}

    try 
    {
        $rdxResource = Get-AzureRmResource -ResourceName $script:RdxName -ResourceType Microsoft.TimeseriesInsights/environments -ResourceGroupName $script:ResourceGroupName
        $script:RdxEnvironmentSkuName = $rdxResource.Sku.name
    }
    catch {}
}

# Setup AAD for webservice
UpdateResourceGroupState ProvisionAAD
UpdateAadApp $script:AadTenant
$script:AadClientId = GetEnvSetting "AadClientId"
UpdateEnvSetting "AadInstance" ($script:AzureEnvironment.ActiveDirectoryAuthority + "{0}")

# Build the solution
Build

# Package and upload solution WebPackages
Package
FinalizeWebPackages

# Build the simulation
SimulationBuild

# Build simulation scripts
SimulationBuildScripts

# Compressed simulation binaries
Write-Verbose "$(Get-Date –f $TIME_STAMP_FORMAT) - Build compressed archive"
Write-Tar "$script:SimulationBuildOutputPath" -OutputPath "$script:SimulationPath/buildOutput.tar" -Quiet 4> $null | Out-Null
Write-BZip2 -LiteralPath "$script:SimulationPath/buildOutput.tar" -OutputPath "$script:SimulationPath" -Quiet 4> $null | Out-Null
Remove-Item "$script:SimulationPath/simulation" -ErrorAction SilentlyContinue | Out-Null
Move-Item "$script:SimulationPath/buildOutput.tar.bz2" "$script:SimulationPath/simulation" | Out-Null

# Copy the factory simulation template, the factory simulation binaries and the VM init script into the WebDeploy container.
Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Upload all required files into the storage account.")
$script:VmArmTemplateUri = UploadFileToContainerBlob $script:VmDeploymentTemplateFile $script:StorageAccount.StorageAccountName "WebDeploy" $true
$script:SimulationUri = UploadFileToContainerBlob "$script:SimulationPath/simulation" $script:StorageAccount.StorageAccountName "WebDeploy" $true
$script:InitSimulationUri = UploadFileToContainerBlob $script:SimulationBuildOutputInitScript $script:StorageAccount.StorageAccountName "WebDeploy" $true
$script:WebAppUri = UploadFileToContainerBlob $script:WebAppLocalPath $script:StorageAccount.StorageAccountName "WebDeploy" -secure $true

# Ensure that our build output is picked up by the ARM deployment.
$script:ArmParameter += @{ `
    webAppUri = $script:WebAppUri; `
    vmArmTemplateUri = $script:VmArmTemplateUri; `
    simulationUri = $script:SimulationUri; `
    initSimulationUri = $script:InitSimulationUri; `
}

$script:X509Collection = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2Collection
$script:X509Collection.Import("$script:CreateCertsPath/private/$script:DeploymentName/$script:UaSecretBaseName.pfx", $script:UaSecretPassword, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable)
$script:UaSecretThumbprint = $script:X509Collection.ThumbPrint
Write-Verbose "$(Get-Date –f $TIME_STAMP_FORMAT) - X509 certificate for OPC UA communication has thumbprint: $script:UaSecretThumbprint"
$script:UaSecretForWebsiteEncoded = [System.Convert]::ToBase64String($script:X509Collection.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pkcs12))
$script:UaSecretForVmEncoded = [System.Convert]::ToBase64String($script:X509Collection.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert, $script:UaSecretPassword))
$script:WebSitesServicePrincipal = Get-AzureRmADServicePrincipal -ServicePrincipalName "abfa0a7c-a6b6-4736-8310-5855508787cd"
if ($script:WebSitesServicePrincipal -eq $null)
{
    Write-Verbose "$(Get-Date –f $TIME_STAMP_FORMAT) - Microsoft.Web serivce principal unknown. Registering Microsoft.Web for the subscription."
    Register-AzureRmResourceProvider -ProviderNamespace Microsoft.Web
    $script:maxTries = $MAX_TRIES;
    while ($script:WebSitesServicePrincipal -eq $null)
    {
        sleep $SECONDS_TO_SLEEP
        $script:WebSitesServicePrincipal = Get-AzureRmADServicePrincipal -ServicePrincipalName "abfa0a7c-a6b6-4736-8310-5855508787cd"
        if ($script:maxTries-- -le 0)
        {
            Write-Error ("$(Get-Date –f $TIME_STAMP_FORMAT) - Timed out while waiting for creation of the ServicePrincipal for resource provider for Microsoft.Web.")
            throw ("Timed out while waiting for creation of the ServicePrincipal for resource provider for Microsoft.Web.")
        }
    }
}
$script:WebSitesServicePrincipalObjectId = $script:WebSitesServicePrincipal.Id
Write-Verbose "$(Get-Date –f $TIME_STAMP_FORMAT) - Websites Service Principal Object Id: $script:WebSitesServicePrincipalObjectId"
$script:RdxAccessPolicyPrincipalObjectId = (Get-AzureRmADServicePrincipal -ServicePrincipalName $script:AadClientId).Id
Write-Verbose "$(Get-Date –f $TIME_STAMP_FORMAT) - AAD Client Service Principal Object Id: $script:RdxAccessPolicyPrincipalObjectId"
$script:RdxOwnerServicePrincipalObjectId = GetOwnerObjectId
Write-Verbose "$(Get-Date –f $TIME_STAMP_FORMAT) - Data Access Contributor Object Id: $script:RdxOwnerServicePrincipalObjectId"
$script:RdxAuthenticationClientSecret = CreateAadClientSecret

# Set up ARM parameters.
$script:ArmParameter += @{ `
    suitename = $script:SuiteName; `
    suiteType = $script:SuiteType; `
    storageName = $script:StorageAccount.StorageAccountName; `
    storageSkuName = $script:StorageSkuName; `
    storageKind = $script:StorageKind; `
    storageEndpointSuffix = $script:AzureEnvironment.StorageEndpointSuffix; `
    aadTenant = $script:AadTenant; `
    aadInstance = $($script:AzureEnvironment.ActiveDirectoryAuthority + "{0}"); `
    aadClientId = $script:AadClientId; `
    webPlanSkuName = $script:WebPlanSkuName; `
    webPlanWorkerSize = $script:WebPlanWorkerSize; `
    webPlanWorkerCount = $script:WebPlanWorkerCount; `
    webPlanAlwaysOn = $script:WebPlanAlwaysOn; `
    iotHubName = $script:IoTHubName; `
    iotHubSkuName = $script:IoTHubSkuName; `
    iotHubSkuCapacityUnits = $script:IoTHubSkuCapacityUnits; `
    rdxDnsName = $script:RdxSuffix; `
    rdxEnvironmentName = $script:RdxEnvironmentName; `
    rdxEnvironmentSkuName = $script:RdxEnvironmentSkuName; `
    rdxAuthenticationClientSecret = $script:RdxAuthenticationClientSecret; `
    rdxAccessPolicyPrincipalObjectId = $script:RdxAccessPolicyPrincipalObjectId; `
    rdxOwnerServicePrincipalObjectId = $script:RdxOwnerServicePrincipalObjectId; `
    vmSize = $script:VmSize; `
    adminUsername = $script:VmAdminUsername; `
    adminPassword = $script:VmAdminPassword; `
    keyVaultSkuName = $script:KeyVaultSkuName; `
    keyVaultSecretBaseName = $script:UaSecretBaseName; `
    keyVaultVmSecret = $script:UaSecretForVmEncoded; `
    keyVaultWebsiteSecret = $script:UaSecretForWebsiteEncoded; `
    uaSecretThumbprint = $script:UaSecretThumbprint; `
    uaSecretPassword =  $script:UaSecretPassword; `
    webSitesServicePrincipalObjectId = $script:WebSitesServicePrincipalObjectId; `
}

# Check if there is a bing maps license key set in the configuration file.
$script:MapApiQueryKey = GetEnvSetting "MapApiQueryKey"
if ([string]::IsNullOrEmpty($script:MapApiQueryKey))
{
    # To enable bing maps functionality, the PowerShell environement variable MapApiQueryKey must hold bing maps license key
    if (-not [string]::IsNullOrEmpty($env:MapApiQueryKey))
    {
        $script:MapApiQueryKey = $env:MapApiQueryKey
    }
}
# the bing maps is only set in the ARM template if there is a valid license key and if it is deployed in public cloud environments.
if (-not [string]::IsNullOrEmpty($script:MapApiQueryKey) -and $script:AzureEnvironmentName -eq "AzureCloud")
{
    # Pass the key to the ARM template.
    $script:ArmParameter += @{mapApiQueryKey=$script:MapApiQueryKey;}
}

# Show deployment parameters.
Write-Output "$(Get-Date –f $TIME_STAMP_FORMAT) - Suite name: $script:SuiteName"
Write-Output "$(Get-Date –f $TIME_STAMP_FORMAT) - Storage Name: $($script:StorageAccount.StorageAccountName)"
Write-Output "$(Get-Date –f $TIME_STAMP_FORMAT) - IotHub Name: $script:IoTHubName"
Write-Output "$(Get-Date –f $TIME_STAMP_FORMAT) - Rdx Name: $script:RdxEnvironmentName"
Write-Output "$(Get-Date –f $TIME_STAMP_FORMAT) - AAD Tenant: $($script:AadTenant)"
Write-Output "$(Get-Date –f $TIME_STAMP_FORMAT) - AAD ClientId: $($script:AadClientId)"
Write-Output "$(Get-Date –f $TIME_STAMP_FORMAT) - ResourceGroup Name: $script:ResourceGroupName"
Write-Output "$(Get-Date –f $TIME_STAMP_FORMAT) - Deployment template file: $script:DeploymentTemplateFile"

Write-Output "$(Get-Date –f $TIME_STAMP_FORMAT) - Provisioning resources, if this is the first time, this operation can take up 10 minutes..."
Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - ARM parameters:")
foreach ($script:ArmParameterKey in $script:ArmParameter.Keys) 
{
    Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - ARM Parameter '$($script:ArmParameterKey)' for deployment has value '$($script:ArmParameter[$script:ArmParameterKey])'")
}

# Deploy resources to Azure
Write-Verbose ("$(Get-Date –f $TIME_STAMP_FORMAT) - Deploy all other resources to Azure")
UpdateResourceGroupState ProvisionAzure
$script:ArmResult = New-AzureRmResourceGroupDeployment -ResourceGroupName $script:ResourceGroupName -TemplateFile $script:DeploymentTemplateFile -TemplateParameterObject $script:ArmParameter -Verbose
if ($script:ArmResult.ProvisioningState -ne "Succeeded")
{
    Write-Error "$(Get-Date –f $TIME_STAMP_FORMAT) - Resource deployment failed"
    UpdateResourceGroupState Failed
    throw "Provisioning failed"
}
else
{
    # For a debug confguration, we enable error logging of the WebApp
    if ($script:Configuration -eq "debug" -and $script:CloudDeploy -eq $true)
    {
        [System.Boolean]$enable = $true;
        Set-AzureRmWebApp -ResourceGroupName $script:ResourceGroupName -Name $script:SuiteName -DetailedErrorLoggingEnabled $enable | Out-Null
    }
}

# Set Config file variables
Write-Verbose  "$(Get-Date –f $TIME_STAMP_FORMAT) - Updating config file settings"
UpdateEnvSetting "ServiceStoreAccountName" $script:StorageAccount.StorageAccountName
UpdateEnvSetting "SolutionStorageAccountConnectionString" $script:ArmResult.Outputs['storageConnectionString'].Value
UpdateEnvSetting "IotHubOwnerConnectionString" $script:ArmResult.Outputs['iotHubOwnerConnectionString'].Value
UpdateEnvSetting "RdxAuthenticationClientSecret" $script:RdxAuthenticationClientSecret
UpdateEnvSetting "RdxDnsName" $script:ArmResult.Outputs['rdxDnsName'].Value
UpdateEnvSetting "RdxEnvironmentId" $script:ArmResult.Outputs['rdxEnvironmentId'].Value
if ($script:ArmResult.Outputs['mapApiQueryKey'].Value.Length -gt 0 -and $script:ArmResult.Outputs['mapApiQueryKey'].Value -ne "0")
{
    UpdateEnvSetting "MapApiQueryKey" $script:ArmResult.Outputs['mapApiQueryKey'].Value
}

UpdateResourceGroupState Complete
Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Provisioning and deployment completed successfully, see {0}.config.user for deployment values" -f $script:DeploymentName)

# For cloud deployments start the website
if ($script:CloudDeploy -eq $true)
{
    $script:MaxTries = $MAX_TRIES
    $script:WebEndpoint = "{0}.{1}" -f $script:DeploymentName, $script:WebsiteSuffix
    if (!(HostEntryExists $script:WebEndpoint))
    {
        Write-Output "$(Get-Date –f $TIME_STAMP_FORMAT) - Waiting for website URL to resolve."
        while (!(HostEntryExists $script:WebEndpoint))
        {
            Clear-DnsClientCache
            Write-Progress -Activity "Resolving website URL" -Status "Trying" -SecondsRemaining ($script:MaxTries*$SECONDS_TO_SLEEP)
            if ($script:MaxTries-- -le 0)
            {
                Write-Warning ("$(Get-Date –f $TIME_STAMP_FORMAT) - Unable to resolve Website endpoint {0}" -f $script:WebAppHomepage)
                break
            }
            sleep $SECONDS_TO_SLEEP
        }
    }
    if (HostEntryExists $script:WebEndpoint)
    {
        # Wait till we can successfully load the page
        Write-Output "$(Get-Date –f $TIME_STAMP_FORMAT) - Waiting for website to respond."
        while ($true)
        {
            try
            {
                $result = Invoke-WebRequest -Uri $script:WebAppHomepage
            }
            catch 
            {
                $result = $null
            }
            if ($result -ne $null -and $result.StatusCode -eq 200)
            {
                break;
            }
            Write-Verbose "$(Get-Date –f $TIME_STAMP_FORMAT) - Sleep for 5 seconds and check again."
            Start-Sleep -Seconds 10
        }
        # start the browser to show the page
        start $script:WebAppHomepage
    }
}
else
{
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - For local deployment, open the Connectedfactory.sln and run the Web project from Visual Studio.")
    Write-Output ("$(Get-Date –f $TIME_STAMP_FORMAT) - Then you can access the dashboard at '{0}'" -f $script:WebAppHomepage)
}
