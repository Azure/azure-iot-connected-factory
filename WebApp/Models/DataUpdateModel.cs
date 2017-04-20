using System.Collections.Generic;
using Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Models
{
    /// <summary>
    /// Defines the data model used for updating telemetry and performance data in client views.
    /// </summary>
    public class OeeKpiDataUpdate
    {
        /// <summary>
        /// The session ID of the browser session this data belongs to.
        /// </summary>
        public string SessionId { get; set; }

        public string TopNode { get; set; }

        public ContosoPerformanceSetting OeeAvailabilityPerformanceSetting { get; set; }
        public ContosoOeeAvailabilityData OeeAvailabilityLast { get; set; }

        public ContosoPerformanceSetting OeePerformancePerformanceSetting { get; set; }
        public ContosoOeePerformanceData OeePerformanceLast { get; set; }

        public ContosoPerformanceSetting OeeQualityPerformanceSetting { get; set; }
        public ContosoOeeQualityData OeeQualityLast { get; set; }

        public ContosoPerformanceSetting OeeOverallPerformanceSetting { get; set; }
        public ContosoOeeOverallData OeeOverallLast { get; set; }

        public ContosoPerformanceSetting Kpi1PerformanceSetting { get; set; }
        public ContosoKpi1Data Kpi1Last { get; set; }

        public ContosoPerformanceSetting Kpi2PerformanceSetting { get; set; }
        public ContosoKpi2Data Kpi2Last { get; set; }
    }

    public class AlertDataUpdate
    {
        /// <summary>
        /// The session ID of the browser session this data belongs to.
        /// </summary>
        public string SessionId { get; set; }

        public string TopNode { get; set; }

        public List<ContosoAlertInfo> Alerts { get; set; }
    }
    public class ChildrenDataUpdate
    {
        /// <summary>
        /// The session ID of the browser session this data belongs to.
        /// </summary>
        public string SessionId { get; set; }

        public string TopNode { get; set; }

        public List<ContosoChildInfo> Children { get; set; }
    }
}