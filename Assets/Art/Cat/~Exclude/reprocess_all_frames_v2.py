# reprocess_all_frames_v2.py
# 优化版：智能背景检测 + 自适应阈值

import os
import sys
import shutil
from PIL import Image
import numpy as np

def detect_bg_color(pixels, sample_step=10):
    """从边缘采样检测背景色，使用模式而非中位数"""
    h, w = pixels.shape[:2]
    edge_pixels = []

    # 采样边缘像素
    for x in range(0, w, sample_step):
        edge_pixels.append(tuple(pixels[0, x]))      # top
        edge_pixels.append(tuple(pixels[h-1, x]))     # bottom
    for y in range(0, h, sample_step):
        edge_pixels.append(tuple(pixels[y, 0]))       # left
        edge_pixels.append(tuple(pixels[y, w-1]))     # right

    edge_arr = np.array(edge_pixels, dtype=float)

    # 使用聚类找到主要的背景颜色（通常占边缘的多数）
    from collections import Counter
    # 量化颜色减少噪声
    quantized = (edge_arr / 10).astype(int) * 10
    color_counts = Counter(map(tuple, quantized.astype(int)))

    # 取最常出现的颜色作为背景
    most_common = color_counts.most_common(1)[0][0]
    bg_color = np.array(most_common, dtype=float)

    return bg_color

def chroma_key_frame(img, threshold=None):
    """
    智能抠图：
    1. 检测背景色
    2. 如果背景是深色，使用更小的阈值
    3. 如果背景是亮色，使用标准阈值
    """
    pixels = np.array(img.convert('RGB'))
    h, w = pixels.shape[:2]

    bg_color = detect_bg_color(pixels)
    dist = np.sqrt(np.sum((pixels.astype(float) - bg_color) ** 2, axis=2))

    # 根据背景亮度自适应阈值
    bg_brightness = np.mean(bg_color)
    if threshold is None:
        if bg_brightness < 50:  # 深色背景
            threshold = 25  # 更小的阈值，避免误抠
        elif bg_brightness < 100:
            threshold = 35
        else:  # 亮色背景
            threshold = 50

    alpha = np.zeros((h, w), dtype=np.uint8)
    alpha[dist < threshold] = 0
    alpha[dist >= threshold * 2] = 255
    edge_mask = (dist >= threshold) & (dist < threshold * 2)
    alpha[edge_mask] = ((dist[edge_mask] - threshold) / threshold * 255).astype(np.uint8)

    result = np.dstack([pixels, alpha])
    return Image.fromarray(result, 'RGBA')

def get_raw_frame(cat_dir, expression, frame_idx):
    """获取原始帧（从_raw子目录或frames目录）"""
    # 先找_raw目录
    raw_dir = os.path.join(cat_dir, expression, "_raw")
    raw_path = os.path.join(raw_dir, f"f_{frame_idx:04d}.png")
    if os.path.exists(raw_path):
        return raw_path

    # 再找frames目录中的原始帧（非抠图版本）
    frames_dir = os.path.join(cat_dir, expression)
    frames_path = os.path.join(frames_dir, f"f_{frame_idx:04d}.png")
    if os.path.exists(frames_path):
        return frames_path

    return None

def process_cat_expression(cat_dir, expression, frame_count=31):
    """处理单个猫的单个表情"""
    output_dir = os.path.join(cat_dir, expression)
    os.makedirs(output_dir, exist_ok=True)

    processed = 0
    for i in range(1, frame_count + 1):
        raw_path = get_raw_frame(cat_dir, expression, i)
        if raw_path is None:
            continue

        try:
            img = Image.open(raw_path)
            result = chroma_key_frame(img)
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
            count = process_cat_expression(cat_dir, expr, frame_count)
            cat_processed += count
            print(f"  {expr}: {count} frames")

        total_processed += cat_processed
        print(f"  Total: {cat_processed} frames")

    print(f"\n{'='*50}")
    print(f"ALL DONE: {total_processed} frames processed")
    print('='*50)

if __name__ == "__main__":
    main()
