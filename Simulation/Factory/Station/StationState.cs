
using Opc.Ua;
using Opc.Ua.Sample.Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Station
{
    public enum StationStatus : int
    {
        Ready = 0,
        WorkInProgress = 1,
        Done = 2,
        Discarded = 3,
        Fault = 4
    }

    public partial class StationState
    {
        private const int c_failureCycleTime = 5000;            // [ms]
        private const ulong c_pressureStableTime = 30 * 1000;   // [ms]
        private const double c_pressureDefault = 2500;          // [mbar]
        private const double c_pressureHigh = 6000;             // [mbar]

        private DateTime m_stationStartTime;
        private DateTime m_cycleStartTime;
        private Stopwatch m_faultClock = new Stopwatch();
        private DateTime m_pressureStableStartTime;

        private ulong m_serialNumber;
        private ulong m_actualCycleTime;
        private ulong m_numberOfManufacturedProducts;
        private ulong m_numberOfDiscardedProducts;
        private double m_energyConsumption;                     // in [kWh]
        private double m_pressure;                              // in [mbar]
        private ulong m_idealCycleTimeDefault;                  // [ms]
        private ulong m_idealCycleTimeMinimum;                  // [ms]
        private StationStatus m_currentStationStatus;

        private Timer m_simulationTimer = null;
        private ISystemContext m_simulationContext;
        private Random m_random;

        protected override void OnAfterCreate(ISystemContext context, NodeState node)
        {
            base.OnAfterCreate(context, node);

            m_numberOfManufacturedProducts = 0;
            m_numberOfDiscardedProducts = 0;
            m_energyConsumption = 0;
            m_pressure = c_pressureDefault;

            m_stationStartTime = DateTime.Now;
            m_faultClock.Reset();
            m_pressureStableStartTime = DateTime.Now;

            m_idealCycleTimeDefault = Program.CycleTime * 1000;
            m_idealCycleTimeMinimum = m_idealCycleTimeDefault / 2;
            m_stationTelemetry.IdealCycleTime.Value = m_idealCycleTimeDefault;
            m_actualCycleTime = m_idealCycleTimeDefault;

            StationCommands.Execute.OnCallMethod = Execute;
            StationCommands.Reset.OnCallMethod = Reset;
            StationCommands.OpenPressureReleaseValve.OnCallMethod = OpenPressureReleaseValve;

            m_simulationContext = context;
            m_random = new Random();

            m_currentStationStatus = StationStatus.Ready;

            UpdateNodeValues();
        }

        private ServiceResult Execute(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_serialNumber = (ulong)inputArguments[0];

            m_cycleStartTime = DateTime.Now;

            m_currentStationStatus = StationStatus.WorkInProgress;

            ulong idealCycleTime = m_stationTelemetry.IdealCycleTime.Value;
            if (idealCycleTime < m_idealCycleTimeMinimum)
            {
                m_stationTelemetry.IdealCycleTime.Value =
                idealCycleTime = m_idealCycleTimeMinimum;
            }
            int cycleTime = (int)(idealCycleTime + Convert.ToUInt32(Math.Abs((double)idealCycleTime * NormalDistribution(m_random, 0.0, 0.1))));

            bool stationFailure = (NormalDistribution(m_random, 0.0, 1.0) > 3.0);
            if (stationFailure)
            {
                // the simulated cycle will take longer when the station fails
                cycleTime = c_failureCycleTime + Convert.ToInt32(Math.Abs((double)c_failureCycleTime * NormalDistribution(m_random, 0.0, 1.0)));
            }

            m_simulationTimer = new Timer(SimulationFinished, stationFailure, cycleTime, Timeout.Infinite);

            UpdateNodeValues();

            return ServiceResult.Good;
        }

        private void SimulationFinished(object state)
        {
            CalculateSimulationResult((bool)state);
            UpdateNodeValues();
            m_simulationTimer.Dispose();
        }

        private ServiceResult Reset(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_faultClock.Stop();

            m_currentStationStatus = StationStatus.Ready;
            UpdateNodeValues();

            return ServiceResult.Good;
        }

        private ServiceResult OpenPressureReleaseValve(ISystemContext context, MethodState method, IList<object> inputArguments, IList<object> outputArguments)
        {
            m_pressure = c_pressureDefault;
            m_pressureStableStartTime = DateTime.Now;

            return ServiceResult.Good;
        }

        private void UpdateNodeValues()
        {
            DateTime Now = DateTime.Now;

            m_stationProduct.ProductSerialNumber.Value = m_serialNumber;
            m_stationProduct.NumberOfManufacturedProducts.Value = m_numberOfManufacturedProducts;
            m_stationProduct.NumberOfDiscardedProducts.Value = m_numberOfDiscardedProducts;

            if (!m_faultClock.IsRunning)
            {
                m_stationTelemetry.FaultyTime.Value = (ulong)m_faultClock.ElapsedMilliseconds;
                if (m_faultClock.ElapsedMilliseconds != 0)
                {
                    m_faultClock.Reset();
                }
            }
            m_stationTelemetry.Status.Value = m_currentStationStatus;
            m_stationTelemetry.EnergyConsumption.Value = m_energyConsumption;                   // [kWh]
            m_stationTelemetry.Pressure.Value = m_pressure;                                     // [mbar]

            m_stationTelemetry.ActualCycleTime.Value = m_actualCycleTime;

            // update source timestamps if a value was changed
            List<BaseInstanceState> m_telemetryList = new List<BaseInstanceState>();
            m_stationProduct.GetChildren(m_simulationContext, m_telemetryList);
            m_stationTelemetry.GetChildren(m_simulationContext, m_telemetryList);
            foreach (BaseInstanceState children in m_telemetryList)
            {
                if ((children.ChangeMasks & NodeStateChangeMasks.Value) != 0)
                {
                    BaseDataVariableState dataVariable = children as BaseDataVariableState;
                    if (dataVariable != null)
                    {
                        dataVariable.Timestamp = Now;
                    }
                }
            }

            ClearChangeMasks(m_simulationContext, true);
        }

        public virtual void CalculateSimulationResult(bool stationFailure)
        {
            bool productDiscarded = (NormalDistribution(m_random, 0.0, 1.0) > 2.0);

            if (stationFailure)
            {
                m_numberOfDiscardedProducts++;
                m_currentStationStatus = StationStatus.Fault;
                m_faultClock.Start();
            }
            else if (productDiscarded)
            {
                m_currentStationStatus = StationStatus.Discarded;
                m_numberOfDiscardedProducts++;
            }
            else
            {
                m_currentStationStatus = StationStatus.Done;
                m_numberOfManufacturedProducts++;
            }

            m_actualCycleTime = (ulong)(DateTime.Now - m_cycleStartTime).TotalMilliseconds;

            double idealCycleTime = m_stationTelemetry.IdealCycleTime.Value;

            // The power consumption of the station increases exponentially if the ideal cycle time is reduced below the default ideal cycle time 
            double cycleTimeModifier = (1 / Math.E) * (1 / Math.Exp(-(double)m_idealCycleTimeDefault / idealCycleTime));
            double powerConsumption = Program.PowerConsumption * cycleTimeModifier;

            // assume the station consumes only power during the active cycle
            // energy consumption [kWh] = (PowerConsumption [kW] * actualCycleTime [s]) / 3600
            m_energyConsumption = (powerConsumption * ((double)m_actualCycleTime / 1000.0)) / 3600.0;

            // For stations configured to generate alerts, calculate pressure
            // Pressure will be stable for c_pressureStableTime and then will increase to c_pressureHigh and stay there until OpenPressureReleaseValve() is called
            if (Program.GenerateAlerts && (((DateTime.Now - m_pressureStableStartTime).TotalMilliseconds) > c_pressureStableTime))
            {
                // slowly increase pressure until c_pressureHigh is reached
                m_pressure += NormalDistribution(m_random, (cycleTimeModifier - 1.0) * 10.0, 10.0);

                if (m_pressure <= c_pressureDefault)
                    m_pressure = c_pressureDefault * NormalDistribution(m_random, 0.0, 10.0);
                if (m_pressure >= c_pressureHigh)
                    m_pressure = c_pressureHigh * NormalDistribution(m_random, 0.0, 10.0);
            }
        }

        private double NormalDistribution(Random rand, double mean, double stdDev)
        {
            // it's possible to convert a generic normal distribution function f(x) to a standard
            // normal distribution (a normal distribution with mean=0 and stdDev=1) with the
            // following formula:
            //
            //  z = (x - mean) / stdDev
            //
            // then with z value you can retrieve the probability value P(X>x) from the standard
            // normal distribution table 

            // these are uniform(0,1) random doubles
            double u1 = rand.NextDouble();
            double u2 = rand.NextDouble();

            // random normal(0,1)
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

            // random normal(mean,stdDev^2)
            return mean + stdDev * randStdNormal;
        }
    }
}
