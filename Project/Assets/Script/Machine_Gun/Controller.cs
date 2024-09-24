using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class Controller : MonoBehaviour
{
    private TcpListener listener;
    private TcpClient client;
    private NetworkStream stream;
    private byte[] buffer = new byte[1024];
    private const int port = 12010;
    private string cachedJson = string.Empty;

    public Camera mainCamera;
    public FireAtTarget fireAtTargetScript;

    private float aimX = 0;
    private float aimY = 0;

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

    float[] RescaleBoxCoordinates(float[] box, float widthScale, float heightScale)
    {
        float[] updatedBox = new float[4];
        updatedBox[0] = box[0] * widthScale; 
        updatedBox[1] = box[1] * heightScale;  
        updatedBox[2] = box[2] * widthScale; 
        updatedBox[3] = box[3] * heightScale;  

        return updatedBox;
    }


    void networkUpdate(){
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
                        Debug.Log("Class: " + data.class_name);

                        FindObjectOfType<FrameSender>().UpdateBoxes(data.green_box, data.red_box);

                        float widthScale = Screen.width / 640f;
                        float heightScale = Screen.height / 480f;

                        Debug.Log($"W{widthScale}");
                        Debug.Log($"H{heightScale}");
                        var green_box_rescaled = RescaleBoxCoordinates(data.green_box, widthScale, heightScale);
                        var red_box_rescaled = RescaleBoxCoordinates(data.red_box, widthScale, heightScale);

                        // Debug.Log($"RW{red_box_rescaled[0]}");
                        // Debug.Log($"RW{red_box_rescaled[2]}");
                        // Debug.Log($"RH{red_box_rescaled[1]}");
                        // Debug.Log($"RH{red_box_rescaled[3]}");
                        aimX = ((red_box_rescaled[0] + red_box_rescaled[2]) / 2);
                        // Debug.Log($"AX{aimX}");
                        aimY = Screen.height - ((red_box_rescaled[1] + red_box_rescaled[3]) / 2);
                        // Debug.Log($"AY{aimY}");

                        fireAtTargetScript.connected = true;
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

    void Update()
    {
        networkUpdate();

        Vector3 screenPoint = new Vector3(aimX, aimY, 0);
        // Vector3 screenPoint = Input.mousePosition;
        Debug.Log($"Screen : {screenPoint}");

        Ray ray = mainCamera.ScreenPointToRay(screenPoint);

        fireAtTargetScript.SetRayDirection(ray.direction);
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
        public string class_name;
    }
}
