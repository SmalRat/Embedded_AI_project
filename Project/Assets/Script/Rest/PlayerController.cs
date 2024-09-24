using System;
using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    private UdpClient udpClient;
    private const int port = 22334;

    private Queue<string> commandQueue = new Queue<string>(); // Queue to hold received commands
    private object queueLock = new object(); // Lock object for thread-safe queue access

    void Start()
    {
        ConnectToServer();
    }

    void ConnectToServer()
    {
        try
        {
            udpClient = new UdpClient(port);
            Debug.Log("UDP Client initialized and listening on port " + port);

            // Start receiving data continuously
            ReceiveData();
        }
        catch (Exception e)
        {
            Debug.LogError("Connection error: " + e.Message);
        }
    }

    void ReceiveData()
    {
        try
        {
            udpClient.BeginReceive(new AsyncCallback(OnDataReceived), null);
        }
        catch (Exception e)
        {
            Debug.LogError("Receive error: " + e.Message);
        }
    }

    void OnDataReceived(IAsyncResult result)
    {
        try
        {
            // Get the data
            var remoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, port);
            byte[] receivedData = udpClient.EndReceive(result, ref remoteEndPoint);

            string message = Encoding.UTF8.GetString(receivedData);
            Debug.Log("Message received: " + message);

            // Queue the received command to be processed in the main thread
            lock (queueLock)
            {
                commandQueue.Enqueue(message);
            }

            // Continue receiving data
            ReceiveData();
        }
        catch (Exception e)
        {
            Debug.LogError("Data receive error: " + e.Message);
        }
    }

    void Update()
    {
        // Process queued commands on the main thread
        lock (queueLock)
        {
            while (commandQueue.Count > 0)
            {
                string command = commandQueue.Dequeue();
                HandleCommand(command);
            }
        }
    }

    void HandleCommand(string command)
    {
        switch (command.Trim().ToUpper()) // Trim and convert to uppercase to avoid case and whitespace issues
        {
            case "TURN_LEFT":
                transform.Rotate(0, -10, 0); // Rotate left
                break;
            case "TURN_RIGHT":
                transform.Rotate(0, 10, 0); // Rotate right
                break;
            case "MOVE_FORWARD":
                transform.Translate(Vector3.forward * Time.deltaTime * 5); // Move forward
                break;
            case "MOVE_BACKWARD":
                transform.Translate(Vector3.back * Time.deltaTime * 5); // Move backward
                break;
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
