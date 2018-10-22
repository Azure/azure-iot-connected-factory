
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso
{
    /// <summary>
    /// Enum, which defines the different performance relevant categories we are handling.
    /// </summary>
    public enum ContosoPerformanceRelevance {
        NotRelevant = 0,

        // OeeOverall is used for internal processing. No OPC UA nodes should be tagged with this relevance.
        OeeOverall,

        // OeeAvailability is used for internal processing. No OPC UA nodes should be tagged with this relevance.
        OeeAvailability,

        // OeeAvailablility_Running and OeeAvailability_Fault are tags used for calculation of the OEE Availability
        //.
        // OEE Availability is the availability rate of the equipment and is calculated as: Actual production time / Potential production time
        // in our case this means: (Overall running time - Fault time) / Overall running time
        OeeAvailability_Running,
        OeeAvailability_Fault,

        // OeePerformance is used for internal processing. No OPC UA nodes should be tagged with this relevance.
        OeePerformance,

        // OeePerformance_Ideal and OeePerformance_Actual are tags used for calculation of the OEE Performance
        //
        // OEE Performance is the performance rate of the equipment and is calculated as: Actual output / Theoretical output
        // in our case this means: Actual cycle time / Ideal cycle time
        OeePerformance_Ideal,
        OeePerformance_Actual,

        // OeeQuality is used for internal processing. No OPC UA nodes should be tagged with this relevance.
        OeeQuality,
    
        // OeeQuality_Bad and OeeQuality_Good are tags used for calculation of the OEE Quality
        //
        // OEE Quality is the quality rate of the equipment and is calculated as: Good output / Actual output
        // in our case this means: Good products/(Good products + Bad products)
        OeeQuality_Bad,
        OeeQuality_Good,

        // Kpi1 is the tag used to mark the OPC UA nodes contributing to the number of manufactured products of a production line.
        Kpi1,

        // Kpi2 is the tag used to mark the OPC UA nodes measuring the energy consumption of a station.
        Kpi2
    };

    /// <summary>
    /// Class to parse perfomance descriptions.
    /// </summary>
    public class ContosoPerformanceDescription
    {
        [JsonProperty]
        public double Minimum;

        [JsonProperty]
        public double Target;

        [JsonProperty]
        public double Maximum;

        [JsonProperty]
        public List<ContosoAlertActionDescription> MinimumAlertActions;

        [JsonProperty]
        public List<ContosoAlertActionDescription> MaximumAlertActions;

        /// <summary>
        /// Default values for the performance descriptions if nothing specified.
        /// </summary>
        public ContosoPerformanceDescription()
        {
            Minimum = 50;
            Target = 90;
            Maximum = 100;
        }
    }

    /// <summary>
    /// Defines the current performance status in colors.
    /// To add a new status each status must have an increasing number of one in its binary rapresentation.
    /// For example for 3 status:
    ///       binary decimal
    /// good    00	   0
	/// medium	01	   1
	/// poor	11	   3
    /// </summary>
    [Flags]
    public enum ContosoPerformanceStatus { Good = 0, Poor = 1 };

    /// <summary>
    /// Enum which defines the operation for the performance aggregation.
    /// </summary>
    public enum ContosoPerformanceSettingAggregator
    {
        Undefined,
        Percent,
        MinStation,
        Add
    }

    /// <summary>
    /// Class to define the performance of topology nodes.
    /// </summary>
    public class ContosoPerformanceSetting
    {
        /// <summary>
        /// Specify type of perf setting.
        /// </summary>
        public ContosoPerformanceSettingAggregator PerfType { get; set; }

        /// <summary>
        /// If the actual value falls below Minimum an alert is created.
        /// </summary>
        public double Minimum { get; set; }

        /// <summary>
        /// Target value for the performance.
        /// </summary>
        public double Target { get; set; }

        /// <summary>
        /// If the actual value raises above Maximum, an alert is created.
        /// </summary>
        public double Maximum { get; set; }

        /// <summary>
        /// Defines the actions a user can execute as reaction for a minimum alert.
        /// </summary>
        public List<ContosoAlertActionDefinition> MinimumAlertActions;

        /// <summary>
        /// Defines the actions a user can execute as reaction for a maximum alert.
        /// </summary>
        public List<ContosoAlertActionDefinition> MaximumAlertActions;

        /// <summary>
        /// Resets the performance values during performance aggregation.
        /// </summary>
        public void Reset()
        {
            if (PerfType == ContosoPerformanceSettingAggregator.MinStation)
            {
                Minimum = Target = Maximum = double.MaxValue;
            }
            else
            {
                Minimum = Target = Maximum = 0;
            }
        }

        /// <summary>
        /// Add a performance settings child station in performance aggregation.
        /// </summary>
        public virtual void AddChildStation(ContosoPerformanceSetting child)
        {
            switch (PerfType)
            {
                case ContosoPerformanceSettingAggregator.MinStation:
                    Minimum = Math.Min(Minimum, child.Minimum);
                    Target = Math.Min(Target, child.Target);
                    Maximum = Math.Min(Maximum, child.Maximum);
                    break;

                case ContosoPerformanceSettingAggregator.Percent:
                case ContosoPerformanceSettingAggregator.Add:
                    Minimum += child.Minimum;
                    Target += child.Target;
                    Maximum += child.Maximum;
                    break;

                case ContosoPerformanceSettingAggregator.Undefined:
                    break;
            }
        }

        /// <summary>
        /// Add a performance settings child node in performance aggregation.
        /// </summary>
        public virtual void AddChild(ContosoPerformanceSetting child)
        {
            switch (PerfType)
            {
                case ContosoPerformanceSettingAggregator.MinStation:
                    if (Minimum == double.MaxValue)
                    {
                        Minimum = Target = Maximum = 0;
                    }
                    goto case ContosoPerformanceSettingAggregator.Add;
                case ContosoPerformanceSettingAggregator.Percent:
                case ContosoPerformanceSettingAggregator.Add:
                    Minimum += child.Minimum;
                    Target += child.Target;
                    Maximum += child.Maximum;
                    break;
                case ContosoPerformanceSettingAggregator.Undefined:
                    break;
            }
        }

        /// <summary>
        /// By default average added items.
        /// </summary>
        public virtual void Done(double addedItems)
        {
            switch (PerfType)
            {
                case ContosoPerformanceSettingAggregator.Percent:
                    Minimum /= addedItems;
                    Target /= addedItems;
                    Maximum /= addedItems;
                    break;
                case ContosoPerformanceSettingAggregator.MinStation:
                case ContosoPerformanceSettingAggregator.Add:
                case ContosoPerformanceSettingAggregator.Undefined:
                    break;
            }
        }

        /// <summary>
        /// Ctor for the ContosoPerformanceSetting.
        /// </summary>
        public ContosoPerformanceSetting(ContosoPerformanceSettingAggregator perfType, string key, ContosoPerformanceDescription performanceDescription)
        {
            PerfType = perfType;
            Minimum = performanceDescription.Minimum;
            Target = performanceDescription.Target;
            Maximum = performanceDescription.Maximum;

            MinimumAlertActions = new List<ContosoAlertActionDefinition>();
            MinimumAlertActions.AddRange(ContosoAlertActionDefinition.Init(performanceDescription.MinimumAlertActions));
            MaximumAlertActions = new List<ContosoAlertActionDefinition>();
            MaximumAlertActions.AddRange(ContosoAlertActionDefinition.Init(performanceDescription.MaximumAlertActions));
        }
    }
}
