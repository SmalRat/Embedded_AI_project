using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Text;

public class Listener : MonoBehaviour
{
    private UdpClient udpClient;
    private const string serverIp = "192.168.43.63"; // Replace with the server's IP address
    private const int port = 22334;

    void Start()
    {
        // Initialize the UdpClient to listen on the specified port
        udpClient = new UdpClient(port);

        // Start listening for messages
        StartListening();
    }

    void StartListening()
    {
        try
        {
            // Begin async receive to listen for incoming data
            udpClient.BeginReceive(new AsyncCallback(OnReceive), null);
        }
        catch (Exception e)
        {
            Debug.LogError("Error: " + e.Message);
        }
    }

    void OnReceive(IAsyncResult result)
    {
        try
        {
            // Get the remote endpoint and the received data
            System.Net.IPEndPoint remoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, port);
            byte[] data = udpClient.EndReceive(result, ref remoteEndPoint);

            // Convert the data to a string
            string message = Encoding.UTF8.GetString(data);
            Debug.Log("Message received: " + message);

            // Continue listening for the next message
            udpClient.BeginReceive(new AsyncCallback(OnReceive), null);
        }
        catch (Exception e)
        {
            Debug.LogError("Error: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        if (udpClient != null)
        {
            udpClient.Close();
        }
    }
}
