using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

public class OPCUA_test : MonoBehaviour
{
    private ApplicationConfiguration config;
    private Session session;

    public DataValue coaxLightMessage;

    async void Awake()
    {
        await InitClient();
        await ConnectToServer("opc.tcp://PC1M0484-1:4840/");
    }

    async void Update()
    {
        // NodeId of the data point you want to read
        NodeId nodeId = new NodeId(50364, 0);

        NodeId coaxLightNodeId = new NodeId(50364, 0);

        // New value you want to write
        object newValue = 100;
        
        // CancellationTokenSource
        CancellationTokenSource cts = new CancellationTokenSource();

        // Call WriteData function
        //StatusCode status = await WriteData(nodeId, newValue, cts.Token);

        DataValue dataValue = await ReadData(nodeId, cts.Token);
        
        coaxLightMessage = await ReadData(coaxLightNodeId, cts.Token);

        //Debug.Log("Value read from server: "+ coaxLightMessage.Value);

        // Handle the returned status
        // if (StatusCode.IsGood(status))
        // {
        //     Debug.Log("Successfully wrote value to server");
        // }
        // else
        // {
        //     Debug.Log("Failed to write value to server");
        // }
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
}
