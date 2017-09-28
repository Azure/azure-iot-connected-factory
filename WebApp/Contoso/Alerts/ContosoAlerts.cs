using GlobalResources;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso
{
    /// <summary>
    /// Defines the known action types for alerts.
    /// </summary>
    public enum ContosoAlertActionType
    {
        None = 0,
        AcknowledgeAlert,
        CloseAlert,
        CallOpcMethod,
        OpenWebPage
    };

    /// <summary>
    /// Class to parse the alert action description.
    /// </summary>
    public class ContosoAlertActionDescription
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ContosoAlertActionType Type;

        [JsonProperty]
        public string Description;

        [JsonProperty]
        public string Parameter;
    }

    /// <summary>
    /// Class for alert action definitions.
    /// </summary>
    public class ContosoAlertActionDefinition
    {
        // The action type.
        public ContosoAlertActionType Type { get; set; }

        // Description of the action, which show up in the UX.
        public string Description { get; set; }

        // Parameters for the action, this could be used per action type as needed.
        public string Parameter;

        /// <summary>
        /// Ctor for the alert action definition.
        /// </summary>
        public ContosoAlertActionDefinition(ContosoAlertActionType type, string description, string parameter)
        {
            Type = type;
            Description = description;
            Parameter = parameter;
        }

        /// <summary>
        /// Returns the list of the default alert actions. Those are acknowledement and close the alert, as well
        /// as navigate in the dashboard to the source of the alert.
        /// </summary>
        public static List<ContosoAlertActionDefinition> DefaultAlertActions()
        {
            List<ContosoAlertActionDefinition> defaultAlertActions = new List<ContosoAlertActionDefinition>();

            defaultAlertActions.Add(new ContosoAlertActionDefinition(ContosoAlertActionType.AcknowledgeAlert, Strings.AlertActionAcknowledge, null));
            defaultAlertActions.Add(new ContosoAlertActionDefinition(ContosoAlertActionType.CloseAlert, Strings.AlertActionClose, null));

            return defaultAlertActions;
        }

        /// <summary>
        /// Initialized the alert action definition object.
        /// </summary>
        public static List<ContosoAlertActionDefinition> Init(List<ContosoAlertActionDescription> alertActionDescriptions)
        {
            List<ContosoAlertActionDefinition> alertActions = new List<ContosoAlertActionDefinition>();

            if (alertActionDescriptions == null)
            {
                alertActions.AddRange(DefaultAlertActions());
            }
            else
            {
                // Process minimum alert actions.
                if (alertActionDescriptions.All(x => x.Type != ContosoAlertActionType.None))
                {
                    // Add the default actions.
                    alertActions.AddRange(DefaultAlertActions());

                    // Add all other configured actions.
                    alertActions.AddRange(alertActionDescriptions.Select(alertActionDescription =>
                        new ContosoAlertActionDefinition(alertActionDescription.Type, alertActionDescription.Description,
                            alertActionDescription.Parameter)
                        ));
                }
            }
            return alertActions;
        }
    }

    /// <summary>
    /// Defines the infomation about alert actions. This information is sent to the clients and
    /// should expose minimal information.
    /// </summary>
    public class ContosoAlertActionInfo
    {
        public long Id { get; set; }

        public ContosoAlertActionType Type { get; set; }

        public string Description { get; set; }

        public ContosoAlertActionInfo(long id, ContosoAlertActionType type, string description)
        {
            Id = id;
            Type = type;
            Description = description;
        }
    }

    /// <summary>
    /// Enum which defines the different alert root causes.
    /// </summary>
    public enum ContosoAlertCause
    {
        AlertCauseValueBelowMinimum = 0,
        AlertCauseValueAboveMaximum,
        AlertCauseOeeOverallBelowMinimum,
        AlertCauseOeeOverallAboveMaximum,
        AlertCauseOeeAvailabilityBelowMinimum,
        AlertCauseOeeAvailabilityAboveMaximum,
        AlertCauseOeePerformanceBelowMinimum,
        AlertCauseOeePerformanceAboveMaximum,
        AlertCauseOeeQualityBelowMinimum,
        AlertCauseOeeQualityAboveMaximum,
        AlertCauseKpi1BelowMinimum,
        AlertCauseKpi1AboveMaximum,
        AlertCauseKpi2BelowMinimum,
        AlertCauseKpi2AboveMaximum
    }

    /// <summary>
    /// Enum which defines the status of the alert.
    /// </summary>
    public enum ContosoAlertStatus
    {
        AlertStatusActive = 0,
        AlertStatusAcknowledged
    }

    /// <summary>
    /// Defines an alert. Each alert has a unique id.
    /// </summary>
    public class ContosoAlert
    {
        // Hold the id the next alert gets.
        private static long _nextId;

        // The alert id.
        private long _id;

        // The root cause of the alert.
        public ContosoAlertCause Cause { get; set; }

        // The status of the alert.
        public ContosoAlertStatus Status { get; set; }

        // The time when the alert was detected.
        public DateTime Time { get; set; }

        // The topology node key of the source of the alert.
        public string Key { get; set; }
        
        // OPC UA nodeId, if the alert source was an OPC UA node.
        public string SubKey { get; set; }

        public long AlertId => _id;

        /// <summary>
        /// Initializes an alert object.
        /// </summary>
        public void Init(ContosoAlertCause alertCause, string key, string subKey, DateTime time)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("key must be non null and not empty.", nameof(key));
            }
            if (subKey == "TopologyRoot")
            {
                throw new ArgumentException("subKey is not allowed to be 'TopologyRoot'.", nameof(subKey));
            }
            if (string.IsNullOrEmpty(subKey))
            {
                subKey = "null";
            }
            _id = _nextId++;
            Cause = alertCause;
            Key = key;
            SubKey = subKey;
            Time = time;
            Status = ContosoAlertStatus.AlertStatusActive;
        }

        /// <summary>
        /// Ctor of the alert object, when the alert source is a topology node.
        /// </summary>
        public ContosoAlert(ContosoAlertCause alertCause, string key, DateTime time)
        {
            Init(alertCause, key, "null", time);
        }

        /// <summary>
        /// Ctor of the alert object, if the alert source is an OPC UA node.
        /// </summary>
        public ContosoAlert(ContosoAlertCause alertCause, string key, string subKey, DateTime time)
        {
            Init(alertCause, key, subKey, time);
        }

        /// <summary>
        /// Returns a description of the alert cause, which is shown in the UX.
        /// </summary>
        public static string GetAlertCauseDescription(ContosoAlertCause cause)
        {
            switch (cause)
            {
                case ContosoAlertCause.AlertCauseOeeOverallBelowMinimum:
                    return Strings.AlertOeeOverallBelowMinimum;

                case ContosoAlertCause.AlertCauseOeeOverallAboveMaximum:
                    return Strings.AlertOeeOverallAboveMaximum;

                case ContosoAlertCause.AlertCauseOeeAvailabilityBelowMinimum:
                    return Strings.AlertOeeAvailabilityBelowMinimum;

                case ContosoAlertCause.AlertCauseOeeAvailabilityAboveMaximum:
                    return Strings.AlertOeeAvailabilityAboveMaximum;

                case ContosoAlertCause.AlertCauseOeePerformanceBelowMinimum:
                    return Strings.AlertOeePerformanceBelowMinimum;

                case ContosoAlertCause.AlertCauseOeePerformanceAboveMaximum:
                    return Strings.AlertOeePerformanceAboveMaximum;

                case ContosoAlertCause.AlertCauseOeeQualityBelowMinimum:
                    return Strings.AlertOeeQualityBelowMinimum;

                case ContosoAlertCause.AlertCauseOeeQualityAboveMaximum:
                    return Strings.AlertOeeQualityAboveMaximum;

                case ContosoAlertCause.AlertCauseKpi1BelowMinimum:
                    return Strings.AlertKpi1BelowMinimum;

                case ContosoAlertCause.AlertCauseKpi1AboveMaximum:
                    return Strings.AlertKpi1AboveMaximum;

                case ContosoAlertCause.AlertCauseKpi2BelowMinimum:
                    return Strings.AlertKpi2BelowMinimum;

                case ContosoAlertCause.AlertCauseKpi2AboveMaximum:
                    return Strings.AlertKpi2AboveMaximum;

                case ContosoAlertCause.AlertCauseValueBelowMinimum:
                    return Strings.AlertValueBelowMinimum;

                case ContosoAlertCause.AlertCauseValueAboveMaximum:
                    return Strings.AlertValueAboveMaximum;
            }
            return Strings.AlertDetailsUnknown;
        }
    }

    /// <summary>
    /// Defines the infomation about alerts. This information is sent to the clients and
    /// should expose minimal information.
    /// </summary>
    public class ContosoAlertInfo
    {
        // Defines the max number of elements of a node in the topology tree.
        // This is factory, production line, station, OPC UA node.
        public const int MaxTopologyDetailsCount = 4;

        // Id of the alert.
        public long AlertId { get; set; }

        // Key of the topology node the alert occured.
        public string Key { get; set; }

        // OPC UA nodeId, if the alert source was an OPC UA node.
        public string SubKey { get; set; }

        // If the actual value falls below Minimum an alert is created.
        public double? Minimum { get; set; }

        // If the actual value raises above Maximum, an alert is created.
        public double? Maximum { get; set; }

        // Time when the alert was detected.
        public DateTime Time { get; set; }

        // Cause of the alert.
        public ContosoAlertCause Cause { get; set; }

        // STatus of the alert.
        public ContosoAlertStatus Status { get; set; }

        // Occurences count of alerts with the same cause in this toplogy node.
        public long Occurences { get; set; }

        // Description of the toplology node, shown in the UX.
        public string[] TopologyDetails { get; set; }

        // Time the alert was detected, shown in the UX.
        public string UxTime { get; set; }

        // Actios for the user to execute for this alert, shown in the UX.
        public List<ContosoAlertActionInfo> AlertActionInfo;
    }
}
