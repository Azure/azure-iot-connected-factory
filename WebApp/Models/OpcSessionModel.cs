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
        /// List of prepopulated OPC UA server endpoints.
        /// </summary>
        public SelectList PrepopulatedEndpoints { get; set; }

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
    }
}