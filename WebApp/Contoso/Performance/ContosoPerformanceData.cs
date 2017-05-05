using System;
using System.Collections.Generic;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Contoso
{

    /// <summary>
    /// Class for one data time.
    /// </summary>
    public class ContosoDataItem
    {
        /// <summary>
        /// Time the data item have been created at source.
        /// </summary>
        public DateTime Time { get; set; }
        /// <summary>
        /// The actual data value.
        /// </summary>
        public double Value { get; set; }
        /// <summary>
        /// Default ctor of a data item.
        /// </summary>
        public ContosoDataItem()
        {
            Time = DateTime.MinValue;
            Value = 0;
        }
        /// <summary>
        /// Ctor of a data item with actual data.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="data"></param>
        public ContosoDataItem(DateTime time, double value)
        {
            Time = time;
            Value = value;
        }
        /// <summary>
        /// Ctor of a data item for inherited ctor.
        /// </summary>
        /// <param name="dataItem"></param>
        protected ContosoDataItem(ContosoDataItem dataItem)
        {
            Time = dataItem.Time;
            Value = dataItem.Value;
        }
        /// <summary>
        /// Default add operation for data item.
        /// Overloaded in inherited classes
        /// </summary>
        /// <param name="dataItem"></param>
        public virtual void Add(ContosoDataItem dataItem)
        {
            Value += dataItem.Value;
            Time = new DateTime(Math.Max(Time.Ticks, dataItem.Time.Ticks), DateTimeKind.Utc);
        }
        /// <summary>
        /// Default add operation when adding a station data item.
        /// Overloaded in inherited classes. By default using Add.
        /// </summary>
        /// <param name="dataItem"></param>
        public virtual void AddStation(ContosoDataItem dataItem)
        {
            Add(dataItem);
        }
    }

    public class ContosoOeeAvailabilityData : ContosoDataItem
    {
        private double _overallRunningTime;
        private double _overallFaultTime;

        public double OverallRunningTime
        {
            get
            {
                return _overallRunningTime;
            }
            set
            {
                _overallRunningTime = value;
                Value = CalculateOeeAvailability();
            }
        }
        public double OverallFaultTime
        {
            get
            {
                return _overallFaultTime;
            }
            set
            {
                _overallFaultTime = value;
                Value = CalculateOeeAvailability();
            }
        }

        public ContosoOeeAvailabilityData() : base()
        {
            _overallRunningTime = 0;
            _overallFaultTime = 0;
            Value = CalculateOeeAvailability();
        }

        public ContosoOeeAvailabilityData(DateTime time, double overallRunningTime, double overallFaultTime) : base()
        {
            _overallRunningTime = overallRunningTime;
            _overallFaultTime = overallFaultTime;
            Time = time;
            Value = CalculateOeeAvailability();
        }

        public double OeeAvailability { get { return Value; } }

        public double CalculateOeeAvailability()
        {
            // OeeAvailability is computed by: Actual production time / Potential production time
            // We get Overall running time and Fault time from the stations, which is used for this computation.
            return ((_overallRunningTime != 0) ? (((_overallRunningTime - _overallFaultTime) * 100) / _overallRunningTime) : 100);
        }

        public static ContosoOeeAvailabilityData operator +(ContosoOeeAvailabilityData x1, ContosoOeeAvailabilityData x2)
        {
            return new ContosoOeeAvailabilityData((x2.Time > x1.Time ? x2.Time : x1.Time), x1.OverallRunningTime + x2.OverallRunningTime, x1.OverallFaultTime + x2.OverallFaultTime);
        }

        public override void Add(ContosoDataItem dataItem)
        {
            ContosoOeeAvailabilityData x = dataItem as ContosoOeeAvailabilityData;
            _overallRunningTime += x.OverallRunningTime;
            OverallFaultTime += x.OverallFaultTime;
            Time = new DateTime(Math.Max(Time.Ticks, dataItem.Time.Ticks), DateTimeKind.Utc);
        }
    }


    public class ContosoOeePerformanceData : ContosoDataItem
    {
        private double _idealCycleTime;
        private double _actualCycleTime;

        public double IdealCycleTime
        {
            get
            {
                return _idealCycleTime;
            }
            set
            {
                _idealCycleTime = value;
                Value = CalculateOeePerformance();
            }
        }
        public double ActualCycleTime
        {
            get
            {
                return _actualCycleTime;
            }
            set
            {
                _actualCycleTime = value;
                Value = CalculateOeePerformance();
            }
        }

        public ContosoOeePerformanceData() : base()
        {
            _idealCycleTime = 0;
            _actualCycleTime = 0;
            Value = CalculateOeePerformance();
        }

        public ContosoOeePerformanceData(DateTime time, double idealCycleTime, double actualOutput) : base()
        {
            _idealCycleTime = idealCycleTime;
            _actualCycleTime = actualOutput;
            Time = time;
            Value = CalculateOeePerformance();
        }

        public double OeePerformance
        {
            get
            {
                return Value;
            }
        }
        public double CalculateOeePerformance()
        {
            // OeePerformance is computed by: max. theoretical output / actual output
            // Use actual cycle time and ideal cycle 
            return (_idealCycleTime != 0 && _actualCycleTime != 0 ? ((_idealCycleTime / _actualCycleTime) * 100) : 100);
        }
        public static ContosoOeePerformanceData operator +(ContosoOeePerformanceData x1, ContosoOeePerformanceData x2)
        {
            return new ContosoOeePerformanceData((x2.Time > x1.Time ? x2.Time : x1.Time), x1.IdealCycleTime + x2.IdealCycleTime, x1.ActualCycleTime + x2.ActualCycleTime);
        }
        public override void Add(ContosoDataItem dataItem)
        {
            ContosoOeePerformanceData x = dataItem as ContosoOeePerformanceData;
            _idealCycleTime += x.IdealCycleTime;
            ActualCycleTime += x.ActualCycleTime;
            Time = new DateTime(Math.Max(Time.Ticks, dataItem.Time.Ticks), DateTimeKind.Utc);
        }
    }


    public class ContosoOeeQualityData : ContosoDataItem
    {
        private double _bad;
        private double _good;

        public double Good
        {
            get
            {
                return _good;
            }
            set
            {
                _good = value;
                Value = CalculateOeeQuality();
            }
        }
        public double Bad
        {
            get
            {
                return _bad;
            }
            set
            {
                _bad = value;
                Value = CalculateOeeQuality();
            }
        }

        public ContosoOeeQualityData() : base()
        {
            _good = 0;
            _bad = 0;
            Value = CalculateOeeQuality();
        }

        public ContosoOeeQualityData(DateTime time, double good, double bad)
        {
            _good = good;
            _bad = bad;
            Value = CalculateOeeQuality();
            Time = time;
        }

        public double OeeQuality { get { return Value; } }

        private double CalculateOeeQuality()
        {
            // OeeQuality is computed by: Good products / Actual output
            // We get only good and bad products from the simulation, so Actual output is the sum of good and bad products.
            return ((_good + _bad) != 0 ? ((_good / (_good + _bad)) * 100) : 100);
        }

        public static ContosoOeeQualityData operator +(ContosoOeeQualityData x1, ContosoOeeQualityData x2)
        {
            return new ContosoOeeQualityData((x2.Time > x1.Time ? x2.Time : x1.Time), x1.Good + x2.Good, x1.Bad + x2.Bad);
        }
        public override void Add(ContosoDataItem dataItem)
        {
            ContosoOeeQualityData x = dataItem as ContosoOeeQualityData;
            _bad += x.Bad;
            Good += x.Good;
            Time = new DateTime(Math.Max(Time.Ticks, dataItem.Time.Ticks), DateTimeKind.Utc);
        }

        public override void AddStation(ContosoDataItem dataItem)
        {
            ContosoOeeQualityData x = dataItem as ContosoOeeQualityData;
            _bad += x.Bad;
            Good = (Good == 0) ? x.Good : Math.Min(Good, x.Good);
            Time = new DateTime(Math.Max(Time.Ticks, dataItem.Time.Ticks), DateTimeKind.Utc);
        }

    }

    public class ContosoOeeOverallData : ContosoDataItem
    {
        private double _availability;
        private double _performance;
        private double _quality;

        public double Availability
        {
            get
            {
                return _availability;
            }
            set
            {
                _availability = value;
                Value = CalculateOeeOverall();
            }
        }

        public double Performance
        {
            get
            {
                return _performance;
            }
            set
            {
                _performance = value;
                Value = CalculateOeeOverall();
            }
        }

        public double Quality
        {
            get
            {
                return _quality;
            }
            set
            {
                _quality = value;
                Value = CalculateOeeOverall();
            }
        }

        public ContosoOeeOverallData() : base()
        {
            _availability = 0;
            _performance = 0;
            _quality = 0;
            Value = CalculateOeeOverall();
        }

        public ContosoOeeOverallData(DateTime time, double availablility, double performance, double quality) : base()
        {
            _availability = availablility;
            _performance = performance;
            _quality = quality;
            Time = time;
            Value = CalculateOeeOverall();
        }

        public ContosoOeeOverallData(ContosoOeeAvailabilityData availablility, ContosoOeePerformanceData performance, ContosoOeeQualityData quality) : base()
        {
            _availability = availablility.OeeAvailability;
            _performance = performance.OeePerformance;
            _quality = quality.OeeQuality;
            Time = new DateTime(Math.Max(availablility.Time.Ticks, Math.Max(performance.Time.Ticks, quality.Time.Ticks)), DateTimeKind.Utc);
            Value = CalculateOeeOverall();
        }

        public double OeeOverall
        {
            get
            {
                return Value;
            }
        }

        public double Update(
            ContosoOeeAvailabilityData availablility,
            ContosoOeePerformanceData performance,
            ContosoOeeQualityData quality)
        {
            _availability = availablility.OeeAvailability;
            _performance = performance.OeePerformance;
            _quality = quality.OeeQuality;
            Value = CalculateOeeOverall();
            return Value;
        }

        private double CalculateOeeOverall()
        {
            double oeeOverall = 100.0;
            if (_availability != 0 && _performance != 0 && _quality != 0)
            {
                oeeOverall = (_availability * _performance * _quality) / 1e4;
            }
            return oeeOverall;
        }
    }

    public class ContosoKpi1Data : ContosoDataItem
    {
        public ContosoKpi1Data() : base()
        {
        }
        public ContosoKpi1Data(DateTime time, double value) : base(time, value)
        {
        }
        public ContosoKpi1Data(ContosoDataItem dataItem) : base(dataItem)
        {
        }

        public void AddStation(ContosoKpi1Data x)
        {
            Kpi = (Kpi == 0) ? x.Kpi : Math.Min(Kpi, x.Kpi);
        }

        public double Kpi
        {
            get
            {
                return Value;
            }
            set
            {
                Value = value;
            }
        }
    }

    public class ContosoKpi2Data : ContosoDataItem
    {
        public ContosoKpi2Data() : base() { }
        public ContosoKpi2Data(DateTime time, double value) : base(time, value) { }
        public ContosoKpi2Data(ContosoDataItem dataItem) : base(dataItem) { }

        public double Kpi
        {
            get
            {
                return Value;
            }
            set
            {
                Value = value;
            }
        }
    }

    public class ContosoAggregatedOeeKpiTimeSpan
    {
        // startTime = endTime - timeSpan
        public DateTime EndTime = DateTime.MinValue;
        public TimeSpan IntervalTimeSpan;

        // list of Oee and Kpi
        public ContosoKpi1Data Kpi1 = new ContosoKpi1Data();
        public ContosoKpi2Data Kpi2 = new ContosoKpi2Data();
        public ContosoOeeOverallData OeeOverall = new ContosoOeeOverallData();
        public ContosoOeeAvailabilityData OeeAvailability = new ContosoOeeAvailabilityData();
        public ContosoOeePerformanceData OeePerformance = new ContosoOeePerformanceData();
        public ContosoOeeQualityData OeeQuality = new ContosoOeeQualityData();

        public ContosoAggregatedOeeKpiTimeSpan(TimeSpan intervalTimeSpan)
        {
            IntervalTimeSpan = intervalTimeSpan;
        }

        public void Reset()
        {
            Kpi1.Kpi = 0;
            Kpi2.Kpi = 0;
            OeeQuality.Bad = 0;
            OeeQuality.Good = 0;
            OeePerformance.IdealCycleTime = 0;
            OeePerformance.ActualCycleTime = 0;
            OeeAvailability.OverallRunningTime = 0;
            OeeAvailability.OverallFaultTime = 0;
            OeeOverall.Performance = 0;
            OeeOverall.Quality = 0;
            OeeOverall.Availability = 0;
        }

        public void Add(ContosoAggregatedOeeKpiTimeSpan x)
        {
            Kpi1.Add(x.Kpi1);
            Kpi2.Add(x.Kpi2);
            OeeAvailability.Add(x.OeeAvailability);
            OeePerformance.Add(x.OeePerformance);
            OeeQuality.Add(x.OeeQuality);
            OeeOverall.Update(OeeAvailability, OeePerformance, OeeQuality);
            if (x.EndTime > EndTime)
            {
                EndTime = x.EndTime;
            }
        }

        public void AddStation(ContosoAggregatedOeeKpiTimeSpan x)
        {
            Kpi1.AddStation(x.Kpi1);
            Kpi2.AddStation(x.Kpi2);
            OeeAvailability.AddStation(x.OeeAvailability);
            OeePerformance.AddStation(x.OeePerformance);
            OeeQuality.AddStation(x.OeeQuality);
            OeeOverall.Update(OeeAvailability, OeePerformance, OeeQuality);
            if (x.EndTime > EndTime)
            {
                EndTime = x.EndTime;
            }
        }
    }

    public class ContosoAggregatedOeeKpiHistogram
    {
        public DateTime EndTime = DateTime.MinValue;
        public TimeSpan IntervalTimeSpan { get; }
        public TimeSpan TotalTimeSpan { get; }
        public TimeSpan UpdateTimeSpan { get; }
        public bool AwaitTasks { get; }
        public bool CheckAlerts { get; }
        public bool UpdateBrowser { get; }
        public bool UpdateTopology { get; }
        public List<ContosoAggregatedOeeKpiTimeSpan> Intervals;

        /// <summary>
        /// Ctor of an aggregated Oee and Kpi Histogram.
        /// </summary>
        /// <param name="_intervalTimeSpan">Timespan of an interval</param>
        /// <param name="_intervals">Number of intervals in aggregate</param>
        /// <param name="_updateTimeSpan">Timespan to update</param>
        /// <param name="_awaitTasks">Update aggregate slow by awaiting each query or fast with parallel queries</param>
        /// <param name="_checkAlerts">Aggregate is used to check for alerts</param>
        /// <param name="_updateBrowser">Aggregate is used to update dashboard</param>
        /// <param name="_updateTopology">Aggregate is used to update topology</param>
        public ContosoAggregatedOeeKpiHistogram(
            TimeSpan _intervalTimeSpan,
            int _intervals,
            TimeSpan _updateTimeSpan,
            bool _awaitTasks,
            bool _checkAlerts = false,
            bool _updateBrowser = false,
            bool _updateTopology = false)
        {
            IntervalTimeSpan = _intervalTimeSpan;
            TotalTimeSpan = TimeSpan.FromSeconds(_intervalTimeSpan.TotalSeconds * _intervals);
            UpdateTimeSpan = _updateTimeSpan;
            AwaitTasks = _awaitTasks;
            CheckAlerts = _checkAlerts;
            UpdateBrowser = _updateBrowser;
            UpdateTopology = _updateTopology;

            Intervals = new List<ContosoAggregatedOeeKpiTimeSpan>();
            for (int i = 0; i < _intervals; i++)
            {
                Intervals.Add(new ContosoAggregatedOeeKpiTimeSpan(IntervalTimeSpan));
            }
        }

        /// <summary>
        /// Intervals can be accessed by index
        /// </summary>
        public ContosoAggregatedOeeKpiTimeSpan this[int i]
        {
            get { return Intervals[i]; }
        }

    }

}


