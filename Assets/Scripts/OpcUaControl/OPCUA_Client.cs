using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using UtilityFunctions.OPCUA;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class OPCUA_Client : MonoBehaviour
{
    private ApplicationConfiguration config;
    private Session session;
    public Dictionary<string, NodeData> allNodes = new Dictionary<string, NodeData>();
    public CancellationTokenSource cts;
    private Subscription subscription;
    private Dictionary<string, Tuple<string, string>> monitoredItems;
    public bool updateReady = true;
    public bool IsConnected => session != null && session.Connected;
    public OPCUAWriteContainer writeContainer = new OPCUAWriteContainer();

    public bool nodesAreReady = false;

    private async void Start()
    {
        await InitClient();
        // await ConnectToServer("opc.tcp://PC1M0484-1:4840/"); // Real OPCUA Server
        await ConnectToServer("opc.tcp://localhost"); // localhost
        getAllNodes(session);
        startSubscription();
        addMonitoredItems();
    }

    public void addToWriteContainer(string parentName, string childName)
    {
        Debug.Log("Adding: " + parentName + "/" + childName);
        NodeId nId = allNodes[parentName + "/" + childName].nodeId;
        
        // Retrieve the initial value associated with the provided parentName and childName from the allNodes dictionary
        Variant initalValue = allNodes[parentName + "/" + childName].dataValue.WrappedValue;
        // Add the node to the write container.
        writeContainer.addToCollection(nId, parentName, childName, initalValue);
        nodesAreReady = true;
    }

    public void removeFromWriteContainer(string parentName, string childName)
    {
        Debug.Log("Removing: " + parentName + "/" + childName + " from write container.");
        // Remove the node from the write container.
        writeContainer.removeFromColection(parentName, childName);
    }

    public async void writeToServer(string parentName, string childName, Variant value)
    {
        // Set the value of the node in the write container.
        writeContainer.container[parentName + "/" + childName].Value.WrappedValue = value;
        await WriteValues();
    }

    void Update()
    {
        // New value you want to write
        object newValue = 100;

        // CancellationTokenSource
        cts = new CancellationTokenSource();

        if (!updateReady)
        {
            // Check if all the data values of nodes in the 'allNodes' collection are not null.
            updateReady = allNodes.Values.All(item => item.dataValue.Value != null);
        };
        
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
        int maxRetries = 10; // Number of retry attempts
        int delayMilliseconds = 2000; // Delay between retries (2 seconds)
        int attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                Debug.Log($"Attempting to connect to OPC UA server... (Attempt {attempt + 1}/{maxRetries})");
                var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointUrl, useSecurity: false);

                var endpointConfiguration = EndpointConfiguration.Create(config);
                var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

                session = await Session.Create(config, endpoint, false, "", 60000, null, null);
                Debug.Log("Connected to OPC UA server" + session.Connected);

                // Exit the retry loop once connected
                return;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Connection attempt {attempt + 1} failed: {e.Message}");
                attempt++;

                if (attempt < maxRetries)
                {
                    Debug.Log($"Retrying connection in {delayMilliseconds / 1000} seconds...");
                    await Task.Delay(delayMilliseconds);
                }
                else
                {
                    Debug.LogError("Max connection attempts reached. Could not connect to OPC UA server.");
                    throw; // Re-throw the exception after max retries
                }
            }
        }
    }

    public void getAllNodes(Session session)
    {
        // Define a BrowseDescription for the top-level ObjectsFolder.
        var parentBrowseDescription = new BrowseDescription
        {
            NodeId = ObjectIds.ObjectsFolder,
            BrowseDirection = BrowseDirection.Forward,
            ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
            IncludeSubtypes = true,
            NodeClassMask = (uint)NodeClass.Object | (uint)NodeClass.Variable,
            ResultMask = (uint)BrowseResultMask.All
        };

        BrowseResultCollection parentResults;
        DiagnosticInfoCollection parentDiagnosticInfos;

        // Perform a browse operation to retrieve information about child nodes of the Object.
        session.Browse(null, null, 0, new BrowseDescriptionCollection { parentBrowseDescription }, out parentResults, out parentDiagnosticInfos);

        foreach (var parentResult in parentResults[0].References)
        {
            string currentParent = parentResult.DisplayName.Text;

            // Check if the current node is not the "Server" node.
            if (currentParent != "Server")
            {
                NodeId parentNodeID = ExpandedNodeId.ToNodeId(parentResult.NodeId, session.NamespaceUris);
                var childBrowseDescription = new BrowseDescription
                {
                    NodeId = parentNodeID,
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)NodeClass.Object | (uint)NodeClass.Variable,
                    ResultMask = (uint)BrowseResultMask.All
                };

                BrowseResultCollection childResults;
                DiagnosticInfoCollection chidlDiagnosticInfos;

                // Perform a browse operation to retrieve information about child nodes of the current parent node.
                session.Browse(null, null, 0, new BrowseDescriptionCollection { childBrowseDescription }, out childResults, out chidlDiagnosticInfos);

                foreach (var childResult in childResults[0].References)
                {
                    string currentChild = childResult.DisplayName.Text;

                    // Add information about the current child node to a collection.
                    allNodes.Add(currentParent + "/" + currentChild, new NodeData(ExpandedNodeId.ToNodeId(childResult.NodeId, session.NamespaceUris)));
                };
            }
        }
    }

    async public Task WriteValues()
    {
        if (session == null || session.Connected == false)
        {
            Debug.LogError("Not connected to a server");
        }
        else
        {
            try
            {
                WriteResponse response = await session.WriteAsync(null, writeContainer.nodesToWrite, cts.Token);
            }
            catch (Exception e)
            {
                Debug.LogError("Error writing data: " + e.Message);
            }
        }
    }


    void startSubscription()
    {
        if (subscription == null)
        {
            subscription = new Subscription(session.DefaultSubscription);
            subscription.PublishingEnabled = true;
            subscription.PublishingInterval = 100;
            session.AddSubscription(subscription);
            subscription.Create();
            Debug.Log("Subscrtiption initialized...");
        }
        else
        {
            Debug.Log("Subscription existing...");
        }
    }

    void addMonitoredItems()
    {
        List<MonitoredItem> monitoredItems = new List<MonitoredItem>();
        Debug.Log("Adding Monitored Items...");

        // Iterate through all the nodes in the 'allNodes' collection.
        foreach (KeyValuePair<string, NodeData> node in allNodes)
        {
            // Create a new MonitoredItem for each node.
            MonitoredItem monitoredItem = new MonitoredItem(subscription.DefaultItem);

            // Set the StartNodeId for the MonitoredItem to the node's NodeId.
            monitoredItem.StartNodeId = node.Value.nodeId;

            // Set the AttributeId to Attributes.Value, indicating that it's monitoring the value of the node.
            monitoredItem.AttributeId = Attributes.Value;

            // Set the MonitoringMode to Reporting, indicating that updates will be reported.
            monitoredItem.MonitoringMode = MonitoringMode.Reporting;

            // Set the SamplingInterval to 10 milliseconds (the frequency of data sampling).
            monitoredItem.SamplingInterval = 10;

            // Set the QueueSize to 1, indicating that only the most recent value is stored.
            monitoredItem.QueueSize = 1;

            // Set DiscardOldest to true, meaning that if the queue is full, the oldest value is discarded.
            monitoredItem.DiscardOldest = true;

            // Set the DisplayName of the monitored item, typically used to identify it.
            monitoredItem.DisplayName = node.Key;

            // Attach the 'updateNodeCallback' method as a notification handler.
            monitoredItem.Notification += new MonitoredItemNotificationEventHandler(updateNodeCallback);

            // Add the MonitoredItem to the list of monitored items.
            monitoredItems.Add(monitoredItem);
        }

        // Add all monitored items to the OPC UA subscription.
        subscription.AddItems(monitoredItems);

        // Apply changes to the subscription to finalize the configuration.
        subscription.ApplyChanges();

        Debug.Log("Monitored Items added!");
    }

    void updateNodeCallback(MonitoredItem item, MonitoredItemNotificationEventArgs e)
    {
        // // Dequeue the latest value update from the MonitoredItem.
        // var value = item.DequeueValues()[0];

        // // Update the data value associated with the MonitoredItem's corresponding node.
        // allNodes[item.DisplayName].dataValue = value;
        var notifications = item.DequeueValues();
        try
        {
            // Check if the notifications are not null.
            foreach (var val in notifications)
            {
                // Debug.Log($"updateNodeCallback fired for {item.DisplayName} with value: {val.Value}");
                allNodes[item.DisplayName].dataValue = val;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error updating node: " + ex.Message);
        }

    }
}
