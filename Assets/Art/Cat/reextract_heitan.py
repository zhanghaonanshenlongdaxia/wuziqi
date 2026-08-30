# reextract_heitan.py
# 重新为黑炭抽帧并抠图

import subprocess
from PIL import Image
import numpy as np
import os

FFMPEG = r"C:\Users\zhn\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0-full_build\bin\ffmpeg.exe"

VIDEO_DIR = r"H:\ComfyUI_windows_portable\ComfyUI\output"
OUTPUT_DIR = r"e:\UnityProject\wuziqi\Assets\Art\Cat\Frames\黑炭"

# 已有的视频
VIDEOS = {
    "smug": "黑炭_smug_00001_.mp4",
    "thinking": "黑炭_thinking_00001_.mp4",
    "worried": "黑炭_worried_00001_.mp4",
}

def extract_and_chroma_key(video_name, expression, fps=6):
    """从视频抽帧并抠图"""
    video_path = os.path.join(VIDEO_DIR, video_name)
    frame_dir = os.path.join(OUTPUT_DIR, expression)
    raw_dir = os.path.join(frame_dir, "_raw")
    os.makedirs(raw_dir, exist_ok=True)

    # 抽帧到_raw目录
    subprocess.run([
        FFMPEG, "-y", "-i", video_path,
        "-vf", f"fps={fps}",
        os.path.join(raw_dir, "f_%04d.png")
    ], capture_output=True)

    raw_files = sorted([f for f in os.listdir(raw_dir) if f.endswith(".png")])
    print(f"  Extracted {len(raw_files)} raw frames")

    # 抠图处理
    for fname in raw_files:
        raw_path = os.path.join(raw_dir, fname)
        out_path = os.path.join(frame_dir, fname)

        img = Image.open(raw_path).convert("RGB")
        pixels = np.array(img)
        h, w = pixels.shape[:2]

        # 检测背景色（从边缘采样）
        edge_pixels = []
        for x in range(0, w, 10):
            edge_pixels.append(pixels[0, x])
            edge_pixels.append(pixels[h-1, x])
        for y in range(0, h, 10):
            edge_pixels.append(pixels[y, 0])
            edge_pixels.append(pixels[y, w-1])

        edge_arr = np.array(edge_pixels, dtype=float)
        bg_color = np.median(edge_arr, axis=0)

        # 计算距离
        dist = np.sqrt(np.sum((pixels.astype(float) - bg_color) ** 2, axis=2))

        # 保守阈值：只删除非常接近背景的像素
        threshold = 40
        alpha = np.zeros((h, w), dtype=np.uint8)
        alpha[dist >= threshold * 3] = 255  # 远离背景的像素完全保留
        edge_mask = (dist >= threshold) & (dist < threshold * 3)
        alpha[edge_mask] = ((dist[edge_mask] - threshold) / (threshold * 2) * 255).astype(np.uint8)

        result = np.dstack([pixels, alpha])
        result_img = Image.fromarray(result, 'RGBA')
        result_img = result_img.resize((256, 256), Image.LANCZOS)
        result_img.save(out_path)

    print(f"  Chroma-keyed {len(raw_files)} frames")
    return len(raw_files)

def main():
    print("=" * 50)
    print("Re-extracting HeiTan frames")
    print("=" * 50)

    for expr, video_name in VIDEOS.items():
        print(f"\n--- {expr} ---")
        count = extract_and_chroma_key(video_name, expr)
        print(f"  Done: {count} frames")

    # 检查缺失的表情
    missing = ["celebrate", "defeat", "idle", "happy"]
    for expr in missing:
        expr_dir = os.path.join(OUTPUT_DIR, expr)
        if not os.path.exists(expr_dir) or len([f for f in os.listdir(expr_dir) if f.endswith(".png")]) < 28:
            print(f"\n[MISSING] {expr} - no video available")

if __name__ == "__main__":
    main()
