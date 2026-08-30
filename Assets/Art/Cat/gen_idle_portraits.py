"""
为5只猫生成待机立绘 (idle)，然后换绿幕
"""
import json, base64, urllib.request, os, time
from PIL import Image
import numpy as np

API_KEY = "ark-5ac4efb5-b854-4ddc-8637-d83e02688b6e-a80b1"
API_URL = "https://ark.cn-beijing.volces.com/api/plan/v3/images/generations"
MODEL = "doubao-seedream-5.0-lite"
OUTPUT_DIR = r"e:\UnityProject\wuziqi\Assets\Art\Cat"

# 待机pose描述
IDLE_POSE = "坐姿端正，眼睛微闭，神态安详，轻轻呼吸，放松自在的表情"

# 5只猫的特征描述
CATS = [
    {"name": "黑炭", "desc": "一只纯黑色的猫，毛发乌黑发亮，眼睛是明亮的黄色"},
    {"name": "花斑", "desc": "一只花斑猫，身上有棕色、黑色和白色的混合花纹"},
    {"name": "银渐层", "desc": "一只银渐层英短猫，毛发银灰色渐变，圆脸大眼睛"},
    {"name": "玄猫", "desc": "一只深黑色的猫，毛发浓密，眼睛深邃神秘"},
    {"name": "仙喵长老", "desc": "一只古老的神仙猫，白色长毛，有长长的眉毛和胡须，仙风道骨"},
]

PROMPT_TEMPLATE = (
    "传统中国水墨工笔画风格，淡彩设色，宣纸笔墨质感。"
    "{cat_desc}，戴着红色项圈挂着金色铃铛。"
    "{pose}。"
    "背景纯绿色幕布(#00FF00)，纯色无纹理无渐变无阴影。"
    "笔触写意墨色浓淡变化。古风手绘可爱风。"
    "猫占画面七成，居中构图。全身完整可见。无文字无其他物件。"
)


def generate_portrait(prompt, reference_path, save_path, seed=42):
    """生成单张立绘"""
    with open(reference_path, "rb") as f:
        ref_b64 = base64.b64encode(f.read()).decode()

    body = {
        "model": MODEL,
        "prompt": prompt,
        "image": f"data:image/png;base64,{ref_b64}",
        "size": "2k",
        "response_format": "b64_json",
        "watermark": False,
        "seed": seed,
    }
    headers = {"Content-Type": "application/json", "Authorization": f"Bearer {API_KEY}"}
    data = json.dumps(body).encode("utf-8")
    req = urllib.request.Request(API_URL, data=data, headers=headers)

    with urllib.request.urlopen(req, timeout=120) as resp:
        result = json.loads(resp.read())

    for img in result.get("data", []):
        if "b64_json" in img:
            raw = base64.b64decode(img["b64_json"])
            with open(save_path, "wb") as f:
                f.write(raw)
            return True
    return False


def replace_bg_with_green(input_path, output_path):
    """将纯色背景替换为绿幕"""
    img = Image.open(input_path).convert('RGB')
    pixels = np.array(img)
    h, w = pixels.shape[:2]

    # 从边缘采样检测背景色
    edge_pixels = []
    for x in range(0, w, 10):
        edge_pixels.append(pixels[0, x])
        edge_pixels.append(pixels[h-1, x])
    for y in range(0, h, 10):
        edge_pixels.append(pixels[y, 0])
        edge_pixels.append(pixels[y, w-1])

    from collections import Counter
    edge_arr = np.array(edge_pixels, dtype=float)
    quantized = (edge_arr / 10).astype(int) * 10
    bg_color = np.array(Counter(map(tuple, quantized.astype(int))).most_common(1)[0][0], dtype=float)

    # 计算距离并替换
    dist = np.sqrt(np.sum((pixels.astype(float) - bg_color) ** 2, axis=2))
    threshold = 40

    result = pixels.copy()
    result[dist < threshold] = [0, 255, 0]  # 绿幕

    # 边缘过渡
    edge_mask = (dist >= threshold) & (dist < threshold * 2)
    for c in range(3):
        result[edge_mask, c] = (
            pixels[edge_mask, c] * (1 - (dist[edge_mask] - threshold) / threshold) +
            0 * 255 * ((dist[edge_mask] - threshold) / threshold)
        ).astype(np.uint8)

    Image.fromarray(result).save(output_path)


def main():
    print("=" * 60)
    print("生成5只猫的待机立绘 (idle)")
    print("=" * 60)

    total = len(CATS)
    done = 0

    for cat in CATS:
        cat_dir = os.path.join(OUTPUT_DIR, cat["name"])
        os.makedirs(cat_dir, exist_ok=True)

        # 找参考图（用已有的任意立绘）
        ref_path = None
        for expr in ["celebrate", "smug", "thinking", "worried", "defeat"]:
            candidate = os.path.join(cat_dir, f"{expr}_green_first.png")
            if os.path.exists(candidate):
                ref_path = candidate
                break

        if not ref_path:
            print(f"\n[SKIP] {cat['name']}: 没有参考图")
            continue

        print(f"\n【{cat['name']}】")
        print(f"  参考图: {os.path.basename(ref_path)}")

        # 生成idle立绘
        raw_path = os.path.join(cat_dir, "idle_raw.png")
        green_path = os.path.join(cat_dir, "idle_green_first.png")

        prompt = PROMPT_TEMPLATE.format(cat_desc=cat["desc"], pose=IDLE_POSE)
        print(f"  生成中...")

        try:
            if generate_portrait(prompt, ref_path, raw_path, seed=42):
                print(f"  换绿幕...")
                replace_bg_with_green(raw_path, green_path)
                print(f"  [OK] idle_green_first.png")
                done += 1
            else:
                print(f"  [FAIL] 生成失败")
        except Exception as e:
            print(f"  [FAIL] {e}")

        time.sleep(1)

    print(f"\n{'=' * 60}")
    print(f"完成: {done}/{total}")
    print(f"{'=' * 60}")


if __name__ == "__main__":
    main()
