"""
手动处理已完成的视频：抽帧 + 色键抠绿
"""
import os, subprocess, shutil
from PIL import Image

FFMPEG = r"C:\Users\zhn\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0-full_build\bin\ffmpeg.exe"
OUT_ROOT = r"H:\ComfyUI_windows_portable\ComfyUI\output"
DST_ROOT = r"e:\UnityProject\wuziqi\Assets\Art\Cat\Frames"

CATS = ["黑炭", "花斑", "银渐层", "玄猫", "仙喵长老"]
EXPRESSIONS = ["thinking", "smug", "worried", "celebrate", "defeat"]


def extract_frames(video_path, output_dir, target_fps=6):
    os.makedirs(output_dir, exist_ok=True)
    raw_dir = os.path.join(output_dir, "_raw")
    os.makedirs(raw_dir, exist_ok=True)
    subprocess.run([FFMPEG, "-y", "-i", video_path, "-vf", f"fps={target_fps}", os.path.join(raw_dir, "f_%04d.png")], capture_output=True)
    raw_files = sorted([f for f in os.listdir(raw_dir) if f.endswith(".png")])
    print(f"  Extracted {len(raw_files)} frames")

    for i, fname in enumerate(raw_files):
        img = Image.open(os.path.join(raw_dir, fname)).convert("RGBA")
        w, h = img.size
        px = img.load()
        out = Image.new("RGBA", (w, h), (0,0,0,0))
        po = out.load()
        for y in range(h):
            for x in range(w):
                r, g, b, a = px[x, y]
                dom = int(g) - max(int(r), int(b))
                if dom > 60:
                    po[x, y] = (r, g, b, 0)
                elif dom > 15:
                    alpha = int((dom - 15) * 255 / 45)
                    po[x, y] = (r, g, b, min(255, 255 - alpha))
                else:
                    if g > r + 12 and g > b + 12:
                        po[x, y] = (r, max(r, b), b, 255)
                    else:
                        po[x, y] = (r, g, b, 255)
        out = out.resize((256, 256), Image.LANCZOS)
        out.save(os.path.join(output_dir, f"f_{i:04d}.png"))
    return len(raw_files)


def main():
    for cat in CATS:
        print(f"\n【{cat}】")
        for exp in EXPRESSIONS:
            frame_dir = os.path.join(DST_ROOT, cat, exp)
            if os.path.exists(frame_dir) and len([f for f in os.listdir(frame_dir) if f.endswith(".png")]) >= 28:
                print(f"  [SKIP] {exp}")
                continue

            # 查找视频文件
            video_pattern = f"{cat}_{exp}"
            videos = [f for f in os.listdir(OUT_ROOT) if f.startswith(video_pattern) and f.endswith(".mp4")]
            if not videos:
                print(f"  [NO VIDEO] {exp}")
                continue

            video_path = os.path.join(OUT_ROOT, sorted(videos)[-1])  # 取最新的
            print(f"  [{exp}] Processing {os.path.basename(video_path)}...")

            count = extract_frames(video_path, frame_dir, target_fps=6)
            print(f"  [OK] {exp}: {count} frames")


if __name__ == "__main__":
    main()
