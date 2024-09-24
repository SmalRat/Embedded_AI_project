import mmap
import numpy as np
import cv2
import os
import time

# File path for shared memory
file_path = r'../../../Project/DisplayWithBoxes.dat'
memory_size = 640 * 480 * 3  # Adjust based on image resolution and format

# Check if the file exists
if not os.path.exists(file_path):
    raise FileNotFoundError(f"Shared memory file not found: {file_path}")

slide_window = []  # To store timestamps for FPS calculation
window_size = 1.0  # Sliding window size in seconds

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

        # Optional: Flip the image horizontally
        img = cv2.flip(img, 1)

        # Convert from RGB (as read from shared memory) to BGR for OpenCV display
        img = cv2.cvtColor(img, cv2.COLOR_RGB2BGR)

        # Display the image in a window
        cv2.imshow('Real-Time Display With Boxes', img)

        # Calculate and display FPS
        slide_window.append(start_time)
        while slide_window and slide_window[0] < start_time - window_size:
            slide_window.pop(0)

        if len(slide_window) > 1:
            fps = len(slide_window) / (slide_window[-1] - slide_window[0])
        else:
            fps = 0.0
        print(f"FPS: {fps:.2f}")
        i += 1

        # Allow for window events and set a delay for real-time display (1 ms delay)
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    # Clean up
    mm.close()

cv2.destroyAllWindows()
