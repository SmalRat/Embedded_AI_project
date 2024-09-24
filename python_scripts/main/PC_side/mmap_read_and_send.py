import mmap
import numpy as np
import cv2
import os
import sys
import time

# File path for shared memory
file_path = r'../../../Project/Display.dat'
memory_size = 640 * 480 * 3  # Adjust based on image resolution and format

hostip = '192.168.1.105'  # Replace with your target host IP address
port_out = 12000  # Replace with your target port number

gst_out = (
    f'appsrc ! videoconvert ! x264enc tune=zerolatency bitrate=500 speed-preset=superfast ! '
    f'rtph264pay config-interval=1 pt=96 ! udpsink host={hostip} port={port_out} auto-multicast=true'
)

# Initialize the VideoWriter with GStreamer pipeline
out = cv2.VideoWriter(gst_out, cv2.CAP_GSTREAMER, 0, 20, (640, 480), True)

slide_window = []  # To store timestamps for FPS calculation
window_size = 1.0  # Sliding window size in seconds

# Check if the VideoWriter has been initialized correctly
if not out.isOpened():
    raise RuntimeError("Failed to open GStreamer pipeline. Check if the GStreamer pipeline string is correct.")

# Check if the file exists
if not os.path.exists(file_path):
    raise FileNotFoundError(f"Shared memory file not found: {file_path}")

# Open the shared memory file
i = 0
with open(file_path, 'r+b') as f:
    mm = mmap.mmap(f.fileno(), memory_size, access=mmap.ACCESS_READ)

    while True:
        start_time = time.time()  # Capture the start time of the frame

        # Read the raw data from the memory-mapped file
        raw_data = mm[:memory_size]

        # Convert the raw data to an image
        img = np.frombuffer(raw_data, dtype=np.uint8).reshape((480, 640, 3))  # Adjust dimensions based on image

        # Rotate the image 180 degrees
        img = cv2.rotate(img, cv2.ROTATE_180)

        img = cv2.flip(img, 1)

        # Convert from RGB (as read from shared memory) to BGR for OpenCV
        img = cv2.cvtColor(img, cv2.COLOR_RGB2BGR)

        # Write the frame to the GStreamer pipeline
        out.write(img)

        # Update the sliding window with the current timestamp
        slide_window.append(start_time)

        # Remove timestamps outside the window size
        while slide_window and slide_window[0] < start_time - window_size:
            slide_window.pop(0)

        # Calculate FPS
        if len(slide_window) > 1:
            fps = len(slide_window) / (slide_window[-1] - slide_window[0])
        else:
            fps = 0.0

        # print(f"Frame: {i}")
        print(f"FPS: {fps:.2f}")
        i += 1

        # Allow for window events and set a delay for real-time display (1 ms delay)
        # if cv2.waitKey(1) & 0xFF == ord('q'):
        #     break

    mm.close()

cv2.destroyAllWindows()
out.release()
