"""
重新生成smug立绘，确保全身可见（加强版）
"""
import json, base64, urllib.request, os, time
from PIL import Image
import numpy as np

API_KEY = "ark-5ac4efb5-b854-4ddc-8637-d83e02688b6e-a80b1"
API_URL = "https://ark.cn-beijing.volces.com/api/plan/v3/images/generations"
MODEL = "doubao-seedream-5.0-lite"
OUTPUT_DIR = r"e:\UnityProject\wuziqi\Assets\Art\Cat"

CATS = [
    {"name": "黑炭", "desc": "一只纯黑色的猫，毛发乌黑发亮，眼睛是明亮的黄色"},
    {"name": "花斑", "desc": "一只花斑猫，身上有棕色、黑色和白色的混合花纹"},
    {"name": "银渐层", "desc": "一只银渐层英短猫，毛发银灰色渐变，圆脸大眼睛"},
    {"name": "玄猫", "desc": "一只深黑色的猫，毛发浓密，眼睛深邃神秘"},
    {"name": "仙喵长老", "desc": "一只古老的神仙猫，白色长毛，有长长的眉毛和胡须，仙风道骨"},
]

SMUG_POSE = "坐姿，一只爪子叉腰，另一只爪子捂嘴偷笑，眼睛眯成月牙形，得意洋洋"

PROMPT_TEMPLATE = (
    "Traditional Chinese ink wash painting, light color, rice paper texture. "
    "{cat_desc}, wearing red collar with golden bell. {pose}. "
    "FULL BODY VIEW: entire cat visible from head to tail tip, all four paws clearly shown. "
    "Cat is SMALL, occupying only 30% of the frame area. "
    "Large empty green space around the cat. "
    "SOLID BRIGHT GREEN #00FF00 chroma key background, flat uniform color. "
    "No text, no other objects. Cute ancient Chinese style."
)


def generate_portrait(prompt, reference_path, save_path, seed=42):
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
    img = Image.open(input_path).convert('RGB')
    pixels = np.array(img)
    h, w = pixels.shape[:2]

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

    dist = np.sqrt(np.sum((pixels.astype(float) - bg_color) ** 2, axis=2))
    threshold = 40

    result = pixels.copy()
    result[dist < threshold] = [0, 255, 0]

    Image.fromarray(result).save(output_path)


def main():
    print("=" * 60)
    print("重新生成smug立绘（全身加强版）")
    print("=" * 60)

    for cat in CATS:
        cat_dir = os.path.join(OUTPUT_DIR, cat["name"])
        ref_path = os.path.join(cat_dir, "idle_green_first.png")
        if not os.path.exists(ref_path):
            ref_path = os.path.join(cat_dir, "celebrate_green_first.png")
        if not os.path.exists(ref_path):
            print(f"[SKIP] {cat['name']}: 没有参考图")
            continue

        print(f"\n【{cat['name']}】")

        raw_path = os.path.join(cat_dir, "smug_raw.png")
        green_path = os.path.join(cat_dir, "smug_green_first.png")

        prompt = PROMPT_TEMPLATE.format(cat_desc=cat["desc"], pose=SMUG_POSE)
        print(f"  生成中...")

        try:
            if generate_portrait(prompt, ref_path, raw_path, seed=42):
                print(f"  换绿幕...")
                replace_bg_with_green(raw_path, green_path)
                print(f"  [OK] smug_green_first.png")
            else:
                print(f"  [FAIL]")
        except Exception as e:
            print(f"  [FAIL] {e}")

        time.sleep(1)

    print(f"\n{'=' * 60}")
    print("完成!")
    print(f"{'=' * 60}")


if __name__ == "__main__":
    main()
