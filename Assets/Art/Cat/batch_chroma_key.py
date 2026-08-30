# -*- coding: utf-8 -*-
"""批量处理视频：提取帧并智能抠图"""

import os, sys, subprocess
from PIL import Image
import numpy as np
import glob

FFMPEG = r'C:\Users\zhn\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0-full_build\bin\ffmpeg.exe'
VIDEO_DIR = r"H:\ComfyUI_windows_portable\ComfyUI\output"
FRAMES_DIR = r"e:\UnityProject\wuziqi\Assets\Art\Cat\Frames"

CATS = ["黑炭", "花斑", "银渐层", "玄猫", "仙喵长老"]
EXPRESSIONS = ["thinking", "smug", "worried", "celebrate", "defeat"]


def detect_bg_color(pixels, sample_step=10):
    """从边缘采样检测背景色"""
    h, w = pixels.shape[:2]
    edge_pixels = []
    for x in range(0, w, sample_step):
        edge_pixels.append(tuple(pixels[0, x]))
        edge_pixels.append(tuple(pixels[h-1, x]))
    for y in range(0, h, sample_step):
        edge_pixels.append(tuple(pixels[y, 0]))
        edge_pixels.append(tuple(pixels[y, w-1]))
    return np.median(edge_pixels, axis=0).astype(float)


def chroma_key_frame(img, threshold=80):
    """对单帧进行抠图，返回RGBA图像"""
    pixels = np.array(img.convert('RGB'))
    h, w = pixels.shape[:2]

    bg_color = detect_bg_color(pixels)
    dist = np.sqrt(np.sum((pixels.astype(float) - bg_color) ** 2, axis=2))

    alpha = np.zeros((h, w), dtype=np.uint8)
    alpha[dist < threshold] = 0
    alpha[dist >= threshold * 1.5] = 255

    edge_mask = (dist >= threshold) & (dist < threshold * 1.5)
    alpha[edge_mask] = ((dist[edge_mask] - threshold) / (threshold * 0.5) * 255).astype(np.uint8)

    result = np.dstack([pixels, alpha])
    return Image.fromarray(result, 'RGBA')


def process_video(video_path, output_dir, target_fps=6):
    """处理视频：提取帧并抠图"""
    os.makedirs(output_dir, exist_ok=True)
    raw_dir = os.path.join(output_dir, "_raw")
    os.makedirs(raw_dir, exist_ok=True)

    subprocess.run([FFMPEG, "-y", "-i", video_path, "-vf", f"fps={target_fps}",
                    os.path.join(raw_dir, "f_%04d.png")], capture_output=True)

    raw_files = sorted([f for f in os.listdir(raw_dir) if f.endswith(".png")])

    for fname in raw_files:
        img = Image.open(os.path.join(raw_dir, fname))
        result = chroma_key_frame(img, threshold=80)
        result = result.resize((256, 256), Image.LANCZOS)
        result.save(os.path.join(output_dir, fname))

    return len(raw_files)


def main():
    # 找所有已完成的视频
    videos = glob.glob(os.path.join(VIDEO_DIR, "*.mp4"))

    processed = 0
    for video_path in sorted(videos):
        fname = os.path.basename(video_path)

        # 匹配猫名和表情
        matched_cat = None
        matched_exp = None
        for cat in CATS:
            if fname.startswith(cat + "_"):
                matched_cat = cat
                for exp in EXPRESSIONS:
                    if fname.startswith(cat + "_" + exp):
                        matched_exp = exp
                        break
                break

        if not matched_cat or not matched_exp:
            continue

        output_dir = os.path.join(FRAMES_DIR, matched_cat, matched_exp)

        # 检查是否已处理
        if os.path.exists(output_dir):
            existing = len([f for f in os.listdir(output_dir) if f.endswith('.png')])
            if existing >= 28:
                print(f"[SKIP] {matched_cat}/{matched_exp} ({existing} frames)")
                continue

        print(f"[PROCESS] {matched_cat}/{matched_exp}...")
        count = process_video(video_path, output_dir, target_fps=6)
        print(f"  Done: {count} frames")
        processed += 1

    print(f"\nTotal processed: {processed}")


if __name__ == "__main__":
    main()
