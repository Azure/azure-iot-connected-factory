
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Opc.Ua;


namespace Microsoft.Azure.IoTSuite.Connectedfactory.WebApp.Controllers
{
    [DataContract]
    public partial class NodeLookup
    {
        public NodeLookup()
        {
        }

        [DataMember]
        public Uri EndPointURL;

        [DataMember]
        public NodeId NodeID;
    }

    [CollectionDataContract]
    public partial class PublishedNodesCollection : List<NodeLookup>
    {
        public PublishedNodesCollection()
        {
        }
    }
}