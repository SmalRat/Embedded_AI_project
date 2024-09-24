using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class BoxCoordinatesServer : MonoBehaviour
{
    private TcpListener listener;
    private TcpClient client;
    private NetworkStream stream;
    private byte[] buffer = new byte[1024];
    private const int port = 12010;
    private string cachedJson = string.Empty;

    void Start()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.BeginAcceptTcpClient(OnClientConnect, null);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to start server: " + e.Message);
        }
    }

    private void OnClientConnect(IAsyncResult result)
    {
        try
        {
            client = listener.EndAcceptTcpClient(result);
            stream = client.GetStream();
            listener.BeginAcceptTcpClient(OnClientConnect, null);
        }
        catch (Exception e)
        {
            Debug.LogError("Error accepting client: " + e.Message);
        }
    }

    void Update()
    {
        if (stream != null && stream.DataAvailable)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                string combinedMessage = cachedJson + message;
                string[] jsonParts = combinedMessage.Split(new[] { "{" }, StringSplitOptions.RemoveEmptyEntries);
                cachedJson = string.Empty;

                Debug.Log(combinedMessage);

                foreach (var jsonPart in jsonParts)
                {
                    string potentialJson = "{" + jsonPart;

                    if (potentialJson.EndsWith("}"))
                    {
                        BoxCoordinatesData data = JsonUtility.FromJson<BoxCoordinatesData>(potentialJson);
                        Debug.Log("Frame: " + data.frame_index);
                        Debug.Log("Green Box: " + string.Join(", ", data.green_box));
                        Debug.Log("Red Box: " + string.Join(", ", data.red_box));
                    }
                    else
                    {
                        cachedJson = potentialJson;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error receiving data: " + e.Message);
            }
        }
    }

    void OnApplicationQuit()
    {
        if (stream != null)
        {
            stream.Close();
        }
        if (client != null)
        {
            client.Close();
        }
        if (listener != null)
        {
            listener.Stop();
        }
    }

    [Serializable]
    public class BoxCoordinatesData
    {
        public int frame_index;
        public float[] green_box;
        public float[] red_box;
    }
}
