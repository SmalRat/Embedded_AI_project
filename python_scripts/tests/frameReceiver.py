import socket
import numpy as np
import cv2
import matplotlib.pyplot as plt

def start_server():
    port = 12000
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    server_socket.bind(('0.0.0.0', port))  # Listen on all interfaces, port 22333

    print(f"Server listening on port {port}")

    buffer = b''
    server_ip = "192.168.1.109" 
    send_port = 22334  # Different port for sending control signals
    gst_port = 12001  # Port for GStreamer video streaming
    hostip = '192.168.1.109'  # Replace with your client IP address

    plt.ion()  # Enable interactive mode
    fig, ax = plt.subplots()

    # GStreamer pipeline string
    gst_out = (
        f'appsrc ! videoconvert ! x264enc tune=zerolatency bitrate=500 speed-preset=superfast ! '
        f'rtph264pay config-interval=1 pt=96 ! udpsink host={hostip} port={gst_port} auto-multicast=true'
    )

    # Placeholder for video properties
    fps = 30  # Assuming a default FPS of 30; update with actual FPS
    w = 1920  # Assuming default width; update with actual width
    h = 1080  # Assuming default height; update with actual height
    out = None

    while True:
        try:
            # Receive data from client
            data, addr = server_socket.recvfrom(65507)  # Buffer size for UDP
            if not data:
                break

            buffer += data

            # Convert byte data to numpy array and decode image
            np_array = np.frombuffer(data, dtype=np.uint8)
            image = cv2.imdecode(np_array, cv2.IMREAD_COLOR)

            if image is not None:
                # Initialize GStreamer VideoWriter if not done yet
                if out is None:
                    fps = 30  # Adjust if necessary
                    w = image.shape[1]
                    h = image.shape[0]
                    out = cv2.VideoWriter(gst_out, cv2.CAP_GSTREAMER, 0, fps, (w, h), True)
                    if not out.isOpened():
                        print("Error: GStreamer pipeline not opened.")
                        break

                # Convert image to RGB format for Matplotlib
                image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
                ax.clear()
                ax.imshow(image_rgb)
                ax.axis('off')  # Hide axes
                plt.draw()
                plt.pause(0.001)  # Pause to update the plot

                # Stream the frame using GStreamer
                out.write(image)

                # Detect blue points
                lower_blue = np.array([100, 150, 0])
                upper_blue = np.array([140, 255, 255])

                # Convert image to HSV
                hsv_image = cv2.cvtColor(image, cv2.COLOR_BGR2HSV)
                mask = cv2.inRange(hsv_image, lower_blue, upper_blue)

                # Find contours in the mask
                contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

                if contours:
                    # Find the largest contour
                    largest_contour = max(contours, key=cv2.contourArea)
                    M = cv2.moments(largest_contour)
                    if M["m00"] > 0:
                        cX = int(M["m10"] / M["m00"])
                        cY = int(M["m01"] / M["m00"])
                        print(f"Blue point detected at: ({cX}, {cY})")

                        # Send direction command based on the position of the blue point
                        command = ""
                        image_center_x = image.shape[1] // 2

                        if cX < image_center_x - 50:
                            command = "TURN_LEFT"
                        elif cX > image_center_x + 50:
                            command = "TURN_RIGHT"
                        else:
                            command = "MOVE_FORWARD"

                        print(f"Command: {command}")

                        # Send command back to Unity
                        udp_client = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
                        udp_client.sendto(command.encode('utf-8'), (hostip, send_port))
                        udp_client.close()

            # Clear buffer after processing
            buffer = b''

        except Exception as e:
            print(f"Error: {e}")
            break

    # Release resources
    if out:
        out.release()
    server_socket.close()
    plt.close(fig)

if __name__ == "__main__":
    start_server()
