using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Text;

public class SocketClient : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private const string serverIp = "192.168.43.63"; // Replace with the server's IP address
    private const int port = 22334;

    void Start()
    {
        ConnectToServer();
    }

    void ConnectToServer()
    {
        try
        {
            client = new TcpClient(serverIp, port);
            stream = client.GetStream();

            // Receive the "Hello World" message
            byte[] data = new byte[1024];
            int bytes = stream.Read(data, 0, data.Length);
            string message = Encoding.UTF8.GetString(data, 0, bytes);
            Debug.Log("Message received: " + message);
        }
        catch (Exception e)
        {
            Debug.LogError("Error: " + e.Message);
        }
        finally
        {
            if (stream != null) stream.Close();
            if (client != null) client.Close();
        }
    }
}
