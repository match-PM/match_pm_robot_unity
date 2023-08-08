using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

public class OPCUA_Client : MonoBehaviour
{
    private ApplicationConfiguration config;
    private Session session;

    public DataValue coaxLightMessage;
    public DataValue ringLightMessage;

    public class ChildNode
    {
        public NodeId nodeId {get; set;}
        public DataValue result = new DataValue();
    }
    
    public class NodeData
    {
        public NodeId nodeId {get; set;}
        public Dictionary<string, ChildNode> childrenNodes {get; set;} 
    }

    public Dictionary<string, NodeData> allNodes;

    public List<(string, string)> nodeNames = new List<(string, string)>();

    public ReadValueIdCollection nodesToRead = new ReadValueIdCollection();

    async void Awake()
    {
        await InitClient();
        // await ConnectToServer("opc.tcp://PC1M0484-1:4840/");
        await ConnectToServer("opc.tcp://pmlab-101:4840");
        allNodes = getAllNodes(session);
    }

    async void Update()
    {
        // New value you want to write
        object newValue = 100;
        
        // CancellationTokenSource
        CancellationTokenSource cts = new CancellationTokenSource();

        await updateNodeValues(cts);
        Debug.Log(allNodes["NestNozzle"].childrenNodes["State"].nodeId);
    }


    async void OnApplicationQuit()
    {
        // Call a method to disconnect from the OPC UA server and release resources.
        try
        {
            // Check if session is connected
            if (session != null && session.Connected)
            {
                // Close the session
                await session.CloseAsync();
                Debug.Log("Disconnected from OPC UA server");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error disconnecting from the server: " + e.Message);
        }
    }

    private async Task DisconnectFromServer()
    {
        try
        {
            // Check if session is connected
            if (session != null && session.Connected)
            {
                // Close the session
                await session.CloseAsync();
                Debug.Log("Disconnected from OPC UA server");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error disconnecting from the server: " + e.Message);
        }
    }

    async Task InitClient()
    {
        // build the application configuration
        config = new ApplicationConfiguration()
        {
            ApplicationName = "Unity OPC UA Client",
            ApplicationType = ApplicationType.Client,
            ApplicationUri = Utils.Format("urn:{0}:UnityOPCUAClient", System.Net.Dns.GetHostName()),
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier(),
                TrustedPeerCertificates = new CertificateTrustList(),
                RejectedCertificateStore = new CertificateTrustList(),
                AutoAcceptUntrustedCertificates = true
            },
            TransportConfigurations = new TransportConfigurationCollection(),
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
            DisableHiResClock = true
        };

        await config.Validate(ApplicationType.Client);
    }

    async Task ConnectToServer(string endpointUrl)
    {
        var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointUrl, useSecurity: false);

        var endpointConfiguration = EndpointConfiguration.Create(config);
        var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

        session = await Session.Create(config, endpoint, false, "", 60000, null, null);
        Debug.Log("Connected to OPC UA server");
    }

    async Task<StatusCode> WriteData(NodeId nodeId, object value, CancellationToken ct)
    {
        // Check if session is connected
        if (session == null || session.Connected == false)
        {
            Debug.LogError("Not connected to a server");
            return StatusCodes.BadNotConnected;
        }

        // Write the node
        try
        {
            WriteValue nodeToWrite = new WriteValue();
            nodeToWrite.NodeId = nodeId;
            nodeToWrite.AttributeId = Attributes.Value;
            nodeToWrite.Value = new DataValue(new Variant(value));
            WriteResponse response = await session.WriteAsync(null, new WriteValueCollection { nodeToWrite }, ct);
            
            // Return the status
            return response.Results[0];
        }
        catch (Exception e)
        {
            Debug.LogError("Error writing data: " + e.Message);
            return StatusCodes.BadUnexpectedError;
        }
    }

    async Task<DataValue> ReadData(NodeId nodeId, CancellationToken ct)
    {
        // Check if session is connected
        if (session == null || session.Connected == false)
        {
            Debug.LogError("Not connected to a server");
            return null;
        }
        
        // Read the node
        try
        {
            ReadValueId nodeToRead = new ReadValueId();
            nodeToRead.NodeId = nodeId;
            nodeToRead.AttributeId = Attributes.Value;
            ReadResponse response = await session.ReadAsync(null, 0, TimestampsToReturn.Both, new ReadValueIdCollection { nodeToRead }, ct);
            
            // Return the value
            return response.Results[0];
        }
        catch (Exception e)
        {
            Debug.LogError("Error reading data: " + e.Message);
            return null;
        }
    }

    public Dictionary<string, NodeData> getAllNodes(Session session)
    {  
        Dictionary<string, NodeData> nodeDataDict = new Dictionary<string, NodeData>();
        var browseDescription = new BrowseDescription
        {
            NodeId = ObjectIds.ObjectsFolder,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
            IncludeSubtypes = true,
            NodeClassMask = (uint) NodeClass.Object | (uint) NodeClass.Variable,
            ResultMask = (uint) BrowseResultMask.All
        };

        BrowseResultCollection results;
        DiagnosticInfoCollection diagnosticInfos;
        session.Browse(null, null, 0, new BrowseDescriptionCollection {browseDescription}, out results, out diagnosticInfos);
        foreach (var result in results[0].References)
        {
            NodeData parentNodeData = new NodeData();
            NodeId parentNodeID =  ExpandedNodeId.ToNodeId(result.NodeId, session.NamespaceUris);
            parentNodeData.nodeId = parentNodeID;


            Dictionary<string, ChildNode> childrenNodesDict = new Dictionary<string, ChildNode>();
            var childBrowseDescription = new BrowseDescription
            {
                NodeId = parentNodeID,
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                IncludeSubtypes = true,
                NodeClassMask = (uint) NodeClass.Object | (uint) NodeClass.Variable,
                ResultMask = (uint) BrowseResultMask.All
            };
            BrowseResultCollection childResults;
            DiagnosticInfoCollection chidlDiagnosticInfos;
            session.Browse(null, null, 0, new BrowseDescriptionCollection {childBrowseDescription}, out childResults, out chidlDiagnosticInfos);

            foreach (var childResult in childResults[0].References)
            {
                ChildNode childNode = new ChildNode() {nodeId = ExpandedNodeId.ToNodeId(childResult.NodeId, session.NamespaceUris)};  
                childrenNodesDict.Add(childResult.DisplayName.Text, childNode);
                nodesToRead.Add(new ReadValueId{NodeId = ExpandedNodeId.ToNodeId(childResult.NodeId, session.NamespaceUris), AttributeId = Attributes.Value});
            };
            parentNodeData.childrenNodes = childrenNodesDict;
            nodeDataDict.Add(result.DisplayName.Text, parentNodeData);
            
        }
        return nodeDataDict;
    }

    async Task updateNodeValues(CancellationTokenSource cts)
    {
        ReadResponse response = await session.ReadAsync(null, 0, TimestampsToReturn.Both, nodesToRead, cts.Token);
        int i = 0;

        foreach(KeyValuePair<string, NodeData> parent in allNodes)
        {
            foreach(KeyValuePair<string, ChildNode> child in allNodes[parent.Key].childrenNodes)
            {
                allNodes[parent.Key].childrenNodes[child.Key].result = response.Results[i];
                i+=1;
            }
        }
    }
}