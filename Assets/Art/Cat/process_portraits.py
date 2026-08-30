"""批量处理立绘：备份 + 换对比色背景"""
import os, shutil
from PIL import Image
from rembg import remove, new_session

os.environ['OMP_NUM_THREADS'] = '4'
session = new_session('u2net')

CAT_ROOT = r"e:\UnityProject\wuziqi\Assets\Art\Cat"

# 背景色映射
BG_COLORS = {
    "黑炭": (255, 255, 255),  # 白
    "玄猫": (255, 255, 255),  # 白
    "花斑": (0, 0, 0),        # 黑
    "银渐层": (0, 0, 0),      # 黑
    "仙喵长老": (0, 0, 0),    # 黑
}

cats = ["黑炭", "花斑", "银渐层", "玄猫", "仙喵长老"]

for cat in cats:
    cat_dir = os.path.join(CAT_ROOT, cat)
    if not os.path.exists(cat_dir):
        print(f"Skip {cat} - dir not found")
        continue

    # 创建备份目录
    backup_dir = os.path.join(cat_dir, "_backup")
    os.makedirs(backup_dir, exist_ok=True)

    bg_rgb = BG_COLORS.get(cat, (0, 0, 0))
    print(f"\n=== {cat} (bg: {bg_rgb}) ===")

    for fname in os.listdir(cat_dir):
        if not fname.endswith("_seedream.png") or fname.endswith("_contrast.png"):
            continue
        if ".meta" in fname:
            continue

        src = os.path.join(cat_dir, fname)
        backup = os.path.join(backup_dir, fname)

        # 备份
        if not os.path.exists(backup):
            shutil.copy2(src, backup)
            print(f"  Backup: {fname}")

        # 换背景
        img = Image.open(src).convert("RGBA")
        img_no_bg = remove(img, session=session)

        bg = Image.new("RGBA", img_no_bg.size, (*bg_rgb, 255))
        bg.paste(img_no_bg, (0, 0), img_no_bg)

        out_name = fname.replace("_seedream.png", "_contrast.png")
        out_path = os.path.join(cat_dir, out_name)
        bg.save(out_path)
        print(f"  Created: {out_name}")

print("\nDone!")
