using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Opc.Ua;
using Opc.Ua.NodeSet2Reader;
using Opc.Ua.Server;

public class OPCUA_Server : MonoBehaviour
{
    private ServerBase server;
    private ApplicationConfiguration config;
    
    void Start()
    {
        // Setup the server configuration.
        config = new ApplicationConfiguration();
        config.ApplicationName = "Unity OPC UA Server";
        config.ApplicationType = ApplicationType.Server;
        config.SecurityConfiguration = new SecurityConfiguration
        {
            // Set your security settings here.
            // In a production server, you should use secure settings!
            AutoAcceptUntrustedCertificates = true,
            RejectSHA1SignedCertificates = false,
            AddAppCertToTrustedStore = true
        };

        // Initialize and start the server.
        server = new ServerBase();
        server.Start(config);

        // Load nodes from the nodeset XML file.
        LoadNodeset("nodeset_pm_server.xml");
    }

    private void LoadNodeset(string filePath)
    {
        // Read the nodeset XML file.
        NodeStateCollection nodeset = new NodeStateCollection();
        NodeSet2Reader reader = new NodeSet2Reader(nodeset, server.NodeManager);

        using (Stream stream = File.OpenRead(filePath))
        {
            reader.Read(stream);
        }

        // Add the nodes to the server.
        foreach (NodeState node in nodeset)
        {
            server.NodeManager.AddPredefinedNode(node);
        }
    }

    // Remember to stop the server when the application quits.
    void OnApplicationQuit()
    {
        server.Stop();
    }
}
