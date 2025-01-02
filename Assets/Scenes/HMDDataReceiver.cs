using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class HMDDataReceiver : MonoBehaviour
{
    private TcpListener _listener;
    private TcpClient _client;
    private NetworkStream _stream;
    private bool _isConnected = false;

    [Tooltip("Server IP Address (e.g., localhost or 127.0.0.1)")]
    public string serverIp = "127.0.0.1";

    [Tooltip("Server Port")]
    public int serverPort = 9001;

    [Tooltip("Sphere representing the CoP position")]
    public Transform sphere;

    void Start()
    {
        StartServer();
    }

    void Update()
    {
        if (_isConnected && _stream != null && _stream.DataAvailable)
        {
            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);

                string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Log the raw data for debugging
                Debug.Log($"Received JSON: {receivedData}");

                ParseAndUpdateVisualization(receivedData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error receiving data: {ex.Message}");
            }
        }
    }

    private void OnApplicationQuit()
    {
        DisconnectFromServer();
    }

    private void StartServer()
    {
        try
        {
            // Start the TcpListener
            _listener = new TcpListener(IPAddress.Parse(serverIp), serverPort);
            _listener.Start();
            Debug.Log("Waiting for client to connect...");

            // Start listening asynchronously
            _listener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), _listener);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error starting server: {ex.Message}");
        }
    }

    private void OnClientConnect(IAsyncResult result)
    {
        try
        {
            _client = _listener.EndAcceptTcpClient(result);
            _stream = _client.GetStream();
            _isConnected = true;
            Debug.Log("Client connected to HMD.");

            // Continue listening for more clients in background
            _listener.BeginAcceptTcpClient(new AsyncCallback(OnClientConnect), _listener);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error accepting client connection: {ex.Message}");
        }
    }

    private void DisconnectFromServer()
    {
        if (_stream != null)
        {
            _stream.Close();
            _stream = null;
        }

        if (_client != null)
        {
            _client.Close();
            _client = null;
        }

        _isConnected = false;
        Debug.Log("Disconnected from the server.");
    }

    private void ParseAndUpdateVisualization(string jsonData)
    {
        try
        {
            if (sphere == null)
            {
                Debug.LogError("Sphere is not assigned!");
                return;
            }

            Debug.Log($"Raw JSON Data Received: {jsonData}");

            var data = JsonUtility.FromJson<CoPData>(jsonData);
            Debug.Log($"Parsed Data: CoPX={data.CoPX}, CoPY={data.CoPY}, Pressures=[{string.Join(", ", data.Pressures ?? Array.Empty<double>())}]");

            // Just log the data to debug
            Debug.Log($"Received CoPX: {data.CoPX}, CoPY: {data.CoPY}");

            // Ensure that the sphere has a renderer before applying the material
            var renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Turn the sphere red
                renderer.material.color = Color.red;
            }
            else
            {
                Debug.LogError("Sphere Renderer is missing!");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error parsing data: {ex.Message}");
        }
    }


    // Data structure matching JSON format
    [Serializable]
    public class CoPData
    {
        public double CoPX;
        public double CoPY;
        public double[] Pressures;
    }
}
