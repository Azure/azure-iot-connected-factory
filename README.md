# Microsoft Azure IoT Suite 
You can deploy preconfigured solutions that implement common Internet of Things (IoT) scenarios to Microsoft Azure using your Azure subscription. You can use preconfigured solutions: 
- as a starting point for your own IoT solution. 
- to learn about the most common patterns in IoT solution design and development. 

Each preconfigured solution implements a common IoT scenario and is a complete, end-to-end implementation. You can deploy the Azure IoT Suite Connected factory preconfigured solution from [https://www.azureiotsuite.com](https://www.azureiotsuite.com), following the guidance outlined in this [document](https://azure.microsoft.com/en-us/documentation/articles/iot-suite-getstarted-preconfigured-solutions/). In addition, you can download the complete source code from this repository to customize and extend the solution to meet your specific requirements. 

## Connected Factory Preconfigured Solution
The Connected Factory Preconfigured Solution illustrates how you can get started with your Industrie 4.0 digital transformation. It brings together key Azure IoT services to enable the following features: OPC UA data ingestion, OPC UA server management, rules and actions and Azure Time Series Insights.

## Release Notes

#### Deployment Names
When using the build.ps1 script for deployment, it is recommended that you use a new deployment name each time you deploy the connected factory solution.

#### Connected Factory Deployment Time
It takes approximately 14 minutes to complete the deployment.

#### Connected Factory Telemetry Flow
After you have successfully deployed the solution and the web application launches the first time it takes approximately 3 minutes for data to show in the solution dashboard.

#### Bing Maps
If you don't have a Bing Maps API for Enterprise account, create one in the [Azure portal](https://portal.azure.com) by clicking + New, search for Bing Maps API for Enterprise and follow prompts to create. 

Get your Bing Maps API for Enterprise QueryKey from the Azure portal: 
1.	Navigate to the Resource Group where your Bing Maps API for Enterprise is in the Azure portal.
2.	Click All Settings, then Key Management. 
3.	You will see two keys: MasterKey and QueryKey. Copy the value for QueryKey.
4.	To have the key picked up by the build.ps1 script, set the environment variable "$env:MapApiQueryKey" in your PowerShell environment to a valid BingMaps license key and it will be picked up by the build script and automatically added to the settings of the App Service.
5.	Run a local or cloud deployment using build.ps1.

#### Simulation VM security
By default the configuration of the VM used for the production line simulation does not allow any inbound connections. This was a deliberate decision to maintain high level security for the VM. If you need to change the configuration of the production line VM to allow inbound connections, e.g. to login via SSH, please make sure you are running a vulnerability check on the VM first and also install the latest patches by following the [instructions](https://wiki.ubuntu.com/Security/Upgrades) on the Ubuntu website. 

## Documentation

  * [IoT Suite documentation](https://azure.microsoft.com/documentation/suites/iot-suite/)
  * [Frequently asked questions for IoT Suite](https://azure.microsoft.com/documentation/articles/iot-suite-faq/)
  * [Permissions on the azureiotsuite.com site](https://azure.microsoft.com/documentation/articles/iot-suite-permissions/). This includes instructions for adding co-administrators to your preconfigured solution.
  
### Visual Studio Solution
  * **Connectedfactory:** contains the source code for the complete preconfigured solution, including the solution portal web app and the simulated factories.

### Preparation
1. Install from  [here](https://www.visualstudio.com/downloads/). Release notes could be found [here](https://www.visualstudio.com/en-us/news/releasenotes/vs2017-relnotes).
  * Choose your edition
  * Any of these editions will work
  * Whatever edition you choose to install please ensure that you have selected:
    * ASP.NET and web development and Azure development workloads in the Web and Cloud section of the Visual Studio installer
    * .NET Core cross-platform development component in the Other Toolsets section of the Visual Studio installer
2. Install dotnet Core for Visual Studio 2017 from [here](https://github.com/dotnet/core/blob/master/release-notes/download-archives/1.1.2-download.md).
  * Choose the SDK Installer
3. Install the latest Azure Command Line tools for PowerShell from [here](https://azure.microsoft.com/en-us/downloads/).
  * Scroll down the page to the command line tools section, in the PowerShell section choose Windows install 
4. Install additional PowerShell cmdlets:
  * Search for Windows PowerShell in Start
  * Right click on result Windows PowerShell and choose Run as Administrator
  * In PowerShell (Administrator mode)
    * Install PSCX PowerShell cmdlets V3.2.2 on your system by "Install-Module PSCX -AllowClobber"
      * Choose Y to message "install from Powershell gallery if prompted"
    * Install-Module Posh-SSH on your system by "Install-Module Posh-SSH -AllowClobber" 
      * Choose Y to message "install from Powershell gallery if prompted"
5. Update your PowerShell profile or session environemnt
  * To build PCS MSBuild.exe must be in the path.
  * For releases prior to v1.0.1706.0 change the $env:PATH variable:
    * $env:PATH=$env:PATH+";C:\Program Files (x86)\MSBuild\14.0\Bin;"
  * For releases starting with v1.0.1706.0 change the $env:PATH variable depending on the Visual Studio edition as these samples show for Enterprise and Community edition:
      * Enterprise
        * $env:path = $env:path + ";C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin;"
        * $env:VisualStudioVersion="15.0"
        * $env:VSToolsPath="C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\Microsoft\VisualStudio\v15.0"
      * Community
        * $env:path = $env:path + ";C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin;"
        * $env:VisualStudioVersion="15.0"
        * $env:VSToolsPath="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\Microsoft\VisualStudio\v15.0"
### Run the build script
1. Search for Windows PowerShell in Start
2. Clone the repository from [here](https://github.com/Azure/azure-iot-connected-factory.git).
3. To build and deploy the solution into the cloud:
  * Run the following script from the root of your cloned repository: `./build.ps1 cloud -DeploymentName <your deployment name> -Configuration [release | debug ]`
    * Deployment naming rules - Length 4 - 64, Case insensitive, supports alphanumeric, underscore and hypens are all valid characters
  * More detailed help on the build script is available by typing get-help `.\build.ps1 - detailed`
  * Sign in with your Azure account credentials when prompted
  * Select the Azure Subscription to use
    * All your subscription will be shown in a list
    * Enter the number of the subscription you wish to use
    * If you only have one subscription it will be automatically selected
  * Choose Azure Location to use
    * List of available locations will be displayed to you
    * Enter the number of the location you wish to use
    * Location will be stored for future use
  * Select an Active Directories to use
    * List of available Active directories will be provided to you
    * Enter the number of the active directory you wish to use
  * When script completes successfully it will open a browser tab with the web app launched
 
 
### Additional build script help
There are more parameters available in the build.ps1 script. Please use get-help `.\build.ps1 -detailed` to get more information on them
 
 
### Delete the deployment
1.	Open a PowerShell command prompt
2.	Log in to your Azure Account and select the subscription the solution was deployed to by:`Select-AzureRmSubscription -SubscriptionName <subscription name the solution was deployed to>`
3.	Change to the root directory of the cloned repository
4.	Run the following script from the root of your cloned repository: `.\build.ps1 delete -DeploymentName <your deployment name>`
5.	This will delete all Azure resources of your deployment as well as the local configuration files
 
### Known Issues
#### Execution Policy Not Set 
1.	If you receive a message in PowerShell on execution policy not being set, do the following:
2.	Search for Windows PowerShell in Start
3.	Right click on result Windows PowerShell and choose Run as Administrator
4.	In PowerShell (Administrator) run: 
`Set-ExecutionPolicy  -ExecutionPolicy Unrestricted -Force`
 
#### Security Warning
1.	If you see a message in PowerShell
`Security warning
Run only scripts that you trust. While scripts from the internet can be useful, this script can potentially harm your
computer. If you trust this script, use the Unblock-File cmdlet to allow the script to run without this warning
message. Do you want to run C:\MyConnectedFactoryClone\build.ps1?
[D] Do not run  [R] Run once  [S] Suspend  [?] Help (default is "D"):
Do you want to run this script?`
2. Choose R
 
#### Sign in Issue
1.	On first run if sign in does not work, please try it again
 
#### Authorization_RequestDenied Message
If you see an message `Invoke-RestMethod : {"odata.error":{"code":"Authorization_RequestDenied","message":{"lang":"en","value":"Insufficient privileges to complete the operation."}}}`
This can be ignored, it is displayed when the user doesn't have administrator privileges to the active directory, the Script will continue to execute

## Feedback

Have ideas for how we can improve Azure IoT? Give us [Feedback](http://feedback.azure.com/forums/321918-azure-iot).

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
