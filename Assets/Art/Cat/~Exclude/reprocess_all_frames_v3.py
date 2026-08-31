# reprocess_all_frames_v3.py
# 保守版：只处理有原始视频帧的情况，避免重复抠图

import os
import sys
from PIL import Image
import numpy as np
from collections import Counter

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

    edge_arr = np.array(edge_pixels, dtype=float)
    quantized = (edge_arr / 10).astype(int) * 10
    color_counts = Counter(map(tuple, quantized.astype(int)))
    most_common = color_counts.most_common(1)[0][0]
    return np.array(most_common, dtype=float)

def chroma_key_frame(img, threshold=None):
    """智能抠图"""
    pixels = np.array(img.convert('RGB'))
    h, w = pixels.shape[:2]

    bg_color = detect_bg_color(pixels)
    dist = np.sqrt(np.sum((pixels.astype(float) - bg_color) ** 2, axis=2))

    # 自适应阈值
    bg_brightness = np.mean(bg_color)
    if threshold is None:
        if bg_brightness < 50:
            threshold = 25
        elif bg_brightness < 100:
            threshold = 35
        else:
            threshold = 50

    # 使用渐变alpha，但更保守
    alpha = np.zeros((h, w), dtype=np.uint8)
    alpha[dist < threshold] = 0
    alpha[dist >= threshold * 2.5] = 255  # 更大的渐变区间
    edge_mask = (dist >= threshold) & (dist < threshold * 2.5)
    alpha[edge_mask] = ((dist[edge_mask] - threshold) / (threshold * 1.5) * 255).astype(np.uint8)

    result = np.dstack([pixels, alpha])
    return Image.fromarray(result, 'RGBA')

def has_raw_frames(cat_dir, expression):
    """检查是否有原始帧（未抠图）"""
    raw_dir = os.path.join(cat_dir, expression, "_raw")
    if not os.path.exists(raw_dir):
        return False

    # 检查_raw目录是否有非RGBA的RGB帧
    for f in os.listdir(raw_dir):
        if f.endswith('.png') and not f.endswith('.meta'):
            path = os.path.join(raw_dir, f)
            try:
                img = Image.open(path)
                if img.mode == 'RGB':
                    return True  # 有RGB原始帧
                elif img.mode == 'RGBA':
                    # 检查是否是简单绿幕
                    pixels = np.array(img)
                    if pixels[:,:,3].mean() > 200:  # 大部分不透明，可能是原始帧带alpha
                        return True
            except:
                pass
    return False

def process_cat_expression(cat_dir, expression, frame_count=31):
    """处理单个猫的单个表情"""
    output_dir = os.path.join(cat_dir, expression)
    raw_dir = os.path.join(output_dir, "_raw")

    if not os.path.exists(raw_dir):
        return 0

    os.makedirs(output_dir, exist_ok=True)
    processed = 0

    for i in range(1, frame_count + 1):
        raw_path = os.path.join(raw_dir, f"f_{i:04d}.png")
        if not os.path.exists(raw_path):
            continue

        try:
            img = Image.open(raw_path)

            # 如果已经是RGBA且透明度合适，跳过
            if img.mode == 'RGBA':
                alpha = np.array(img)[:,:,3]
                if alpha.mean() < 200 and alpha.mean() > 50:
                    # 看起来已经抠图过了
                    continue

            result = chroma_key_frame(img, threshold=None)
            output_path = os.path.join(output_dir, f"f_{i:04d}.png")
            result = result.resize((256, 256), Image.LANCZOS)
            result.save(output_path)
            processed += 1
        except Exception as e:
            print(f"  Error processing frame {i}: {e}")

    return processed

def main():
    base_dir = os.path.dirname(os.path.abspath(__file__))
    frames_dir = os.path.join(base_dir, "Frames")

    cats = ["黑炭", "花斑", "银渐层", "玄猫", "仙喵长老", "橘座"]
    expressions = ["idle", "happy", "celebrate", "smug", "defeat", "thinking", "worried"]
    frame_count = 31

    total_processed = 0

    for cat in cats:
        cat_dir = os.path.join(frames_dir, cat)
        if not os.path.exists(cat_dir):
            print(f"\n[SKIP] {cat}: directory not found")
            continue

        print(f"\n{'='*50}")
        print(f"Processing {cat}")
        print('='*50)

        cat_processed = 0
        for expr in expressions:
            # 先检查是否有原始帧
            raw_dir = os.path.join(cat_dir, expr, "_raw")
            if not os.path.exists(raw_dir):
                print(f"  {expr}: no _raw directory, skipping")
                continue

            count = process_cat_expression(cat_dir, expr, frame_count)
            cat_processed += count
            if count > 0:
                print(f"  {expr}: {count} frames processed")
            else:
                print(f"  {expr}: already processed")

        total_processed += cat_processed
        print(f"  Total: {cat_processed} new frames")

    print(f"\n{'='*50}")
    print(f"ALL DONE: {total_processed} frames processed")
    print('='*50)

if __name__ == "__main__":
    main()
