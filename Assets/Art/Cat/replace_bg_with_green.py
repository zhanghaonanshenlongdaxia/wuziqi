# -*- coding: utf-8 -*-
"""批量将立绘纯色/接近纯色背景替换为绿幕 #00FF00
通过边缘采样确定背景颜色，然后替换相似颜色区域"""

import os
import glob
from PIL import Image
import numpy as np

PORTRAITS_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'Portraits')
GREEN_SCREEN = np.array([0, 255, 0], dtype=np.uint8)
COLOR_DIST_THRESHOLD = 65

def color_distance(c1, c2):
    return np.sqrt(np.sum((c1.astype(float) - c2.astype(float)) ** 2, axis=-1))

def replace_bg_with_green(img_path, out_path):
    """Replace solid background with green screen by sampling edges."""
    img = Image.open(img_path).convert('RGB')
    pixels = np.array(img)
    h, w = pixels.shape[:2]

    # 采样所有边缘像素来确定背景颜色
    edge_pixels = []
    # 上边缘
    edge_pixels.extend([tuple(pixels[0, x]) for x in range(0, w, 5)])
    # 下边缘
    edge_pixels.extend([tuple(pixels[h-1, x]) for x in range(0, w, 5)])
    # 左边缘
    edge_pixels.extend([tuple(pixels[y, 0]) for y in range(0, h, 5)])
    # 右边缘
    edge_pixels.extend([tuple(pixels[y, w-1]) for y in range(0, h, 5)])

    # 过滤掉已经是绿色的像素
    non_green_edges = [p for p in edge_pixels if not (p[0] < 30 and p[1] > 200 and p[2] < 30)]

    if not non_green_edges:
        print(f'  Already green screen, skipping')
        return 0

    # 计算主要背景颜色（用中位数更稳定）
    bg_color = np.median(non_green_edges, axis=0).astype(float)
    print(f'  Detected bg color: {bg_color.astype(int)}')

    # 计算每个像素到背景色的距离
    dist = color_distance(pixels, bg_color)

    # 距离小于阈值的替换为绿幕
    bg_mask = dist < COLOR_DIST_THRESHOLD

    result = pixels.copy()
    result[bg_mask] = GREEN_SCREEN

    result_img = Image.fromarray(result)
    result_img.save(out_path)
    return bg_mask.sum()

def main():
    # 处理两种类型的文件
    patterns = [
        os.path.join(PORTRAITS_DIR, 'Portrait_*.png'),
        os.path.join(os.path.dirname(PORTRAITS_DIR), '*_seedream.png'),
    ]

    all_files = []
    for pattern in patterns:
        all_files.extend(glob.glob(pattern))

    print(f'Found {len(all_files)} portrait files\n')

    for p_path in sorted(all_files):
        fname = os.path.basename(p_path)
        print(f'Processing {fname}...')

        bg_pixels = replace_bg_with_green(p_path, p_path)
        print(f'  Replaced {bg_pixels} pixels')

    print(f'\nDone!')

if __name__ == '__main__':
    main()
