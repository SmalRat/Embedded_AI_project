using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System.IO;
using System.IO.MemoryMappedFiles;
using System;
using System.Collections;

public class FrameSender : MonoBehaviour
{
    public Camera camera;
    // public RawImage displayImage;
    private RenderTexture renderTexture;
    private MemoryMappedFile mmf, mmfWithBoxes;
    private MemoryMappedViewAccessor accessor, accessorWithBoxes;
    private const string filePath = "./Display.dat"; // File path for shared memory
    private const string filePathWithBoxes = "./DisplayWithBoxes.dat";
    private const long memorySize = 640 * 480 * 3; // Adjust based on image resolution and format
    private bool boxesToDraw = false;
    private float[] greenBox;
    private float[] redBox;

    void Start()
    {
        // Create or open the file for shared memory
        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
        {
            fileStream.SetLength(memorySize); // Set the length of the file
        }

        using (var fileStream = new FileStream(filePathWithBoxes, FileMode.Create, FileAccess.ReadWrite))
        {
            fileStream.SetLength(memorySize); // Set the length of the file
        }

        mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, "SharedMemory");
        accessor = mmf.CreateViewAccessor();

        mmfWithBoxes = MemoryMappedFile.CreateFromFile(filePathWithBoxes, FileMode.Open, "SharedMemoryWithBoxes");
        accessorWithBoxes = mmfWithBoxes.CreateViewAccessor();

        // renderTexture = new RenderTexture(640, 480, 24);
        // RenderTexture defaultTexture = camera.targetTexture;
        // camera.targetTexture = renderTexture;
        // displayImage.texture = renderTexture;
        // defaultTexture = renderTexture;

        // camera.targetDisplay = 0;

        UnityEngine.Debug.Log("Shared memory initialized.");
    }

    IEnumerator RecordFrame()
    {
        yield return new WaitForEndOfFrame();

        if (mmf != null && accessor != null)
        {
            try
            {
                renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
                ScreenCapture.CaptureScreenshotIntoRenderTexture(renderTexture);

                RenderTexture rt=new RenderTexture(640, 480, 24);
                Graphics.Blit(renderTexture,rt);

                AsyncGPUReadback.Request(rt, 0, TextureFormat.RGB24, (request) =>
                {
                    ProcessFrame(request, rt);
                });
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Error writing to shared memory: " + e.Message);
            }
        }
    }

    void ReadbackCompleted(AsyncGPUReadbackRequest request)
    {
        DestroyImmediate(renderTexture);

        using (var rawData = request.GetData<byte>())
        {
            try
            {
                if (rawData.Length != memorySize)
                    {
                        UnityEngine.Debug.LogError("Error: Raw data size mismatch.");
                        return;
                    }

                    accessor.WriteArray<byte>(0, rawData.ToArray(), 0, rawData.Length);

                    // Debug.Log("Texture sent and destroyed");
                    System.Threading.Thread.Sleep(50); // Adjust delay as needed
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Error writing to shared memory: " + e.Message);
            }
        }
    }

    void ProcessFrame(AsyncGPUReadbackRequest request, RenderTexture rt)
    {
        DestroyImmediate(renderTexture);

        using (var rawData = request.GetData<byte>())
        {
            try
            {
                if (rawData.Length != memorySize)
                {
                    UnityEngine.Debug.LogError("Error: Raw data size mismatch.");
                    return;
                }

                // Write to the first shared memory file (original image)
                accessor.WriteArray(0, rawData.ToArray(), 0, rawData.Length);

                // If boxes are to be drawn, handle them
                if (boxesToDraw)
                {
                    // Copy the texture and draw boxes
                    Texture2D textureWithBoxes = new Texture2D(640, 480, TextureFormat.RGB24, false);
                    RenderTexture.active = rt;
                    textureWithBoxes.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    textureWithBoxes.Apply();

                    // Draw green and red boxes on the texture
                    DrawBoundingBoxes(textureWithBoxes, greenBox, Color.green);
                    DrawBoundingBoxes(textureWithBoxes, redBox, Color.blue);

                    // DrawSight(textureWithBoxes, Color.red);

                    // Write the updated texture with bounding boxes to the second shared memory file
                    byte[] textureData = textureWithBoxes.GetRawTextureData();
                    if (textureData.Length != memorySize)
                    {
                        UnityEngine.Debug.LogError("Error: Raw data size mismatch.");
                        return;
                    }
                    accessorWithBoxes.WriteArray(0, textureData, 0, textureData.Length);
                    Destroy(textureWithBoxes); // Clean up the texture
                }

                System.Threading.Thread.Sleep(50); // Adjust delay as needed
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Error writing to shared memory: " + e.Message);
            }
        }
    }

    void DrawBoundingBoxes(Texture2D texture, float[] box, Color color)
    {
        // Ensure we have a valid box
        if (box == null || box.Length != 4) return;

        int left = Mathf.FloorToInt(box[0]);
        int top = Mathf.FloorToInt(box[1]);
        int right = Mathf.FloorToInt(box[2]);
        int bottom = Mathf.FloorToInt(box[3]);

        Debug.Log($"{left} {top} {right} {bottom}");
        // Draw the box (horizontal lines)
        for (int x = left; x <= right; x++)
        {
            texture.SetPixel(x, 480 - top, color);    // Top line
            texture.SetPixel(x, 480 - bottom, color); // Bottom line
        }

        // Draw the box (vertical lines)
        for (int y = top; y <= bottom; y++)
        {
            texture.SetPixel(left, 480 - y, color);   // Left line
            texture.SetPixel(right, 480 - y, color);  // Right line
        }
        texture.Apply();
    }

    void DrawSight(Texture2D texture, Color color)
    {
        // int left = Mathf.FloorToInt(box[0]);
        // int top = Mathf.FloorToInt(box[1]);
        // int right = Mathf.FloorToInt(box[2]);
        // int bottom = Mathf.FloorToInt(box[3]);

        // int x_center = left + right / 2;
        // int y_center = top + bottom / 2;
        // int x_center = 120;
        // int y_center = 100;

        // for (int x = 10; x <= 20; x++){
        //     for (int y = 10; y <= 20; x++){
        //         texture.SetPixel(x, y, color);
        //     }
        // }

        // texture.Apply();
    }

    public void UpdateBoxes(float[] greenBox, float[] redBox)
    {
        this.greenBox = greenBox;
        this.redBox = redBox;
        boxesToDraw = true;
    }

    public void LateUpdate()
    {
        StartCoroutine(RecordFrame());
    }

    // void Update()
    // {
    //     if (mmf != null && accessor != null)
    //     {
    //         try
    //         {
    //             Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
    //             RenderTexture.active = renderTexture;
    //             texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
    //             texture.Apply();
    //             RenderTexture.active = null;

    //             byte[] rawData = texture.GetRawTextureData();
    //             if (rawData.Length != memorySize)
    //             {
    //                 UnityEngine.Debug.LogError("Error: Raw data size mismatch.");
    //                 return;
    //             }

    //             // Write to shared memory
    //             accessor.WriteArray(0, rawData, 0, rawData.Length);

    //             Destroy(texture);
    //             // System.Threading.Thread.Sleep(50); // Adjust delay as needed
    //         }
    //         catch (Exception e)
    //         {
    //             UnityEngine.Debug.LogError("Error writing to shared memory: " + e.Message);
    //         }
    //     }
    // }

    void OnApplicationQuit()
    {
        if (accessor != null)
        {
            accessor.Dispose();
        }
        if (mmf != null)
        {
            mmf.Dispose();
        }
    }
}
