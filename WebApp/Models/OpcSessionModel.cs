using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Models
{
    /// <summary>
    /// A view model for the Browser view.
    /// </summary>
    public class OpcSessionModel
    {
        /// <summary>
        /// The ID of the active session.
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// The OPC UA server endpoint Url the session is connected to.
        /// </summary>
        public string EndpointUrl { get; set; }

        /// <summary>
        /// The OPC UA Endpoint endpoint Id.
        /// </summary>
        public string EndpointId { get; set; }

        /// <summary>
        /// Header text of the error view.
        /// </summary>
        public string ErrorHeader { get; set; }

        /// <summary>
        /// Error text for the error view.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Url to come back when disconnect is invoked.
        /// </summary>
        public string BackUrl { get; set; }

        /// <summary>
        /// List of Endpoint models.
        /// </summary>
        public List<Endpoint> endpointList { get; set; }

        /// <summary>
        /// List of Supervisor models.
        /// </summary>
        public List<Supervisor> supervisorList { get; set; }
    }

    /// <summary>
    /// A view model for the Endpoint view.
    /// </summary>
    public class Endpoint
    {
        /// <summary>
        /// The OPC UA Endpoint endpoint Id.
        /// </summary>
        public string EndpointId { get; set; }

        /// <summary>
        /// The OPC UA Endpoint endpoint Url.
        /// </summary>
        public string EndpointUrl { get; set; }

        /// <summary>
        /// The OPC UA Endpoint Security Mode.
        /// </summary>
        public string SecurityMode { get; set; }

        /// <summary>
        ///The OPC UA Endpoint Security Policy.
        /// </summary>
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// The OPC UA Endpoint Security Level.
        /// </summary>
        public int? SecurityLevel { get; set; }

        /// <summary>
        /// The OPC UA Application Id.
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// The OPC UA ProductUri
        /// </summary>
        public string ProductUri { get; set; }

        /// <summary>
        /// The OPC UA endpoint activation status
        /// </summary>
        public bool? Activated { get; set; }
    }

    /// <summary>
    /// A view model for the Supervisor view.
    /// </summary>
    public class Supervisor
    {
        /// <summary>
        /// Supervisor models.
        /// </summary>
        public SupervisorApiModel supervisorModel { get; set; }

        /// <summary>
        /// Supervisor has application children.
        /// </summary>
        public bool HasApplication { get; set; }
    }
}