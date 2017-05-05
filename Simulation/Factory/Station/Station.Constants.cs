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
    #region Method Identifiers
    /// <summary>
    /// A class that declares constants for all Methods in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Methods
    {
        /// <summary>
        /// The identifier for the StationCommandsType_Execute Method.
        /// </summary>
        public const uint StationCommandsType_Execute = 51;

        /// <summary>
        /// The identifier for the StationCommandsType_Reset Method.
        /// </summary>
        public const uint StationCommandsType_Reset = 50;

        /// <summary>
        /// The identifier for the StationCommandsType_OpenPressureReleaseValve Method.
        /// </summary>
        public const uint StationCommandsType_OpenPressureReleaseValve = 431;

        /// <summary>
        /// The identifier for the StationType_StationCommands_Execute Method.
        /// </summary>
        public const uint StationType_StationCommands_Execute = 306;

        /// <summary>
        /// The identifier for the StationType_StationCommands_Reset Method.
        /// </summary>
        public const uint StationType_StationCommands_Reset = 305;

        /// <summary>
        /// The identifier for the StationType_StationCommands_OpenPressureReleaseValve Method.
        /// </summary>
        public const uint StationType_StationCommands_OpenPressureReleaseValve = 433;

        /// <summary>
        /// The identifier for the StationInstance_StationCommands_Execute Method.
        /// </summary>
        public const uint StationInstance_StationCommands_Execute = 426;

        /// <summary>
        /// The identifier for the StationInstance_StationCommands_Reset Method.
        /// </summary>
        public const uint StationInstance_StationCommands_Reset = 425;

        /// <summary>
        /// The identifier for the StationInstance_StationCommands_OpenPressureReleaseValve Method.
        /// </summary>
        public const uint StationInstance_StationCommands_OpenPressureReleaseValve = 435;
    }
    #endregion

    #region Object Identifiers
    /// <summary>
    /// A class that declares constants for all Objects in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Objects
    {
        /// <summary>
        /// The identifier for the StationType_StationProduct Object.
        /// </summary>
        public const uint StationType_StationProduct = 258;

        /// <summary>
        /// The identifier for the StationType_StationTelemetry Object.
        /// </summary>
        public const uint StationType_StationTelemetry = 277;

        /// <summary>
        /// The identifier for the StationType_StationCommands Object.
        /// </summary>
        public const uint StationType_StationCommands = 304;

        /// <summary>
        /// The identifier for the StationInstance Object.
        /// </summary>
        public const uint StationInstance = 377;

        /// <summary>
        /// The identifier for the StationInstance_StationProduct Object.
        /// </summary>
        public const uint StationInstance_StationProduct = 378;

        /// <summary>
        /// The identifier for the StationInstance_StationTelemetry Object.
        /// </summary>
        public const uint StationInstance_StationTelemetry = 397;

        /// <summary>
        /// The identifier for the StationInstance_StationCommands Object.
        /// </summary>
        public const uint StationInstance_StationCommands = 424;
    }
    #endregion

    #region ObjectType Identifiers
    /// <summary>
    /// A class that declares constants for all ObjectTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectTypes
    {
        /// <summary>
        /// The identifier for the StationProductType ObjectType.
        /// </summary>
        public const uint StationProductType = 1;

        /// <summary>
        /// The identifier for the TelemetryType ObjectType.
        /// </summary>
        public const uint TelemetryType = 20;

        /// <summary>
        /// The identifier for the StationCommandsType ObjectType.
        /// </summary>
        public const uint StationCommandsType = 49;

        /// <summary>
        /// The identifier for the StationType ObjectType.
        /// </summary>
        public const uint StationType = 257;
    }
    #endregion

    #region Variable Identifiers
    /// <summary>
    /// A class that declares constants for all Variables in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Variables
    {
        /// <summary>
        /// The identifier for the StationProductType_ProductSerialNumber Variable.
        /// </summary>
        public const uint StationProductType_ProductSerialNumber = 2;

        /// <summary>
        /// The identifier for the StationProductType_NumberOfManufacturedProducts Variable.
        /// </summary>
        public const uint StationProductType_NumberOfManufacturedProducts = 8;

        /// <summary>
        /// The identifier for the StationProductType_NumberOfDiscardedProducts Variable.
        /// </summary>
        public const uint StationProductType_NumberOfDiscardedProducts = 14;

        /// <summary>
        /// The identifier for the TelemetryType_OverallRunningTime Variable.
        /// </summary>
        public const uint TelemetryType_OverallRunningTime = 21;

        /// <summary>
        /// The identifier for the TelemetryType_FaultyTime Variable.
        /// </summary>
        public const uint TelemetryType_FaultyTime = 22;

        /// <summary>
        /// The identifier for the TelemetryType_Status Variable.
        /// </summary>
        public const uint TelemetryType_Status = 359;

        /// <summary>
        /// The identifier for the TelemetryType_EnergyConsumption Variable.
        /// </summary>
        public const uint TelemetryType_EnergyConsumption = 29;

        /// <summary>
        /// The identifier for the TelemetryType_Pressure Variable.
        /// </summary>
        public const uint TelemetryType_Pressure = 428;

        /// <summary>
        /// The identifier for the TelemetryType_IdealCycleTime Variable.
        /// </summary>
        public const uint TelemetryType_IdealCycleTime = 35;

        /// <summary>
        /// The identifier for the TelemetryType_ActualCycleTime Variable.
        /// </summary>
        public const uint TelemetryType_ActualCycleTime = 41;

        /// <summary>
        /// The identifier for the StationCommandsType_Execute_InputArguments Variable.
        /// </summary>
        public const uint StationCommandsType_Execute_InputArguments = 52;

        /// <summary>
        /// The identifier for the StationType_StationProduct_ProductSerialNumber Variable.
        /// </summary>
        public const uint StationType_StationProduct_ProductSerialNumber = 259;

        /// <summary>
        /// The identifier for the StationType_StationProduct_NumberOfManufacturedProducts Variable.
        /// </summary>
        public const uint StationType_StationProduct_NumberOfManufacturedProducts = 265;

        /// <summary>
        /// The identifier for the StationType_StationProduct_NumberOfDiscardedProducts Variable.
        /// </summary>
        public const uint StationType_StationProduct_NumberOfDiscardedProducts = 271;

        /// <summary>
        /// The identifier for the StationType_StationTelemetry_OverallRunningTime Variable.
        /// </summary>
        public const uint StationType_StationTelemetry_OverallRunningTime = 278;

        /// <summary>
        /// The identifier for the StationType_StationTelemetry_FaultyTime Variable.
        /// </summary>
        public const uint StationType_StationTelemetry_FaultyTime = 279;

        /// <summary>
        /// The identifier for the StationType_StationTelemetry_Status Variable.
        /// </summary>
        public const uint StationType_StationTelemetry_Status = 365;

        /// <summary>
        /// The identifier for the StationType_StationTelemetry_EnergyConsumption Variable.
        /// </summary>
        public const uint StationType_StationTelemetry_EnergyConsumption = 286;

        /// <summary>
        /// The identifier for the StationType_StationTelemetry_Pressure Variable.
        /// </summary>
        public const uint StationType_StationTelemetry_Pressure = 432;

        /// <summary>
        /// The identifier for the StationType_StationTelemetry_IdealCycleTime Variable.
        /// </summary>
        public const uint StationType_StationTelemetry_IdealCycleTime = 292;

        /// <summary>
        /// The identifier for the StationType_StationTelemetry_ActualCycleTime Variable.
        /// </summary>
        public const uint StationType_StationTelemetry_ActualCycleTime = 298;

        /// <summary>
        /// The identifier for the StationType_StationCommands_Execute_InputArguments Variable.
        /// </summary>
        public const uint StationType_StationCommands_Execute_InputArguments = 307;

        /// <summary>
        /// The identifier for the StationInstance_StationProduct_ProductSerialNumber Variable.
        /// </summary>
        public const uint StationInstance_StationProduct_ProductSerialNumber = 379;

        /// <summary>
        /// The identifier for the StationInstance_StationProduct_NumberOfManufacturedProducts Variable.
        /// </summary>
        public const uint StationInstance_StationProduct_NumberOfManufacturedProducts = 385;

        /// <summary>
        /// The identifier for the StationInstance_StationProduct_NumberOfDiscardedProducts Variable.
        /// </summary>
        public const uint StationInstance_StationProduct_NumberOfDiscardedProducts = 391;

        /// <summary>
        /// The identifier for the StationInstance_StationTelemetry_OverallRunningTime Variable.
        /// </summary>
        public const uint StationInstance_StationTelemetry_OverallRunningTime = 398;

        /// <summary>
        /// The identifier for the StationInstance_StationTelemetry_FaultyTime Variable.
        /// </summary>
        public const uint StationInstance_StationTelemetry_FaultyTime = 399;

        /// <summary>
        /// The identifier for the StationInstance_StationTelemetry_Status Variable.
        /// </summary>
        public const uint StationInstance_StationTelemetry_Status = 400;

        /// <summary>
        /// The identifier for the StationInstance_StationTelemetry_EnergyConsumption Variable.
        /// </summary>
        public const uint StationInstance_StationTelemetry_EnergyConsumption = 406;

        /// <summary>
        /// The identifier for the StationInstance_StationTelemetry_Pressure Variable.
        /// </summary>
        public const uint StationInstance_StationTelemetry_Pressure = 434;

        /// <summary>
        /// The identifier for the StationInstance_StationTelemetry_IdealCycleTime Variable.
        /// </summary>
        public const uint StationInstance_StationTelemetry_IdealCycleTime = 412;

        /// <summary>
        /// The identifier for the StationInstance_StationTelemetry_ActualCycleTime Variable.
        /// </summary>
        public const uint StationInstance_StationTelemetry_ActualCycleTime = 418;

        /// <summary>
        /// The identifier for the StationInstance_StationCommands_Execute_InputArguments Variable.
        /// </summary>
        public const uint StationInstance_StationCommands_Execute_InputArguments = 427;
    }
    #endregion

    #region Method Node Identifiers
    /// <summary>
    /// A class that declares constants for all Methods in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class MethodIds
    {
        /// <summary>
        /// The identifier for the StationCommandsType_Execute Method.
        /// </summary>
        public static readonly ExpandedNodeId StationCommandsType_Execute = new ExpandedNodeId(Station.Methods.StationCommandsType_Execute, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationCommandsType_Reset Method.
        /// </summary>
        public static readonly ExpandedNodeId StationCommandsType_Reset = new ExpandedNodeId(Station.Methods.StationCommandsType_Reset, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationCommandsType_OpenPressureReleaseValve Method.
        /// </summary>
        public static readonly ExpandedNodeId StationCommandsType_OpenPressureReleaseValve = new ExpandedNodeId(Station.Methods.StationCommandsType_OpenPressureReleaseValve, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType_StationCommands_Execute Method.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationCommands_Execute = new ExpandedNodeId(Station.Methods.StationType_StationCommands_Execute, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType_StationCommands_Reset Method.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationCommands_Reset = new ExpandedNodeId(Station.Methods.StationType_StationCommands_Reset, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType_StationCommands_OpenPressureReleaseValve Method.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationCommands_OpenPressureReleaseValve = new ExpandedNodeId(Station.Methods.StationType_StationCommands_OpenPressureReleaseValve, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationCommands_Execute Method.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationCommands_Execute = new ExpandedNodeId(Station.Methods.StationInstance_StationCommands_Execute, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationCommands_Reset Method.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationCommands_Reset = new ExpandedNodeId(Station.Methods.StationInstance_StationCommands_Reset, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationCommands_OpenPressureReleaseValve Method.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationCommands_OpenPressureReleaseValve = new ExpandedNodeId(Station.Methods.StationInstance_StationCommands_OpenPressureReleaseValve, Station.Namespaces.Station);
    }
    #endregion

    #region Object Node Identifiers
    /// <summary>
    /// A class that declares constants for all Objects in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectIds
    {
        /// <summary>
        /// The identifier for the StationType_StationProduct Object.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationProduct = new ExpandedNodeId(Station.Objects.StationType_StationProduct, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType_StationTelemetry Object.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationTelemetry = new ExpandedNodeId(Station.Objects.StationType_StationTelemetry, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType_StationCommands Object.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationCommands = new ExpandedNodeId(Station.Objects.StationType_StationCommands, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance Object.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance = new ExpandedNodeId(Station.Objects.StationInstance, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationProduct Object.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationProduct = new ExpandedNodeId(Station.Objects.StationInstance_StationProduct, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationTelemetry Object.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationTelemetry = new ExpandedNodeId(Station.Objects.StationInstance_StationTelemetry, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationCommands Object.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationCommands = new ExpandedNodeId(Station.Objects.StationInstance_StationCommands, Station.Namespaces.Station);
    }
    #endregion

    #region ObjectType Node Identifiers
    /// <summary>
    /// A class that declares constants for all ObjectTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectTypeIds
    {
        /// <summary>
        /// The identifier for the StationProductType ObjectType.
        /// </summary>
        public static readonly ExpandedNodeId StationProductType = new ExpandedNodeId(Station.ObjectTypes.StationProductType, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the TelemetryType ObjectType.
        /// </summary>
        public static readonly ExpandedNodeId TelemetryType = new ExpandedNodeId(Station.ObjectTypes.TelemetryType, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationCommandsType ObjectType.
        /// </summary>
        public static readonly ExpandedNodeId StationCommandsType = new ExpandedNodeId(Station.ObjectTypes.StationCommandsType, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType ObjectType.
        /// </summary>
        public static readonly ExpandedNodeId StationType = new ExpandedNodeId(Station.ObjectTypes.StationType, Station.Namespaces.Station);
    }
    #endregion

    #region Variable Node Identifiers
    /// <summary>
    /// A class that declares constants for all Variables in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class VariableIds
    {
        /// <summary>
        /// The identifier for the StationProductType_ProductSerialNumber Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationProductType_ProductSerialNumber = new ExpandedNodeId(Station.Variables.StationProductType_ProductSerialNumber, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationProductType_NumberOfManufacturedProducts Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationProductType_NumberOfManufacturedProducts = new ExpandedNodeId(Station.Variables.StationProductType_NumberOfManufacturedProducts, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationProductType_NumberOfDiscardedProducts Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationProductType_NumberOfDiscardedProducts = new ExpandedNodeId(Station.Variables.StationProductType_NumberOfDiscardedProducts, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the TelemetryType_OverallRunningTime Variable.
        /// </summary>
        public static readonly ExpandedNodeId TelemetryType_OverallRunningTime = new ExpandedNodeId(Station.Variables.TelemetryType_OverallRunningTime, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the TelemetryType_FaultyTime Variable.
        /// </summary>
        public static readonly ExpandedNodeId TelemetryType_FaultyTime = new ExpandedNodeId(Station.Variables.TelemetryType_FaultyTime, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the TelemetryType_Status Variable.
        /// </summary>
        public static readonly ExpandedNodeId TelemetryType_Status = new ExpandedNodeId(Station.Variables.TelemetryType_Status, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the TelemetryType_EnergyConsumption Variable.
        /// </summary>
        public static readonly ExpandedNodeId TelemetryType_EnergyConsumption = new ExpandedNodeId(Station.Variables.TelemetryType_EnergyConsumption, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the TelemetryType_Pressure Variable.
        /// </summary>
        public static readonly ExpandedNodeId TelemetryType_Pressure = new ExpandedNodeId(Station.Variables.TelemetryType_Pressure, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the TelemetryType_IdealCycleTime Variable.
        /// </summary>
        public static readonly ExpandedNodeId TelemetryType_IdealCycleTime = new ExpandedNodeId(Station.Variables.TelemetryType_IdealCycleTime, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the TelemetryType_ActualCycleTime Variable.
        /// </summary>
        public static readonly ExpandedNodeId TelemetryType_ActualCycleTime = new ExpandedNodeId(Station.Variables.TelemetryType_ActualCycleTime, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationCommandsType_Execute_InputArguments Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationCommandsType_Execute_InputArguments = new ExpandedNodeId(Station.Variables.StationCommandsType_Execute_InputArguments, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType_StationProduct_ProductSerialNumber Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationProduct_ProductSerialNumber = new ExpandedNodeId(Station.Variables.StationType_StationProduct_ProductSerialNumber, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType_StationProduct_NumberOfManufacturedProducts Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationProduct_NumberOfManufacturedProducts = new ExpandedNodeId(Station.Variables.StationType_StationProduct_NumberOfManufacturedProducts, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType_StationProduct_NumberOfDiscardedProducts Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationProduct_NumberOfDiscardedProducts = new ExpandedNodeId(Station.Variables.StationType_StationProduct_NumberOfDiscardedProducts, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType_StationTelemetry_OverallRunningTime Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationTelemetry_OverallRunningTime = new ExpandedNodeId(Station.Variables.StationType_StationTelemetry_OverallRunningTime, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType_StationTelemetry_FaultyTime Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationTelemetry_FaultyTime = new ExpandedNodeId(Station.Variables.StationType_StationTelemetry_FaultyTime, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType_StationTelemetry_Status Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationTelemetry_Status = new ExpandedNodeId(Station.Variables.StationType_StationTelemetry_Status, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType_StationTelemetry_EnergyConsumption Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationTelemetry_EnergyConsumption = new ExpandedNodeId(Station.Variables.StationType_StationTelemetry_EnergyConsumption, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType_StationTelemetry_Pressure Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationTelemetry_Pressure = new ExpandedNodeId(Station.Variables.StationType_StationTelemetry_Pressure, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType_StationTelemetry_IdealCycleTime Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationTelemetry_IdealCycleTime = new ExpandedNodeId(Station.Variables.StationType_StationTelemetry_IdealCycleTime, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType_StationTelemetry_ActualCycleTime Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationTelemetry_ActualCycleTime = new ExpandedNodeId(Station.Variables.StationType_StationTelemetry_ActualCycleTime, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationType_StationCommands_Execute_InputArguments Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationType_StationCommands_Execute_InputArguments = new ExpandedNodeId(Station.Variables.StationType_StationCommands_Execute_InputArguments, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationProduct_ProductSerialNumber Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationProduct_ProductSerialNumber = new ExpandedNodeId(Station.Variables.StationInstance_StationProduct_ProductSerialNumber, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationProduct_NumberOfManufacturedProducts Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationProduct_NumberOfManufacturedProducts = new ExpandedNodeId(Station.Variables.StationInstance_StationProduct_NumberOfManufacturedProducts, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationProduct_NumberOfDiscardedProducts Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationProduct_NumberOfDiscardedProducts = new ExpandedNodeId(Station.Variables.StationInstance_StationProduct_NumberOfDiscardedProducts, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationTelemetry_OverallRunningTime Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationTelemetry_OverallRunningTime = new ExpandedNodeId(Station.Variables.StationInstance_StationTelemetry_OverallRunningTime, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationTelemetry_FaultyTime Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationTelemetry_FaultyTime = new ExpandedNodeId(Station.Variables.StationInstance_StationTelemetry_FaultyTime, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationTelemetry_Status Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationTelemetry_Status = new ExpandedNodeId(Station.Variables.StationInstance_StationTelemetry_Status, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationTelemetry_EnergyConsumption Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationTelemetry_EnergyConsumption = new ExpandedNodeId(Station.Variables.StationInstance_StationTelemetry_EnergyConsumption, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationTelemetry_Pressure Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationTelemetry_Pressure = new ExpandedNodeId(Station.Variables.StationInstance_StationTelemetry_Pressure, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationTelemetry_IdealCycleTime Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationTelemetry_IdealCycleTime = new ExpandedNodeId(Station.Variables.StationInstance_StationTelemetry_IdealCycleTime, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationTelemetry_ActualCycleTime Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationTelemetry_ActualCycleTime = new ExpandedNodeId(Station.Variables.StationInstance_StationTelemetry_ActualCycleTime, Station.Namespaces.Station);

        /// <summary>
        /// The identifier for the StationInstance_StationCommands_Execute_InputArguments Variable.
        /// </summary>
        public static readonly ExpandedNodeId StationInstance_StationCommands_Execute_InputArguments = new ExpandedNodeId(Station.Variables.StationInstance_StationCommands_Execute_InputArguments, Station.Namespaces.Station);
    }
    #endregion

    #region BrowseName Declarations
    /// <summary>
    /// Declares all of the BrowseNames used in the Model Design.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class BrowseNames
    {
        /// <summary>
        /// The BrowseName for the ActualCycleTime component.
        /// </summary>
        public const string ActualCycleTime = "ActualCycleTime";

        /// <summary>
        /// The BrowseName for the EnergyConsumption component.
        /// </summary>
        public const string EnergyConsumption = "EnergyConsumption";

        /// <summary>
        /// The BrowseName for the Execute component.
        /// </summary>
        public const string Execute = "Execute";

        /// <summary>
        /// The BrowseName for the FaultyTime component.
        /// </summary>
        public const string FaultyTime = "FaultyTime";

        /// <summary>
        /// The BrowseName for the IdealCycleTime component.
        /// </summary>
        public const string IdealCycleTime = "IdealCycleTime";

        /// <summary>
        /// The BrowseName for the NumberOfDiscardedProducts component.
        /// </summary>
        public const string NumberOfDiscardedProducts = "NumberOfDiscardedProducts";

        /// <summary>
        /// The BrowseName for the NumberOfManufacturedProducts component.
        /// </summary>
        public const string NumberOfManufacturedProducts = "NumberOfManufacturedProducts";

        /// <summary>
        /// The BrowseName for the OpenPressureReleaseValve component.
        /// </summary>
        public const string OpenPressureReleaseValve = "OpenPressureReleaseValve";

        /// <summary>
        /// The BrowseName for the OverallRunningTime component.
        /// </summary>
        public const string OverallRunningTime = "OverallRunningTime";

        /// <summary>
        /// The BrowseName for the Pressure component.
        /// </summary>
        public const string Pressure = "Pressure";

        /// <summary>
        /// The BrowseName for the ProductSerialNumber component.
        /// </summary>
        public const string ProductSerialNumber = "ProductSerialNumber";

        /// <summary>
        /// The BrowseName for the Reset component.
        /// </summary>
        public const string Reset = "Reset";

        /// <summary>
        /// The BrowseName for the StationCommands component.
        /// </summary>
        public const string StationCommands = "StationCommands";

        /// <summary>
        /// The BrowseName for the StationCommandsType component.
        /// </summary>
        public const string StationCommandsType = "StationCommandsType";

        /// <summary>
        /// The BrowseName for the StationInstance component.
        /// </summary>
        public const string StationInstance = "StationInstance";

        /// <summary>
        /// The BrowseName for the StationProduct component.
        /// </summary>
        public const string StationProduct = "StationProduct";

        /// <summary>
        /// The BrowseName for the StationProductType component.
        /// </summary>
        public const string StationProductType = "StationProductType";

        /// <summary>
        /// The BrowseName for the StationTelemetry component.
        /// </summary>
        public const string StationTelemetry = "StationTelemetry";

        /// <summary>
        /// The BrowseName for the StationType component.
        /// </summary>
        public const string StationType = "StationType";

        /// <summary>
        /// The BrowseName for the Status component.
        /// </summary>
        public const string Status = "Status";

        /// <summary>
        /// The BrowseName for the TelemetryType component.
        /// </summary>
        public const string TelemetryType = "TelemetryType";
    }
    #endregion

    #region Namespace Declarations
    /// <summary>
    /// Defines constants for all namespaces referenced by the model design.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Namespaces
    {
        /// <summary>
        /// The URI for the OpcUa namespace (.NET code namespace is 'Opc.Ua').
        /// </summary>
        public const string OpcUa = "http://opcfoundation.org/UA/";

        /// <summary>
        /// The URI for the OpcUaXsd namespace (.NET code namespace is 'Opc.Ua').
        /// </summary>
        public const string OpcUaXsd = "http://opcfoundation.org/UA/2008/02/Types.xsd";

        /// <summary>
        /// The URI for the Station namespace (.NET code namespace is 'Station').
        /// </summary>
        public const string Station = "http://opcfoundation.org/UA/Station/";
    }
    #endregion
}