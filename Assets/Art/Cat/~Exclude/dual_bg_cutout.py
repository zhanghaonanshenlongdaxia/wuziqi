"""
双背景抠图：结合绿幕 + 对比色背景，取两者优点
- 绿幕：边缘检测精准
- 对比色：毛发细节保留好
用法: python dual_bg_cutout.py <猫名> [表情]
"""
import os, sys, shutil
from PIL import Image
import numpy as np
from rembg import remove, new_session

os.environ['OMP_NUM_THREADS'] = '4'
rembg_session = new_session('u2net')

CAT_ROOT = r"e:\UnityProject\wuziqi\Assets\Art\Cat"

# 背景色
GREEN = np.array([0, 255, 0])
CONTRAST_COLORS = {
    "黑炭": np.array([255, 255, 255]),
    "玄猫": np.array([255, 255, 255]),
    "花斑": np.array([0, 0, 0]),
    "银渐层": np.array([0, 0, 0]),
    "仙喵长老": np.array([0, 0, 0]),
}


def green_screen_cutout(img_array, bg_color, threshold=60):
    """绿幕抠图：基于颜色距离"""
    rgb = img_array[:, :, :3].astype(float)
    diff = np.sqrt(np.sum((rgb - bg_color) ** 2, axis=2))
    alpha = np.where(diff < threshold, 0, 255).astype(np.uint8)
    # 边缘渐变
    edge_mask = (diff < threshold * 2) & (diff >= threshold)
    alpha[edge_mask] = ((diff[edge_mask] - threshold) / threshold * 255).astype(np.uint8)
    return alpha


def dual_bg_cutout(green_path, contrast_path, output_path):
    """双背景抠图：结合两种方法的优点"""
    green_img = np.array(Image.open(green_path).convert("RGBA"))
    contrast_img = np.array(Image.open(contrast_path).convert("RGBA"))

    # 方法1：绿幕抠图（基于绿色检测）
    alpha_green = green_screen_cutout(green_img, GREEN)

    # 方法2：rembg抠图（基于AI模型）
    pil_img = Image.open(contrast_path).convert("RGBA")
    rembg_result = remove(pil_img, session=rembg_session)
    alpha_rembg = np.array(rembg_result)[:, :, 3]

    # 结合策略：取两者中更"确定"的部分
    # - 绿幕对纯绿背景边缘精准
    # - rembg对复杂细节（毛发）更好
    combined_alpha = np.maximum(alpha_green, alpha_rembg)

    # 使用绿幕版本的颜色（更准确）
    result = green_img.copy()
    result[:, :, 3] = combined_alpha

    Image.fromarray(result).save(output_path)
    print(f"  Dual cutout: {os.path.basename(output_path)}")


def process_cat(cat_name, expressions=None):
    """处理一只猫的所有表情"""
    cat_dir = os.path.join(CAT_ROOT, cat_name)
    if not os.path.exists(cat_dir):
        print(f"Dir not found: {cat_dir}")
        return

    if not expressions:
        expressions = ["celebrate", "defeat", "idle", "smug", "thinking", "worried"]

    print(f"\n=== {cat_name} ===")

    for exp in expressions:
        green_file = f"{cat_name}_{exp}_seedream.png"
        contrast_file = f"{cat_name}_{exp}_contrast.png"
        output_file = f"{cat_name}_{exp}_dual.png"

        green_path = os.path.join(cat_dir, green_file)
        contrast_path = os.path.join(cat_dir, contrast_file)
        output_path = os.path.join(cat_dir, output_file)

        if not os.path.exists(green_path):
            print(f"  Skip {exp}: no seedream file")
            continue
        if not os.path.exists(contrast_path):
            print(f"  Skip {exp}: no contrast file")
            continue

        dual_bg_cutout(green_path, contrast_path, output_path)


if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python dual_bg_cutout.py <猫名> [表情1,表情2,...]")
        print("Example: python dual_bg_cutout.py 黑炭 idle")
        sys.exit(1)

    cat_name = sys.argv[1]
    expressions = sys.argv[2].split(",") if len(sys.argv) > 2 else None

    process_cat(cat_name, expressions)
