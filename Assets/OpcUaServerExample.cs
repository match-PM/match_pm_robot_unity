using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Server;
using System.Collections.Generic;

public class UnityOpcUaServer : MonoBehaviour
{
    private CancellationTokenSource cts;
    private Task serverTask;
    private StandardServer serverInstance;
    private string certPath;

    void Start()
    {
        certPath = System.IO.Path.Combine(Application.persistentDataPath, "pki"); // MAIN THREAD!
        Debug.Log(Application.persistentDataPath);

        cts = new CancellationTokenSource();
        serverTask = Task.Run(() =>
        {
            try
            {
                var server = new MyServer();
                serverInstance = server;

                var config = CreateServerConfig(certPath);

                server.Start(config);

                Debug.LogWarning("OPC UA Server started (no security) at opc.tcp://localhost:4840/UnityOpcUaServer/");

                while (!cts.IsCancellationRequested)
                {
                    Thread.Sleep(500);
                }

                server.Stop();
            }
            catch (System.Exception ex)
            {
                Debug.LogError("OPC UA Server exception: " + ex);
            }
        });
    }

    void OnApplicationQuit()
    {
        cts?.Cancel();
        serverInstance?.Stop();
    }

    private ApplicationConfiguration CreateServerConfig(string certPath)
    {
        string subjectName = "CN=UnityOpcUaServer";
        string certFile = System.IO.Path.Combine(certPath, "unityopcua.pfx");

        var config = new ApplicationConfiguration()
        {
            ApplicationName = "UnityOpcUaServer",
            ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:UnityOpcUaServer",
            ApplicationType = ApplicationType.Server,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = certPath,
                    SubjectName = subjectName,
                    // CertificateFile = certFile
                },
                AutoAcceptUntrustedCertificates = true,
                AddAppCertToTrustedStore = true
            },
            ServerConfiguration = new ServerConfiguration
            {
                BaseAddresses = { "opc.tcp://localhost:4840/UnityOpcUaServer/" },
                SecurityPolicies = new ServerSecurityPolicyCollection
                {
                    new ServerSecurityPolicy
                    {
                        SecurityMode = MessageSecurityMode.None,
                        SecurityPolicyUri = SecurityPolicies.None
                    }
                }
            },
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            DisableHiResClock = true
        };

        config.Validate(ApplicationType.Server).Wait();

        return config;
    }


    // --- OPC UA Server Implementation ---
    public class MyServer : StandardServer
    {
        protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        {
            return new MasterNodeManager(server, configuration, null, new[] { new SimpleNodeManager(server, configuration) });
        }
    }

    public class SimpleNodeManager : CustomNodeManager2
    {
        public SimpleNodeManager(IServerInternal server, ApplicationConfiguration config)
            : base(server, config, "http://yourorganisation.com/UA/UnityOpcUaServer")
        {
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            var folder = CreateFolder(null, "UnityData", "UnityData", externalReferences);

            var testVariable = CreateVariable(
                folder,
                "TestValue",
                "TestValue",
                DataTypeIds.Double,
                ValueRanks.Scalar,
                42.0
            );
        }

        protected FolderState CreateFolder(NodeState parent, string path, string name, IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            var folder = new FolderState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypeIds.Organizes,
                TypeDefinitionId = ObjectTypeIds.FolderType,
                NodeId = new NodeId(name, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = name,
            };

            if (parent == null)
            {
                if (externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out IList<IReference> refs))
                {
                    folder.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                    refs.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, folder.NodeId));
                }
            }
            else
            {
                parent.AddChild(folder);
            }

            AddPredefinedNode(SystemContext, folder);
            return folder;
        }

        protected BaseDataVariableState CreateVariable(
            NodeState parent,
            string path,
            string name,
            NodeId dataType,
            int valueRank,
            object initialValue)
        {
            var variable = new BaseDataVariableState(parent)
            {
                SymbolicName = name,
                ReferenceTypeId = ReferenceTypeIds.Organizes,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                NodeId = new NodeId(name, NamespaceIndex),
                BrowseName = new QualifiedName(name, NamespaceIndex),
                DisplayName = name,
                DataType = dataType,
                ValueRank = valueRank,
                AccessLevel = AccessLevels.CurrentReadOrWrite,
                UserAccessLevel = AccessLevels.CurrentReadOrWrite,
                Value = initialValue,
                StatusCode = StatusCodes.Good,
                Timestamp = System.DateTime.UtcNow
            };

            parent.AddChild(variable);
            AddPredefinedNode(SystemContext, variable);

            return variable;
        }
    }
}
