# -*- coding: utf-8 -*-
"""批量重新抠图：处理所有已恢复的帧"""

import os
from PIL import Image
import numpy as np
import glob

FRAMES_DIR = r"e:\UnityProject\wuziqi\Assets\Art\Cat\Frames"

CATS = ["黑炭", "花斑", "银渐层", "玄猫", "仙喵长老"]
EXPRESSIONS = ["thinking", "smug", "worried", "celebrate", "defeat"]


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

    return np.median(edge_pixels, axis=0).astype(float)


def chroma_key_frame(img, threshold=60):
    """对单帧进行抠图，返回RGBA图像"""
    pixels = np.array(img.convert('RGB'))
    h, w = pixels.shape[:2]

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
    return Image.fromarray(result, 'RGBA')


def reprocess_frames(frame_dir):
    """重新处理目录中的所有帧"""
    raw_dir = os.path.join(frame_dir, "_raw")

    # 如果有_raw目录，从那里读取原始帧
    if os.path.exists(raw_dir):
        source_dir = raw_dir
    else:
        source_dir = frame_dir

    frames = sorted(glob.glob(os.path.join(source_dir, "f_*.png")))
    if not frames:
        return 0

    processed = 0
    for frame_path in frames:
        fname = os.path.basename(frame_path)
        img = Image.open(frame_path)
        result = chroma_key_frame(img, threshold=60)

        # 保存为256x256
        result = result.resize((256, 256), Image.LANCZOS)
        result.save(os.path.join(frame_dir, fname))
        processed += 1

    return processed


def main():
    total_processed = 0

    for cat in CATS:
        print(f"\n[{cat}]")
        for expr in EXPRESSIONS:
            frame_dir = os.path.join(FRAMES_DIR, cat, expr)

            if not os.path.exists(frame_dir):
                continue

            # 检查是否有_raw目录（原始未抠图的帧）
            raw_dir = os.path.join(frame_dir, "_raw")
            if not os.path.exists(raw_dir):
                # 没有_raw，说明当前帧可能已经是抠图后的
                # 需要先检查帧是否是RGBA
                sample = glob.glob(os.path.join(frame_dir, "f_*.png"))
                if sample:
                    img = Image.open(sample[0])
                    if img.mode == 'RGBA':
                        print(f"  [SKIP] {expr} (already RGBA)")
                        continue

            count = reprocess_frames(frame_dir)
            print(f"  [OK] {expr}: {count} frames")
            total_processed += count

    print(f"\nTotal processed: {total_processed}")


if __name__ == "__main__":
    main()
