"""压缩帧图片 + 整理视频"""
import os, shutil
from PIL import Image

FRAMES_DIR = r"e:\UnityProject\wuziqi\Assets\Art\Cat\Frames"
VIDEOS_DIR = r"e:\UnityProject\wuziqi\Assets\Art\Cat\Videos"
VIDEO_SOURCE = r"H:\ComfyUI_windows_portable\ComfyUI\output"

os.makedirs(VIDEOS_DIR, exist_ok=True)

# 1. 压缩帧图片
print("=== 压缩帧图片 ===")
total_before = 0
total_after = 0

for cat in os.listdir(FRAMES_DIR):
    cat_dir = os.path.join(FRAMES_DIR, cat)
    if not os.path.isdir(cat_dir):
        continue

    for exp in os.listdir(cat_dir):
        exp_dir = os.path.join(cat_dir, exp)
        if not os.path.isdir(exp_dir):
            continue

        for fname in os.listdir(exp_dir):
            if not fname.startswith("f_") or not fname.endswith(".png"):
                continue

            fpath = os.path.join(exp_dir, fname)
            size_before = os.path.getsize(fpath)
            total_before += size_before

            img = Image.open(fpath)
            # 转为P模式（调色板）可以大幅压缩PNG
            if img.mode == "RGBA":
                # 保持RGBA但优化压缩
                img.save(fpath, optimize=True)
            else:
                img.save(fpath, optimize=True)

            size_after = os.path.getsize(fpath)
            total_after += size_after

print(f"压缩前: {total_before / 1024 / 1024:.1f} MB")
print(f"压缩后: {total_after / 1024 / 1024:.1f} MB")
print(f"节省: {(total_before - total_after) / 1024 / 1024:.1f} MB")

# 2. 整理视频
print("\n=== 整理视频 ===")
for fname in os.listdir(VIDEO_SOURCE):
    if not fname.endswith(".mp4"):
        continue
    # 只保留最新的视频（不带序号的）
    if "_00001_" in fname or "_00002_" in fname or "_00003_" in fname:
        continue

    src = os.path.join(VIDEO_SOURCE, fname)
    dst = os.path.join(VIDEOS_DIR, fname)
    if not os.path.exists(dst):
        shutil.copy2(src, dst)
        print(f"  复制: {fname}")

# 也复制带序号的最新视频（如果同名不带序号的不存在）
for fname in sorted(os.listdir(VIDEO_SOURCE), reverse=True):
    if not fname.endswith(".mp4"):
        continue

    # 提取基础名（去掉序号）
    base = fname
    for suffix in ["_00001_", "_00002_", "_00003_"]:
        if suffix in fname:
            base = fname.replace(suffix, "_")
            break

    dst = os.path.join(VIDEOS_DIR, base)
    if not os.path.exists(dst):
        src = os.path.join(VIDEO_SOURCE, fname)
        shutil.copy2(src, dst)
        print(f"  复制: {fname} -> {base}")

print(f"\n视频目录: {VIDEOS_DIR}")
print(f"视频数量: {len([f for f in os.listdir(VIDEOS_DIR) if f.endswith('.mp4')])}")
