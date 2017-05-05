/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;
using System.Runtime.Serialization;
using Opc.Ua;

namespace Station
{
    #region StationProductState Class
    #if (!OPCUA_EXCLUDE_StationProductState)
    /// <summary>
    /// Stores an instance of the StationProductType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class StationProductState : BaseObjectState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public StationProductState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Station.ObjectTypes.StationProductType, Station.Namespaces.Station, namespaceUris);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context)
        {
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        protected override void Initialize(ISystemContext context, NodeState source)
        {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        /// <summary>
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvU3RhdGlvbi//////BGCAAAEAAAABABoA" +
           "AABTdGF0aW9uUHJvZHVjdFR5cGVJbnN0YW5jZQEBAQABAQEA/////wMAAAAVYIkKAgAAAAEAEwAAAFBy" +
           "b2R1Y3RTZXJpYWxOdW1iZXIBAQIAAC8APwIAAAAACf////8BAf////8AAAAAFWCJCgIAAAABABwAAABO" +
           "dW1iZXJPZk1hbnVmYWN0dXJlZFByb2R1Y3RzAQEIAAAvAD8IAAAAAAn/////AQH/////AAAAABVgiQoC" +
           "AAAAAQAZAAAATnVtYmVyT2ZEaXNjYXJkZWRQcm9kdWN0cwEBDgAALwA/DgAAAAAJ/////wEB/////wAA" +
           "AAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the ProductSerialNumber Variable.
        /// </summary>
        public BaseDataVariableState<ulong> ProductSerialNumber
        {
            get
            {
                return m_productSerialNumber;
            }

            set
            {
                if (!Object.ReferenceEquals(m_productSerialNumber, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_productSerialNumber = value;
            }
        }

        /// <summary>
        /// A description for the NumberOfManufacturedProducts Variable.
        /// </summary>
        public BaseDataVariableState<ulong> NumberOfManufacturedProducts
        {
            get
            {
                return m_numberOfManufacturedProducts;
            }

            set
            {
                if (!Object.ReferenceEquals(m_numberOfManufacturedProducts, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_numberOfManufacturedProducts = value;
            }
        }

        /// <summary>
        /// A description for the NumberOfDiscardedProducts Variable.
        /// </summary>
        public BaseDataVariableState<ulong> NumberOfDiscardedProducts
        {
            get
            {
                return m_numberOfDiscardedProducts;
            }

            set
            {
                if (!Object.ReferenceEquals(m_numberOfDiscardedProducts, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_numberOfDiscardedProducts = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <summary>
        /// Populates a list with the children that belong to the node.
        /// </summary>
        /// <param name="context">The context for the system being accessed.</param>
        /// <param name="children">The list of children to populate.</param>
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_productSerialNumber != null)
            {
                children.Add(m_productSerialNumber);
            }

            if (m_numberOfManufacturedProducts != null)
            {
                children.Add(m_numberOfManufacturedProducts);
            }

            if (m_numberOfDiscardedProducts != null)
            {
                children.Add(m_numberOfDiscardedProducts);
            }

            base.GetChildren(context, children);
        }

        /// <summary>
        /// Finds the child with the specified browse name.
        /// </summary>
        protected override BaseInstanceState FindChild(
            ISystemContext context,
            QualifiedName browseName,
            bool createOrReplace,
            BaseInstanceState replacement)
        {
            if (QualifiedName.IsNull(browseName))
            {
                return null;
            }

            BaseInstanceState instance = null;

            switch (browseName.Name)
            {
                case Station.BrowseNames.ProductSerialNumber:
                {
                    if (createOrReplace)
                    {
                        if (ProductSerialNumber == null)
                        {
                            if (replacement == null)
                            {
                                ProductSerialNumber = new BaseDataVariableState<ulong>(this);
                            }
                            else
                            {
                                ProductSerialNumber = (BaseDataVariableState<ulong>)replacement;
                            }
                        }
                    }

                    instance = ProductSerialNumber;
                    break;
                }

                case Station.BrowseNames.NumberOfManufacturedProducts:
                {
                    if (createOrReplace)
                    {
                        if (NumberOfManufacturedProducts == null)
                        {
                            if (replacement == null)
                            {
                                NumberOfManufacturedProducts = new BaseDataVariableState<ulong>(this);
                            }
                            else
                            {
                                NumberOfManufacturedProducts = (BaseDataVariableState<ulong>)replacement;
                            }
                        }
                    }

                    instance = NumberOfManufacturedProducts;
                    break;
                }

                case Station.BrowseNames.NumberOfDiscardedProducts:
                {
                    if (createOrReplace)
                    {
                        if (NumberOfDiscardedProducts == null)
                        {
                            if (replacement == null)
                            {
                                NumberOfDiscardedProducts = new BaseDataVariableState<ulong>(this);
                            }
                            else
                            {
                                NumberOfDiscardedProducts = (BaseDataVariableState<ulong>)replacement;
                            }
                        }
                    }

                    instance = NumberOfDiscardedProducts;
                    break;
                }
            }

            if (instance != null)
            {
                return instance;
            }

            return base.FindChild(context, browseName, createOrReplace, replacement);
        }
        #endregion

        #region Private Fields
        private BaseDataVariableState<ulong> m_productSerialNumber;
        private BaseDataVariableState<ulong> m_numberOfManufacturedProducts;
        private BaseDataVariableState<ulong> m_numberOfDiscardedProducts;
        #endregion
    }
    #endif
    #endregion

    #region TelemetryState Class
    #if (!OPCUA_EXCLUDE_TelemetryState)
    /// <summary>
    /// Stores an instance of the TelemetryType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class TelemetryState : BaseObjectState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public TelemetryState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Station.ObjectTypes.TelemetryType, Station.Namespaces.Station, namespaceUris);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context)
        {
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        protected override void Initialize(ISystemContext context, NodeState source)
        {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        /// <summary>
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvU3RhdGlvbi//////BGCAAAEAAAABABUA" +
           "AABUZWxlbWV0cnlUeXBlSW5zdGFuY2UBARQAAQEUAP////8HAAAAFWCJCgIAAAABABIAAABPdmVyYWxs" +
           "UnVubmluZ1RpbWUBARUAAC8APxUAAAAACf////8BAf////8AAAAAFWCJCgIAAAABAAoAAABGYXVsdHlU" +
           "aW1lAQEWAAAvAD8WAAAAAAn/////AQH/////AAAAABVgiQoCAAAAAQAGAAAAU3RhdHVzAQFnAQAvAD9n" +
           "AQAAABv/////AQH/////AAAAABVgiQoCAAAAAQARAAAARW5lcmd5Q29uc3VtcHRpb24BAR0AAC8APx0A" +
           "AAAAC/////8BAf////8AAAAAFWCJCgIAAAABAAgAAABQcmVzc3VyZQEBrAEALwA/rAEAAAAL/////wEB" +
           "/////wAAAAAVYIkKAgAAAAEADgAAAElkZWFsQ3ljbGVUaW1lAQEjAAAvAD8jAAAAAAn/////AwP/////" +
           "AAAAABVgiQoCAAAAAQAPAAAAQWN0dWFsQ3ljbGVUaW1lAQEpAAAvAD8pAAAAAAn/////AQH/////AAAA" +
           "AA==";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the OverallRunningTime Variable.
        /// </summary>
        public BaseDataVariableState<ulong> OverallRunningTime
        {
            get
            {
                return m_overallRunningTime;
            }

            set
            {
                if (!Object.ReferenceEquals(m_overallRunningTime, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_overallRunningTime = value;
            }
        }

        /// <summary>
        /// A description for the FaultyTime Variable.
        /// </summary>
        public BaseDataVariableState<ulong> FaultyTime
        {
            get
            {
                return m_faultyTime;
            }

            set
            {
                if (!Object.ReferenceEquals(m_faultyTime, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_faultyTime = value;
            }
        }

        /// <summary>
        /// A description for the Status Variable.
        /// </summary>
        public BaseDataVariableState Status
        {
            get
            {
                return m_status;
            }

            set
            {
                if (!Object.ReferenceEquals(m_status, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_status = value;
            }
        }

        /// <summary>
        /// A description for the EnergyConsumption Variable.
        /// </summary>
        public BaseDataVariableState<double> EnergyConsumption
        {
            get
            {
                return m_energyConsumption;
            }

            set
            {
                if (!Object.ReferenceEquals(m_energyConsumption, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_energyConsumption = value;
            }
        }

        /// <summary>
        /// A description for the Pressure Variable.
        /// </summary>
        public BaseDataVariableState<double> Pressure
        {
            get
            {
                return m_pressure;
            }

            set
            {
                if (!Object.ReferenceEquals(m_pressure, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_pressure = value;
            }
        }

        /// <summary>
        /// A description for the IdealCycleTime Variable.
        /// </summary>
        public BaseDataVariableState<ulong> IdealCycleTime
        {
            get
            {
                return m_idealCycleTime;
            }

            set
            {
                if (!Object.ReferenceEquals(m_idealCycleTime, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_idealCycleTime = value;
            }
        }

        /// <summary>
        /// A description for the ActualCycleTime Variable.
        /// </summary>
        public BaseDataVariableState<ulong> ActualCycleTime
        {
            get
            {
                return m_actualCycleTime;
            }

            set
            {
                if (!Object.ReferenceEquals(m_actualCycleTime, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_actualCycleTime = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <summary>
        /// Populates a list with the children that belong to the node.
        /// </summary>
        /// <param name="context">The context for the system being accessed.</param>
        /// <param name="children">The list of children to populate.</param>
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_overallRunningTime != null)
            {
                children.Add(m_overallRunningTime);
            }

            if (m_faultyTime != null)
            {
                children.Add(m_faultyTime);
            }

            if (m_status != null)
            {
                children.Add(m_status);
            }

            if (m_energyConsumption != null)
            {
                children.Add(m_energyConsumption);
            }

            if (m_pressure != null)
            {
                children.Add(m_pressure);
            }

            if (m_idealCycleTime != null)
            {
                children.Add(m_idealCycleTime);
            }

            if (m_actualCycleTime != null)
            {
                children.Add(m_actualCycleTime);
            }

            base.GetChildren(context, children);
        }

        /// <summary>
        /// Finds the child with the specified browse name.
        /// </summary>
        protected override BaseInstanceState FindChild(
            ISystemContext context,
            QualifiedName browseName,
            bool createOrReplace,
            BaseInstanceState replacement)
        {
            if (QualifiedName.IsNull(browseName))
            {
                return null;
            }

            BaseInstanceState instance = null;

            switch (browseName.Name)
            {
                case Station.BrowseNames.OverallRunningTime:
                {
                    if (createOrReplace)
                    {
                        if (OverallRunningTime == null)
                        {
                            if (replacement == null)
                            {
                                OverallRunningTime = new BaseDataVariableState<ulong>(this);
                            }
                            else
                            {
                                OverallRunningTime = (BaseDataVariableState<ulong>)replacement;
                            }
                        }
                    }

                    instance = OverallRunningTime;
                    break;
                }

                case Station.BrowseNames.FaultyTime:
                {
                    if (createOrReplace)
                    {
                        if (FaultyTime == null)
                        {
                            if (replacement == null)
                            {
                                FaultyTime = new BaseDataVariableState<ulong>(this);
                            }
                            else
                            {
                                FaultyTime = (BaseDataVariableState<ulong>)replacement;
                            }
                        }
                    }

                    instance = FaultyTime;
                    break;
                }

                case Station.BrowseNames.Status:
                {
                    if (createOrReplace)
                    {
                        if (Status == null)
                        {
                            if (replacement == null)
                            {
                                Status = new BaseDataVariableState(this);
                            }
                            else
                            {
                                Status = (BaseDataVariableState)replacement;
                            }
                        }
                    }

                    instance = Status;
                    break;
                }

                case Station.BrowseNames.EnergyConsumption:
                {
                    if (createOrReplace)
                    {
                        if (EnergyConsumption == null)
                        {
                            if (replacement == null)
                            {
                                EnergyConsumption = new BaseDataVariableState<double>(this);
                            }
                            else
                            {
                                EnergyConsumption = (BaseDataVariableState<double>)replacement;
                            }
                        }
                    }

                    instance = EnergyConsumption;
                    break;
                }

                case Station.BrowseNames.Pressure:
                {
                    if (createOrReplace)
                    {
                        if (Pressure == null)
                        {
                            if (replacement == null)
                            {
                                Pressure = new BaseDataVariableState<double>(this);
                            }
                            else
                            {
                                Pressure = (BaseDataVariableState<double>)replacement;
                            }
                        }
                    }

                    instance = Pressure;
                    break;
                }

                case Station.BrowseNames.IdealCycleTime:
                {
                    if (createOrReplace)
                    {
                        if (IdealCycleTime == null)
                        {
                            if (replacement == null)
                            {
                                IdealCycleTime = new BaseDataVariableState<ulong>(this);
                            }
                            else
                            {
                                IdealCycleTime = (BaseDataVariableState<ulong>)replacement;
                            }
                        }
                    }

                    instance = IdealCycleTime;
                    break;
                }

                case Station.BrowseNames.ActualCycleTime:
                {
                    if (createOrReplace)
                    {
                        if (ActualCycleTime == null)
                        {
                            if (replacement == null)
                            {
                                ActualCycleTime = new BaseDataVariableState<ulong>(this);
                            }
                            else
                            {
                                ActualCycleTime = (BaseDataVariableState<ulong>)replacement;
                            }
                        }
                    }

                    instance = ActualCycleTime;
                    break;
                }
            }

            if (instance != null)
            {
                return instance;
            }

            return base.FindChild(context, browseName, createOrReplace, replacement);
        }
        #endregion

        #region Private Fields
        private BaseDataVariableState<ulong> m_overallRunningTime;
        private BaseDataVariableState<ulong> m_faultyTime;
        private BaseDataVariableState m_status;
        private BaseDataVariableState<double> m_energyConsumption;
        private BaseDataVariableState<double> m_pressure;
        private BaseDataVariableState<ulong> m_idealCycleTime;
        private BaseDataVariableState<ulong> m_actualCycleTime;
        #endregion
    }
    #endif
    #endregion

    #region ExecuteMethodState Class
    #if (!OPCUA_EXCLUDE_ExecuteMethodState)
    /// <summary>
    /// Stores an instance of the ExecuteMethodType Method.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class ExecuteMethodState : MethodState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public ExecuteMethodState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Constructs an instance of a node.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>The new node.</returns>
        public new static NodeState Construct(NodeState parent)
        {
            return new ExecuteMethodState(parent);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context)
        {
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        /// <summary>
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvU3RhdGlvbi//////BGGCCgQAAAABABEA" +
           "AABFeGVjdXRlTWV0aG9kVHlwZQEBrQEALwEBrQGtAQAAAQH/////AQAAABVgqQoCAAAAAAAOAAAASW5w" +
           "dXRBcmd1bWVudHMBAa4BAC4ARK4BAACWAQAAAAEAKgEBUwAAAAwAAABTZXJpYWxOdW1iZXIACf////8A" +
           "AAAAAwAAAAAwAAAAVGhlIHNlcmlhbCBudW1iZXIgb2YgdGhlIHBhcnQgdG8gYmUgbWFudWZhY3R1cmVk" +
           "AQAoAQEAAAABAf////8AAAAA";
        #endregion
        #endif
        #endregion

        #region Event Callbacks
        /// <summary>
        /// Raised when the the method is called.
        /// </summary>
        public ExecuteMethodStateMethodCallHandler OnCall;
        #endregion

        #region Public Properties
        #endregion

        #region Overridden Methods
        /// <summary>
        /// Invokes the method, returns the result and output argument.
        /// </summary>
        /// <param name="context">The current context.</param>
        /// <param name="objectId">The id of the object.</param>
        /// <param name="inputArguments">The input arguments which have been already validated.</param>
        /// <param name="outputArguments">The output arguments which have initialized with thier default values.</param>
        protected override ServiceResult Call(
            ISystemContext context,
            NodeId objectId,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            if (OnCall == null)
            {
                return base.Call(context, objectId, inputArguments, outputArguments);
            }

            ServiceResult result = null;

            ulong serialNumber = (ulong)inputArguments[0];

            if (OnCall != null)
            {
                result = OnCall(
                    context,
                    this,
                    objectId,
                    serialNumber);
            }

            return result;
        }
        #endregion

        #region Private Fields
        #endregion
    }

    /// <summary>
    /// Used to receive notifications when the method is called.
    /// </summary>
    /// <exclude />
    public delegate ServiceResult ExecuteMethodStateMethodCallHandler(
        ISystemContext context,
        MethodState method,
        NodeId objectId,
        ulong serialNumber);
    #endif
    #endregion

    #region StationCommandsState Class
    #if (!OPCUA_EXCLUDE_StationCommandsState)
    /// <summary>
    /// Stores an instance of the StationCommandsType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class StationCommandsState : BaseObjectState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public StationCommandsState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Station.ObjectTypes.StationCommandsType, Station.Namespaces.Station, namespaceUris);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context)
        {
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        protected override void Initialize(ISystemContext context, NodeState source)
        {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        /// <summary>
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvU3RhdGlvbi//////BGCAAAEAAAABABsA" +
           "AABTdGF0aW9uQ29tbWFuZHNUeXBlSW5zdGFuY2UBATEAAQExAP////8DAAAABGGCCgQAAAABAAcAAABF" +
           "eGVjdXRlAQEzAAAvAQEzADMAAAABAf////8BAAAAFWCpCgIAAAAAAA4AAABJbnB1dEFyZ3VtZW50cwEB" +
           "NAAALgBENAAAAJYBAAAAAQAqAQFTAAAADAAAAFNlcmlhbE51bWJlcgAJ/////wAAAAADAAAAADAAAABU" +
           "aGUgc2VyaWFsIG51bWJlciBvZiB0aGUgcGFydCB0byBiZSBtYW51ZmFjdHVyZWQBACgBAQAAAAEB////" +
           "/wAAAAAEYYIKBAAAAAEABQAAAFJlc2V0AQEyAAAvAQEyADIAAAABAf////8AAAAABGGCCgQAAAABABgA" +
           "AABPcGVuUHJlc3N1cmVSZWxlYXNlVmFsdmUBAa8BAC8BAa8BrwEAAAEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the ExecuteMethodType Method.
        /// </summary>
        public ExecuteMethodState Execute
        {
            get
            {
                return m_executeMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_executeMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_executeMethod = value;
            }
        }

        /// <summary>
        /// A description for the Reset Method.
        /// </summary>
        public MethodState Reset
        {
            get
            {
                return m_resetMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_resetMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_resetMethod = value;
            }
        }

        /// <summary>
        /// A description for the OpenPressureReleaseValve Method.
        /// </summary>
        public MethodState OpenPressureReleaseValve
        {
            get
            {
                return m_openPressureReleaseValveMethod;
            }

            set
            {
                if (!Object.ReferenceEquals(m_openPressureReleaseValveMethod, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_openPressureReleaseValveMethod = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <summary>
        /// Populates a list with the children that belong to the node.
        /// </summary>
        /// <param name="context">The context for the system being accessed.</param>
        /// <param name="children">The list of children to populate.</param>
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_executeMethod != null)
            {
                children.Add(m_executeMethod);
            }

            if (m_resetMethod != null)
            {
                children.Add(m_resetMethod);
            }

            if (m_openPressureReleaseValveMethod != null)
            {
                children.Add(m_openPressureReleaseValveMethod);
            }

            base.GetChildren(context, children);
        }

        /// <summary>
        /// Finds the child with the specified browse name.
        /// </summary>
        protected override BaseInstanceState FindChild(
            ISystemContext context,
            QualifiedName browseName,
            bool createOrReplace,
            BaseInstanceState replacement)
        {
            if (QualifiedName.IsNull(browseName))
            {
                return null;
            }

            BaseInstanceState instance = null;

            switch (browseName.Name)
            {
                case Station.BrowseNames.Execute:
                {
                    if (createOrReplace)
                    {
                        if (Execute == null)
                        {
                            if (replacement == null)
                            {
                                Execute = new ExecuteMethodState(this);
                            }
                            else
                            {
                                Execute = (ExecuteMethodState)replacement;
                            }
                        }
                    }

                    instance = Execute;
                    break;
                }

                case Station.BrowseNames.Reset:
                {
                    if (createOrReplace)
                    {
                        if (Reset == null)
                        {
                            if (replacement == null)
                            {
                                Reset = new MethodState(this);
                            }
                            else
                            {
                                Reset = (MethodState)replacement;
                            }
                        }
                    }

                    instance = Reset;
                    break;
                }

                case Station.BrowseNames.OpenPressureReleaseValve:
                {
                    if (createOrReplace)
                    {
                        if (OpenPressureReleaseValve == null)
                        {
                            if (replacement == null)
                            {
                                OpenPressureReleaseValve = new MethodState(this);
                            }
                            else
                            {
                                OpenPressureReleaseValve = (MethodState)replacement;
                            }
                        }
                    }

                    instance = OpenPressureReleaseValve;
                    break;
                }
            }

            if (instance != null)
            {
                return instance;
            }

            return base.FindChild(context, browseName, createOrReplace, replacement);
        }
        #endregion

        #region Private Fields
        private ExecuteMethodState m_executeMethod;
        private MethodState m_resetMethod;
        private MethodState m_openPressureReleaseValveMethod;
        #endregion
    }
    #endif
    #endregion

    #region StationState Class
    #if (!OPCUA_EXCLUDE_StationState)
    /// <summary>
    /// Stores an instance of the StationType ObjectType.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public partial class StationState : BaseObjectState
    {
        #region Constructors
        /// <summary>
        /// Initializes the type with its default attribute values.
        /// </summary>
        public StationState(NodeState parent) : base(parent)
        {
        }

        /// <summary>
        /// Returns the id of the default type definition node for the instance.
        /// </summary>
        protected override NodeId GetDefaultTypeDefinitionId(NamespaceTable namespaceUris)
        {
            return Opc.Ua.NodeId.Create(Station.ObjectTypes.StationType, Station.Namespaces.Station, namespaceUris);
        }

        #if (!OPCUA_EXCLUDE_InitializationStrings)
        /// <summary>
        /// Initializes the instance.
        /// </summary>
        protected override void Initialize(ISystemContext context)
        {
            Initialize(context, InitializationString);
            InitializeOptionalChildren(context);
        }

        protected override void Initialize(ISystemContext context, NodeState source)
        {
            InitializeOptionalChildren(context);
            base.Initialize(context, source);
        }

        /// <summary>
        /// Initializes the any option children defined for the instance.
        /// </summary>
        protected override void InitializeOptionalChildren(ISystemContext context)
        {
            base.InitializeOptionalChildren(context);
        }

        #region Initialization String
        private const string InitializationString =
           "AQAAACQAAABodHRwOi8vb3BjZm91bmRhdGlvbi5vcmcvVUEvU3RhdGlvbi//////hGCAAAEAAAABABMA" +
           "AABTdGF0aW9uVHlwZUluc3RhbmNlAQEBAQEBAQEB/////wMAAACEYIAKAQAAAAEADgAAAFN0YXRpb25Q" +
           "cm9kdWN0AQECAQAvAQEBAAIBAAAB/////wMAAAAVYIkKAgAAAAEAEwAAAFByb2R1Y3RTZXJpYWxOdW1i" +
           "ZXIBAQMBAC8APwMBAAAACf////8BAf////8AAAAAFWCJCgIAAAABABwAAABOdW1iZXJPZk1hbnVmYWN0" +
           "dXJlZFByb2R1Y3RzAQEJAQAvAD8JAQAAAAn/////AQH/////AAAAABVgiQoCAAAAAQAZAAAATnVtYmVy" +
           "T2ZEaXNjYXJkZWRQcm9kdWN0cwEBDwEALwA/DwEAAAAJ/////wEB/////wAAAACEYIAKAQAAAAEAEAAA" +
           "AFN0YXRpb25UZWxlbWV0cnkBARUBAC8BARQAFQEAAAH/////BwAAABVgiQoCAAAAAQASAAAAT3ZlcmFs" +
           "bFJ1bm5pbmdUaW1lAQEWAQAvAD8WAQAAAAn/////AQH/////AAAAABVgiQoCAAAAAQAKAAAARmF1bHR5" +
           "VGltZQEBFwEALwA/FwEAAAAJ/////wEB/////wAAAAAVYIkKAgAAAAEABgAAAFN0YXR1cwEBbQEALwA/" +
           "bQEAAAAb/////wEB/////wAAAAAVYIkKAgAAAAEAEQAAAEVuZXJneUNvbnN1bXB0aW9uAQEeAQAvAD8e" +
           "AQAAAAv/////AQH/////AAAAABVgiQoCAAAAAQAIAAAAUHJlc3N1cmUBAbABAC8AP7ABAAAAC/////8B" +
           "Af////8AAAAAFWCJCgIAAAABAA4AAABJZGVhbEN5Y2xlVGltZQEBJAEALwA/JAEAAAAJ/////wMD////" +
           "/wAAAAAVYIkKAgAAAAEADwAAAEFjdHVhbEN5Y2xlVGltZQEBKgEALwA/KgEAAAAJ/////wEB/////wAA" +
           "AACEYIAKAQAAAAEADwAAAFN0YXRpb25Db21tYW5kcwEBMAEALwEBMQAwAQAAAf////8DAAAABGGCCgQA" +
           "AAABAAcAAABFeGVjdXRlAQEyAQAvAQEzADIBAAABAf////8BAAAAFWCpCgIAAAAAAA4AAABJbnB1dEFy" +
           "Z3VtZW50cwEBMwEALgBEMwEAAJYBAAAAAQAqAQFTAAAADAAAAFNlcmlhbE51bWJlcgAJ/////wAAAAAD" +
           "AAAAADAAAABUaGUgc2VyaWFsIG51bWJlciBvZiB0aGUgcGFydCB0byBiZSBtYW51ZmFjdHVyZWQBACgB" +
           "AQAAAAEB/////wAAAAAEYYIKBAAAAAEABQAAAFJlc2V0AQExAQAvAQEyADEBAAABAf////8AAAAABGGC" +
           "CgQAAAABABgAAABPcGVuUHJlc3N1cmVSZWxlYXNlVmFsdmUBAbEBAC8BAa8BsQEAAAEB/////wAAAAA=";
        #endregion
        #endif
        #endregion

        #region Public Properties
        /// <summary>
        /// A description for the StationProduct Object.
        /// </summary>
        public StationProductState StationProduct
        {
            get
            {
                return m_stationProduct;
            }

            set
            {
                if (!Object.ReferenceEquals(m_stationProduct, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_stationProduct = value;
            }
        }

        /// <summary>
        /// A description for the StationTelemetry Object.
        /// </summary>
        public TelemetryState StationTelemetry
        {
            get
            {
                return m_stationTelemetry;
            }

            set
            {
                if (!Object.ReferenceEquals(m_stationTelemetry, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_stationTelemetry = value;
            }
        }

        /// <summary>
        /// A description for the StationCommands Object.
        /// </summary>
        public StationCommandsState StationCommands
        {
            get
            {
                return m_stationCommands;
            }

            set
            {
                if (!Object.ReferenceEquals(m_stationCommands, value))
                {
                    ChangeMasks |= NodeStateChangeMasks.Children;
                }

                m_stationCommands = value;
            }
        }
        #endregion

        #region Overridden Methods
        /// <summary>
        /// Populates a list with the children that belong to the node.
        /// </summary>
        /// <param name="context">The context for the system being accessed.</param>
        /// <param name="children">The list of children to populate.</param>
        public override void GetChildren(
            ISystemContext context,
            IList<BaseInstanceState> children)
        {
            if (m_stationProduct != null)
            {
                children.Add(m_stationProduct);
            }

            if (m_stationTelemetry != null)
            {
                children.Add(m_stationTelemetry);
            }

            if (m_stationCommands != null)
            {
                children.Add(m_stationCommands);
            }

            base.GetChildren(context, children);
        }

        /// <summary>
        /// Finds the child with the specified browse name.
        /// </summary>
        protected override BaseInstanceState FindChild(
            ISystemContext context,
            QualifiedName browseName,
            bool createOrReplace,
            BaseInstanceState replacement)
        {
            if (QualifiedName.IsNull(browseName))
            {
                return null;
            }

            BaseInstanceState instance = null;

            switch (browseName.Name)
            {
                case Station.BrowseNames.StationProduct:
                {
                    if (createOrReplace)
                    {
                        if (StationProduct == null)
                        {
                            if (replacement == null)
                            {
                                StationProduct = new StationProductState(this);
                            }
                            else
                            {
                                StationProduct = (StationProductState)replacement;
                            }
                        }
                    }

                    instance = StationProduct;
                    break;
                }

                case Station.BrowseNames.StationTelemetry:
                {
                    if (createOrReplace)
                    {
                        if (StationTelemetry == null)
                        {
                            if (replacement == null)
                            {
                                StationTelemetry = new TelemetryState(this);
                            }
                            else
                            {
                                StationTelemetry = (TelemetryState)replacement;
                            }
                        }
                    }

                    instance = StationTelemetry;
                    break;
                }

                case Station.BrowseNames.StationCommands:
                {
                    if (createOrReplace)
                    {
                        if (StationCommands == null)
                        {
                            if (replacement == null)
                            {
                                StationCommands = new StationCommandsState(this);
                            }
                            else
                            {
                                StationCommands = (StationCommandsState)replacement;
                            }
                        }
                    }

                    instance = StationCommands;
                    break;
                }
            }

            if (instance != null)
            {
                return instance;
            }

            return base.FindChild(context, browseName, createOrReplace, replacement);
        }
        #endregion

        #region Private Fields
        private StationProductState m_stationProduct;
        private TelemetryState m_stationTelemetry;
        private StationCommandsState m_stationCommands;
        #endregion
    }
    #endif
    #endregion
}