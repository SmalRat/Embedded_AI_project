import time
import os
import cv2
import sys
import argparse
import socket
import json
import numpy as np

from inference_module import *

IMG_SIZE = (640, 640) # Todo: check

server_ip = '0.0.0.0'
port_in = 12000

gst_in = (
    f'udpsrc port={port_in} ! application/x-rtp, payload=96 ! rtph264depay ! avdec_h264 ! '
    'videoconvert ! appsink'
    # "gst-launch-1.0 udpsrc port=12000 ! application/x-raw,format=RGB,width=640,height=480,framerate=24/1 ! appsink"
)


hostip = '192.168.1.110'
port_out = 12001

gst_out = (
    f'appsrc ! videoconvert ! x264enc tune=zerolatency bitrate=500 speed-preset=superfast ! '
    f'rtph264pay config-interval=1 pt=96 ! udpsink host={hostip} port={port_out} auto-multicast=true'
)

TCP_IP = '192.168.1.110'
TCP_PORT = 12010

def log_fps(slide_window = [], window_size = 1.0):
    start_time = time.time()
    while slide_window and slide_window[0] < start_time - window_size:
        slide_window.pop(0)

    slide_window.append(start_time)

    if len(slide_window) > 1:
        fps = len(slide_window) / (slide_window[-1] - slide_window[0])
    else:
        fps = 0.0

    print(f"FPS: {fps:.2f}")

def predict_next_position(box, delta_x=0, delta_y=0):
    """Predict the next position of the box."""
    x1, y1, x2, y2 = box
    return [x1 + delta_x, y1 + delta_y, x2 + delta_x, y2 + delta_y]

# prev_box = None
# prev_time = None
# def predict_next_position(box, delta_x=0, delta_y=0):
#     """Predict the next position of the box."""
#     if prev_box:
#         x1, y1, x2, y2 = box
#         px1, py1, px2, py2 = prev_box
#         pcx = (px1 + px2) / 2
#         pcy = (py1 + py2) / 2
#         cx = (x1 + x2) / 2
#         cy = (y1 + y2) / 2
#         diff = (cx - pcx, cy - pcy)
#         prev_time = time.time()
#         prev_box = box
#         return speed
#     return 0

def parsing():
    parser = argparse.ArgumentParser(description='Process some integers.')
    # basic params
    parser.add_argument('--model_path', type=str, required= True, help='model path, could be .pt or .rknn file')
    parser.add_argument('--target', type=str, default='rk3588', help='target RKNPU platform')
    parser.add_argument('--device_id', type=str, default=None, help='device id')

    # data params
    parser.add_argument('--anno_json', type=str, default='../../../datasets/COCO/annotations/instances_val2017.json', help='coco annotation path')
    # coco val folder: '../../../datasets/COCO//val2017'
    parser.add_argument('--coco_map_test', action='store_true', help='enable coco map test')
    parser.add_argument('--anchors', type=str, default='../model/anchors_yolov5.txt', help='target to anchor file, only yolov5, yolov7 need this param')

    args = parser.parse_args()

    return args

def get_anchors(args):
    with open(args.anchors, 'r') as f:
        values = [float(_v) for _v in f.readlines()]
        anchors = np.array(values).reshape(3,-1,2).tolist()
    print("use anchors from '{}', which is {}".format(args.anchors, anchors))

    return anchors

def setup_connections():
    in_stream = cv2.VideoCapture(gst_in, cv2.CAP_GSTREAMER)
    if not in_stream.isOpened():
        print("Error: Could not open video stream.")
        exit(1)

    conn = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    conn.connect((TCP_IP, TCP_PORT))

    fps = int(in_stream.get(cv2.CAP_PROP_FPS))
    w = int(in_stream.get(cv2.CAP_PROP_FRAME_WIDTH))
    h = int(in_stream.get(cv2.CAP_PROP_FRAME_HEIGHT))

    print("In stream FPS: ", fps)
    print("In stream Width: ", w)
    print("In stream Height: ", h)

    out_stream = cv2.VideoWriter(gst_out, cv2.CAP_GSTREAMER, 0, fps, (w, h), True)

    return in_stream, (fps, w, h), out_stream, conn


def get_inputs(platform, img_src):
    # Due to rga init with (0,0,0), we using pad_color (0,0,0) instead of (114, 114, 114)
    pad_color = (0,0,0)
    img = co_helper.letter_box(im= img_src, new_shape=(IMG_SIZE[1], IMG_SIZE[0]), pad_color=pad_color)
    img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)

    # preprocee if not rknn model
    if platform in ['pytorch', 'onnx']:
        input_data = img.transpose((2,0,1))
        input_data = input_data.reshape(1,*input_data.shape).astype(np.float32)
        input_data = input_data/255.
    else:
        input_data = img
        input_data = input_data.reshape(1,*input_data.shape)

    return input_data


def generate_feedback(frame_id, img_src, boxes, scores, classes):
    if boxes is not None:
        for k, box in enumerate(co_helper.get_real_box(boxes)):
            green_box = box.tolist()
            red_box = predict_next_position(green_box)

            # Send coordinates over TCP as JSON
            # print(green_box)
            data = json.dumps({
                'frame_index': frame_id,
                'green_box': green_box,
                'red_box': red_box,
                'class_name': CLASSES[classes[k]]
            })
            feedback_connection.sendall(data.encode('utf-8'))
        draw(img_src, co_helper.get_real_box(boxes), scores, classes)

def process_frame(frame_id, model, platform, anchors, img_src):
    input_data = get_inputs(platform, img_src)
    outputs = model.run([input_data])
    boxes, classes, scores = post_process(outputs, anchors, (IMG_SIZE[1], IMG_SIZE[0]))

    generate_feedback(frame_id, img_src, boxes, scores, classes)

    out_stream.write(img_src)

def release_resources(feedback_connection, in_stream, out_stream, cap):
    feedback_connection.close()
    cap.release()
    out_stream.release()
    in_stream.release()

if __name__ == '__main__':

    args = parsing()

    anchors = get_anchors(args)
    model, platform = setup_model(args)
    co_helper = COCO_test_helper(enable_letter_box=True)

    in_stream, video_params, out_stream, feedback_connection = setup_connections()

    i = 0

    while in_stream.isOpened():
        ret, img_src = in_stream.read()
        if not ret:
            break

        process_frame(i, model, platform, anchors, img_src)
        i = i+1
        log_fps()
        # if (i % 10 == 0):
        #     print(f"Frame: {i}")

    release_resources(feedback_connection, in_stream, out_stream, cap)
    print("Video streaming completed.")
