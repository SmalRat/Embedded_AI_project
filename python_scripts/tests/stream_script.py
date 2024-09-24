import cv2

# Define GStreamer pipeline for output
host_ip = '192.168.1.102'  # Replace with your server's IP address
port = 12000  # Port to send video to the server

# Open video capture
# cap = cv2.VideoCapture("/home/oleksandr/Desktop/UCU_Subjects/EMAI/Lab3/test/test2.mp4")  # Change to video file path if needed
cap = cv2.VideoCapture(0)  # Change to video file path if needed

# Get video properties
fps = int(cap.get(cv2.CAP_PROP_FPS))
w = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
h = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))

print("FPS: ", fps)
print("Width: ", w)
print("Height: ", h)

# GStreamer pipeline string for sending video
gst_out = (
    f'appsrc ! videoconvert ! x264enc tune=zerolatency bitrate=500 speed-preset=superfast ! '
    f'rtph264pay config-interval=1 pt=96 ! udpsink host={host_ip} port={port}'
)

print(gst_out)
# Initialize VideoWriter with GStreamer pipeline
out = cv2.VideoWriter(gst_out, cv2.CAP_GSTREAMER, 0, fps, (w, h), True)

while cap.isOpened():
    ret, frame = cap.read()
    if not ret:
        break

    # Write frame to GStreamer pipeline
    out.write(frame)

# Release resources
cap.release()
out.release()
print("Video streaming completed.")

