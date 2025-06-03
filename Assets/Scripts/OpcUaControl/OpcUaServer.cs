using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using System.Security.Cryptography.X509Certificates;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using System;

public class OpcUaServer : MonoBehaviour
{
    private ApplicationInstance app;

    async void Start()
    {
        Debug.Log("[OPC UA] Server Start() called.");
        var pfxPath = Path.Combine(Application.streamingAssetsPath, "Certificates/Own/MinimalUnityOpcUa.pfx");
        Debug.Log($"[OPC UA] PFX Path: {pfxPath}");
        var serverCert = new X509Certificate2(
            pfxPath,
            "", // your PFX password, if any
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet
        );

        Debug.Log($"Loaded Cert: {serverCert.Subject}");
        Debug.Log($"Has Private Key: {serverCert.HasPrivateKey}");

        var config = new ApplicationConfiguration()
        {
            ApplicationName = "MinimalUnityOpcUa",
            ApplicationUri = "urn:MinimalUnityOpcUa", // Just a simple URI for now
            ApplicationType = ApplicationType.Server,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    Certificate = serverCert,
                    Thumbprint = serverCert.Thumbprint
                },
                AutoAcceptUntrustedCertificates = true
            },
            ServerConfiguration = new ServerConfiguration
            {
                BaseAddresses = new Opc.Ua.StringCollection { "opc.tcp://localhost:4840" }
            }
        };

        app = new ApplicationInstance
        {
            ApplicationName = "MinimalUnityOpcUa",
            ApplicationType = ApplicationType.Server,
            ApplicationConfiguration = config
        };

        Debug.Log($"Certificate in config: {config.SecurityConfiguration.ApplicationCertificate.Certificate}");
        Debug.Log($"Thumbprint: {config.SecurityConfiguration.ApplicationCertificate.Thumbprint}");
        Debug.Log($"Subject: {config.SecurityConfiguration.ApplicationCertificate.Certificate?.Subject}");

        await app.Start(new StandardServer());
        Debug.Log("[OPC UA] Minimal server started on opc.tcp://localhost:4840");
    }

    void OnApplicationQuit()
    {
        app?.Stop();
        Debug.Log("[OPC UA] Minimal server stopped");
    }
}
