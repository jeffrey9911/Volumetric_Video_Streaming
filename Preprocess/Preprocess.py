import os
import re

import tkinter as tk
from tkinter import filedialog

import subprocess

import json

import time

import math

import queue

# Default settings
_DracoBuildPath = "draco/build/draco_encoder"
_Mesh_Format = ".obj"
_Texture_Format = ".png"
_Audio_Format = ".wav"
_FPS = 30
_Texture_res_w = 2048
_Texture_res_h = 1024

def GetFiles(filePath, fileType):
    files = []
    for fileName in os.listdir(filePath):
        if fileName.endswith(fileType):
            files.append(os.path.join(filePath, fileName))
    return files


def natural_sort_key(s):
    return [int(c) if c.isdigit() else c for c in re.split(r'(\d+)', s)]

def SelectFile(textDisplay):
    root = tk.Tk()
    root.withdraw()
    selected_file = filedialog.askdirectory(title=textDisplay)
    return selected_file

def get_audio_length_ffmpeg(file_path):
    cmd = ['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'default=noprint_wrappers=1:nokey=1', file_path]
    result = subprocess.run(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    duration_str = result.stdout.decode().strip()
    try:
        return float(duration_str)
    except ValueError:
        print("\nCould not determine the file duration.")
        return None
    
def FilePattern(input_filename):
    match = re.search(r"(\d+)(?=\.\w+$)", input_filename)
    if match:
        index = match.group(1)
        length = len(index)
        pattern = re.sub(r"\d+(?=\.\w+$)", f"%0{length}d", input_filename)
        return pattern
    else:
        return "No digits found in the file name."
    
def auto_floor_ceil(value):
    if value - math.floor(value) < 0.5:
        return math.floor(value)
    else:
        return math.ceil(value)


if not os.path.exists(_DracoBuildPath):
    input("\nDraco build folder not found. Click Enter to exit.")
    exit()
else:
    print("\nDraco build folder found.")


gname = input("\nPlease enter the name of the VV: ")

input("\nClick Enter to select input folder.")
_Input_Path = SelectFile("Select input folder.")

input("\nClick Enter to select output folder.")
_Output_Path = SelectFile("Select output folder")

vv_name = f"vv-{gname}-{str(int(time.time()))}"

_Output_Path = os.path.join(_Output_Path, vv_name)
os.makedirs(_Output_Path, exist_ok=True)

_Mesh_Files = GetFiles(_Input_Path, _Mesh_Format)
_Mesh_Files.sort(key=natural_sort_key)

_Mesh_Output = []
for meshFile in _Mesh_Files:
    _Mesh_Output.append(os.path.basename(meshFile).replace(_Mesh_Format, '.drc'))

_Texture_Files = GetFiles(_Input_Path, _Texture_Format)
_Texture_Files.sort(key=natural_sort_key)
for i in range(len(_Texture_Files)):
    os.rename(_Texture_Files[i], f"{_Input_Path}/tex_{i:06d}{_Texture_Format}")
_Texture_Files = GetFiles(_Input_Path, _Texture_Format)
_Texture_Files.sort(key=natural_sort_key)

_Audio_Files = GetFiles(_Input_Path, _Audio_Format)
_Audio_Files.sort(key=natural_sort_key)
isAudio = False

if (len(_Audio_Files) > 0):
    _FPS = auto_floor_ceil(len(_Texture_Files) / get_audio_length_ffmpeg(_Audio_Files[0]))
    isAudio = True

for meshFile in _Mesh_Files:
    draco_cli = [
        f"{_DracoBuildPath}",
        "-i", meshFile,
        "-o", f"{_Output_Path}/{os.path.basename(meshFile).replace(_Mesh_Format, '.drc')}",
        "-cl", "10"
    ]
    subprocess.run(draco_cli)


if isAudio:
    ffmpeg_cli_audio = [
        'ffmpeg',
        '-framerate', f'{_FPS}',
        '-i', f"{_Input_Path}/{FilePattern(os.path.basename(_Texture_Files[0]))}",
        '-i', _Audio_Files[0],
        '-c:v', 'libx265',
        '-c:a', 'aac',
        '-vf', f'scale={_Texture_res_w}:{_Texture_res_h}:force_original_aspect_ratio=decrease,pad={_Texture_res_w}:{_Texture_res_h}:(ow-iw)/2:(oh-ih)/2',
        '-crf', '28',
        '-preset', 'slower',
        '-tag:v', 'hvc1',
        '-pix_fmt', 'yuv420p',
        f"{_Output_Path}/texture.mp4"
    ]
    subprocess.run(ffmpeg_cli_audio)
else:
    ffmpeg_cli_noaudio = [
    'ffmpeg',
    '-framerate', f'{_FPS}',
    '-i', f"{_Input_Path}/{FilePattern(os.path.basename(_Texture_Files[0]))}",
    '-c:v', 'libx265',
    '-vf', f'scale={_Texture_res_w}:{_Texture_res_h}:force_original_aspect_ratio=decrease,pad={_Texture_res_w}:{_Texture_res_h}:(ow-iw)/2:(oh-ih)/2',
    '-crf', '28',
    '-preset', 'slower',
    '-tag:v', 'hvc1',
    '-pix_fmt', 'yuv420p',
    f"{_Output_Path}/texture.mp4"
    ]
    subprocess.run(ffmpeg_cli_noaudio)



_Manifest = {
    "name" : vv_name,
    "fps" : _FPS,
    "audio" : isAudio,
    "count" : len(_Mesh_Files),
    "texture" : "texture.mp4",
    "meshes" : _Mesh_Output
}

with open(f"{_Output_Path}/manifest.json", "w") as json_file:
    json.dump(_Manifest, json_file)


DropCount = 0
_Missing_Meshes = queue.Queue(maxsize=0)
for meshFile in _Mesh_Output:
    if not os.path.exists(f"{_Output_Path}/{meshFile}"):
        _Missing_Meshes.put(meshFile)
    else:
        while not _Missing_Meshes.empty():
            missing_file = _Missing_Meshes.get()
            DropCount += 1
            with open(f"{_Output_Path}/{meshFile}", "rb") as f:
                data = f.read()
            with open(f"{_Output_Path}/{missing_file}", "wb") as f:
                f.write(data)
            print(f"Duplicated {missing_file} from {meshFile}.")

while not _Missing_Meshes.empty():
    missing_file = _Missing_Meshes.get()
    with open(f"{_Output_Path}/{_Mesh_Output[0]}", "rb") as f:
        data = f.read()
    with open(f"{_Output_Path}/{missing_file}", "wb") as f:
        f.write(data)
    print(f"Duplicated {missing_file} from {_Mesh_Output[0]}.")

print(f"\n{DropCount} dropped meshes.")
            

input("\nDone. Click Enter to exit.")