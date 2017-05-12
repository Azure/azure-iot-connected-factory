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

## Documentation

  * [IoT Suite documentation](https://azure.microsoft.com/documentation/suites/iot-suite/)
  * [Frequently asked questions for IoT Suite](https://azure.microsoft.com/documentation/articles/iot-suite-faq/)
  * [Permissions on the azureiotsuite.com site](https://azure.microsoft.com/documentation/articles/iot-suite-permissions/). This includes instructions for adding co-administrators to your preconfigured solution.
  
### Visual Studio Solution
  * **Connectedfactory:** contains the source code for the complete preconfigured solution, including the solution portal web app and the simulated factories.

### Requirements
  The project was developed and tested using the following tools:
  * VisualStudio 2015 Update 3
  * .NetCore 1.0 with SDK Preview 2 build 3121
  
## Feedback

Have ideas for how we can improve Azure IoT? Give us [Feedback](http://feedback.azure.com/forums/321918-azure-iot).

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
