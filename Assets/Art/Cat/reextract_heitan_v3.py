# reextract_heitan_v3.py
# 改进的绿幕抠图算法

import subprocess
from PIL import Image
import numpy as np
import os

FFMPEG = r"C:\Users\zhn\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0-full_build\bin\ffmpeg.exe"

VIDEO_DIR = r"H:\ComfyUI_windows_portable\ComfyUI\output"
OUTPUT_DIR = r"e:\UnityProject\wuziqi\Assets\Art\Cat\Frames\黑炭"

VIDEOS = {
    "smug": "黑炭_smug_00001_.mp4",
    "thinking": "黑炭_thinking_00001_.mp4",
    "worried": "黑炭_worried_00001_.mp4",
}

def chroma_key_green_v3(pixels):
    """
    改进的绿幕抠图：
    1. 绿色必须明显主导 (G - max(R,B) > 40)
    2. G值本身必须足够高 (> 100)，避免误删深色像素
    """
    r, g, b = pixels[:,:,0].astype(float), pixels[:,:,1].astype(float), pixels[:,:,2].astype(float)

    # 绿色主导程度
    green_dominance = g - np.maximum(r, b)

    # 初始化alpha为不透明
    h, w = pixels.shape[:2]
    alpha = np.full((h, w), 255, dtype=np.uint8)

    # 条件1: 完全绿幕 - 绿色明显主导且G值高
    full_green = (green_dominance > 40) & (g > 100)
    alpha[full_green] = 0

    # 条件2: 边缘过渡 - 绿色有一定主导且G值中等
    edge_green = (green_dominance > 20) & (green_dominance <= 40) & (g > 80)
    alpha[edge_green] = (255 - (green_dominance[edge_green] - 20) * 255 / 20).astype(np.uint8)

    return alpha

def extract_and_chroma_key(video_name, expression, fps=6):
    """从视频抽帧并抠图"""
    video_path = os.path.join(VIDEO_DIR, video_name)
    frame_dir = os.path.join(OUTPUT_DIR, expression)
    raw_dir = os.path.join(frame_dir, "_raw")
    os.makedirs(raw_dir, exist_ok=True)

    # 抽帧
    subprocess.run([
        FFMPEG, "-y", "-i", video_path,
        "-vf", f"fps={fps}",
        os.path.join(raw_dir, "f_%04d.png")
    ], capture_output=True)

    raw_files = sorted([f for f in os.listdir(raw_dir) if f.endswith(".png")])
    print(f"  Extracted {len(raw_files)} raw frames")

    # 抠图
    for fname in raw_files:
        raw_path = os.path.join(raw_dir, fname)
        out_path = os.path.join(frame_dir, fname)

        img = Image.open(raw_path).convert("RGB")
        pixels = np.array(img)
        alpha = chroma_key_green_v3(pixels)

        result = np.dstack([pixels, alpha])
        result_img = Image.fromarray(result, 'RGBA')
        result_img = result_img.resize((256, 256), Image.LANCZOS)
        result_img.save(out_path)

    print(f"  Chroma-keyed {len(raw_files)} frames")
    return len(raw_files)

def main():
    print("=" * 50)
    print("HeiTan frames extraction (improved chroma key)")
    print("=" * 50)

    for expr, video_name in VIDEOS.items():
        print(f"\n--- {expr} ---")
        count = extract_and_chroma_key(video_name, expr)
        print(f"  Done: {count} frames")

    # 检查缺失
    missing = ["celebrate", "defeat", "idle", "happy"]
    for expr in missing:
        expr_dir = os.path.join(OUTPUT_DIR, expr)
        if not os.path.exists(expr_dir) or len([f for f in os.listdir(expr_dir) if f.endswith(".png")]) < 28:
            print(f"\n[MISSING] {expr}")

if __name__ == "__main__":
    main()
