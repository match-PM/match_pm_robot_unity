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
    public bool updateReady = false;
    public bool IsConnected => session != null && session.Connected;
    public OPCUAWriteContainer writeContainer = new OPCUAWriteContainer();

    public bool nodesAreReady = false;
    private bool isWritePending = false;
    private bool writeValuesDirty = false;
    private string serverUrl = "opc.tcp://localhost";
    private bool isReconnecting = false;

    private async void Start()
    {
        cts = new CancellationTokenSource();
        await InitClient();
        // await ConnectToServer("opc.tcp://PC1M0484-1:4840/"); // Real OPCUA Server
        await ConnectToServer(serverUrl); // localhost
        getAllNodes(session);
        startSubscription();
        addMonitoredItems();
        // Signal that OPC UA is fully initialised — controllers wait on this flag
        nodesAreReady = true;
    }

    public void addToWriteContainer(string parentName, string childName)
    {
        Debug.Log("Adding: " + parentName + "/" + childName);
        NodeId nId = allNodes[parentName + "/" + childName].nodeId;
        
        // Retrieve the initial value associated with the provided parentName and childName from the allNodes dictionary
        Variant initalValue = allNodes[parentName + "/" + childName].dataValue.WrappedValue;
        // Add the node to the write container.
        writeContainer.addToCollection(nId, parentName, childName, initalValue);
    }

    public void removeFromWriteContainer(string parentName, string childName)
    {
        Debug.Log("Removing: " + parentName + "/" + childName + " from write container.");
        // Remove the node from the write container.
        writeContainer.removeFromColection(parentName, childName);
    }

    /// <summary>
    /// Set a value to be written. The actual write is coalesced into one call per frame.
    /// </summary>
    public void writeToServer(string parentName, string childName, Variant value)
    {
        // Just update the value in the write container — no network call yet
        writeContainer.container[parentName + "/" + childName].Value.WrappedValue = value;
        writeValuesDirty = true;
    }

    void Update()
    {
        // Only check once nodes are loaded; gate prevents false-positive on empty allNodes
        if (nodesAreReady && !updateReady)
        {
            bool allReady = true;
            foreach (var item in allNodes.Values)
            {
                if (item.dataValue.Value == null)
                {
                    allReady = false;
                    break;
                }
            }
            updateReady = allReady;
        }

        // Coalesce all write requests into a single OPC UA write per frame
        if (updateReady && writeValuesDirty && !isWritePending && IsConnected)
        {
            writeValuesDirty = false;
            CoalescedWrite();
        }
    }

    private async void CoalescedWrite()
    {
        isWritePending = true;
        try
        {
            if (session != null && session.Connected && writeContainer.nodesToWrite != null)
            {
                await session.WriteAsync(null, writeContainer.nodesToWrite, cts.Token);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error writing data: " + e.Message);
        }
        finally
        {
            isWritePending = false;
        }
    }

    void OnDestroy()
    {
        nodesAreReady = false;
        updateReady = false;
        cts?.Cancel();
        cts?.Dispose();
        cts = null;

        try
        {
            if (subscription != null)
            {
                subscription.Delete(true);
                subscription = null;
            }
            if (session != null && session.Connected)
            {
                session.Close();
                session = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error during cleanup: " + e.Message);
        }
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
                session.KeepAlive += OnSessionKeepAlive;
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

    void startSubscription()
    {
        if (subscription == null)
        {
            subscription = new Subscription(session.DefaultSubscription);
            subscription.PublishingEnabled = true;
            subscription.PublishingInterval = 100;
            session.AddSubscription(subscription);
            subscription.Create();
            Debug.Log("Subscription initialized...");
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

        foreach (KeyValuePair<string, NodeData> node in allNodes)
        {
            MonitoredItem monitoredItem = new MonitoredItem(subscription.DefaultItem);
            monitoredItem.StartNodeId = node.Value.nodeId;
            monitoredItem.AttributeId = Attributes.Value;
            monitoredItem.MonitoringMode = MonitoringMode.Reporting;
            monitoredItem.SamplingInterval = 10;
            monitoredItem.QueueSize = 1;
            monitoredItem.DiscardOldest = true;
            monitoredItem.DisplayName = node.Key;
            monitoredItem.Notification += new MonitoredItemNotificationEventHandler(updateNodeCallback);
            monitoredItems.Add(monitoredItem);
        }

        subscription.AddItems(monitoredItems);
        subscription.ApplyChanges();
        Debug.Log("Monitored Items added!");
    }

    void updateNodeCallback(MonitoredItem item, MonitoredItemNotificationEventArgs e)
    {
        var notifications = item.DequeueValues();
        try
        {
            foreach (var val in notifications)
            {
                allNodes[item.DisplayName].dataValue = val;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error updating node: " + ex.Message);
        }
    }

    /// <summary>
    /// Called by OPC UA SDK on the background thread when the session keep-alive fires.
    /// If the server is unreachable the session enters a reconnecting state.
    /// </summary>
    private void OnSessionKeepAlive(ISession sender, KeepAliveEventArgs e)
    {
        if (e.CurrentState != ServerState.Running)
        {
            if (!isReconnecting)
            {
                isReconnecting = true;
                Debug.LogWarning("OPC UA session lost — attempting reconnect…");
                _ = TryReconnectAsync();
            }
        }
    }

    private async Task TryReconnectAsync()
    {
        int attempt = 0;
        while (attempt < 20)
        {
            attempt++;
            try
            {
                Debug.Log($"Reconnect attempt {attempt}…");
                session.Reconnect();
                Debug.Log("Reconnected to OPC UA server.");
                isReconnecting = false;
                return;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Reconnect attempt {attempt} failed: {ex.Message}");
                await Task.Delay(2000);
            }
        }
        Debug.LogError("Could not reconnect after 20 attempts.");
        isReconnecting = false;
    }
}
