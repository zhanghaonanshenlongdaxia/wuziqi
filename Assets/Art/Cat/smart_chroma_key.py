# -*- coding: utf-8 -*-
"""智能抠图：检测每帧实际背景色，替换为透明"""

import os, sys, subprocess
from PIL import Image, ImageFilter
import numpy as np

FFMPEG = r'C:\Users\zhn\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0-full_build\bin\ffmpeg.exe'

def detect_bg_color(pixels, sample_step=10):
    """从边缘采样检测背景色"""
    h, w = pixels.shape[:2]
    edge_pixels = []

    # 上下边缘
    for x in range(0, w, sample_step):
        edge_pixels.append(tuple(pixels[0, x]))
        edge_pixels.append(tuple(pixels[h-1, x]))
    # 左右边缘
    for y in range(0, h, sample_step):
        edge_pixels.append(tuple(pixels[y, 0]))
        edge_pixels.append(tuple(pixels[y, w-1]))

    # 用中位数（比均值更稳定）
    return np.median(edge_pixels, axis=0).astype(float)

def chroma_key_frame(img, bg_color=None, threshold=70):
    """对单帧进行抠图，返回RGBA图像"""
    pixels = np.array(img.convert('RGB'))
    h, w = pixels.shape[:2]

    if bg_color is None:
        bg_color = detect_bg_color(pixels)

    # 计算每个像素到背景色的距离
    dist = np.sqrt(np.sum((pixels.astype(float) - bg_color) ** 2, axis=2))

    # 创建alpha通道
    alpha = np.zeros((h, w), dtype=np.uint8)

    # 距离 < threshold: 完全透明（背景）
    alpha[dist < threshold] = 0

    # 距离 >= threshold * 1.5: 完全不透明（前景）
    alpha[dist >= threshold * 1.5] = 255

    # 中间区域：渐变（边缘平滑）
    edge_mask = (dist >= threshold) & (dist < threshold * 1.5)
    alpha[edge_mask] = ((dist[edge_mask] - threshold) / (threshold * 0.5) * 255).astype(np.uint8)

    # 组合RGBA
    result = np.dstack([pixels, alpha])
    return Image.fromarray(result, 'RGBA'), bg_color

def process_video_frames(video_path, output_dir, target_fps=6):
    """处理视频：提取帧并抠图"""
    os.makedirs(output_dir, exist_ok=True)
    raw_dir = os.path.join(output_dir, "_raw")
    os.makedirs(raw_dir, exist_ok=True)

    # 提取帧
    subprocess.run([FFMPEG, "-y", "-i", video_path, "-vf", f"fps={target_fps}",
                    os.path.join(raw_dir, "f_%04d.png")], capture_output=True)

    raw_files = sorted([f for f in os.listdir(raw_dir) if f.endswith(".png")])
    print(f"Extracted {len(raw_files)} frames")

    # 处理每帧（每帧独立检测背景色）
    for i, fname in enumerate(raw_files):
        img = Image.open(os.path.join(raw_dir, fname))
        result, _ = chroma_key_frame(img, threshold=80)

        # 保存为256x256
        result = result.resize((256, 256), Image.LANCZOS)
        result.save(os.path.join(output_dir, f"f_{i:04d}.png"))

    return len(raw_files)


if __name__ == "__main__":
    video_path = r'H:/ComfyUI_windows_portable/ComfyUI/output/黑炭_smug_00001_.mp4'
    output_dir = r'e:\UnityProject\wuziqi\Assets\Art\Cat\Frames\黑炭\smug'

    count = process_video_frames(video_path, output_dir, target_fps=6)
    print(f"Done! {count} frames processed")
