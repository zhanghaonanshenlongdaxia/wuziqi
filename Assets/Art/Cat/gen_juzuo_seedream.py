import json, base64, urllib.request, os, sys, time

API_KEY = "ark-5ac4efb5-b854-4ddc-8637-d83e02688b6e-a80b1"
API_URL = "https://ark.cn-beijing.volces.com/api/plan/v3/images/generations"

def generate_image(prompt, save_path, reference_image=None, seed=42):
    """调用火山引擎 Seedream 5.0 pro 生成图片"""
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {API_KEY}"
    }

    body = {
        "model": "doubao-seedream-5.0-lite",
        "prompt": prompt,
        "size": "2k",
        "response_format": "b64_json",
        "watermark": False,
        "seed": seed
    }

    # 如果有参考图，走图生图
    if reference_image:
        with open(reference_image, "rb") as f:
            img_b64 = base64.b64encode(f.read()).decode()
        ext = os.path.splitext(reference_image)[1].lower()
        mime = {"png": "image/png", "jpg": "image/jpeg", "jpeg": "image/jpeg"}.get(ext, "image/png")
        body["image"] = f"data:{mime};base64,{img_b64}"

    data = json.dumps(body).encode("utf-8")
    req = urllib.request.Request(API_URL, data=data, headers=headers)

    print(f"Calling Seedream API...")
    print(f"Prompt: {prompt[:80]}...")

    try:
        with urllib.request.urlopen(req, timeout=120) as resp:
            result = json.loads(resp.read())
    except urllib.error.HTTPError as e:
        error_body = e.read().decode()
        print(f"API Error {e.code}: {error_body}")
        sys.exit(1)

    if "error" in result:
        print(f"Error: {result['error']}")
        sys.exit(1)

    # 解码并保存图片
    for i, img_data in enumerate(result.get("data", [])):
        if "b64_json" in img_data:
            img_bytes = base64.b64decode(img_data["b64_json"])
            save_to = save_path if i == 0 else save_path.replace(".png", f"_{i}.png")
            with open(save_to, "wb") as f:
                f.write(img_bytes)
            print(f"Saved: {save_to} ({len(img_bytes)//1024}KB)")
            return save_to

    print("No image data in response")
    print(json.dumps(result, indent=2, ensure_ascii=False)[:500])
    sys.exit(1)


# ============================================
# 橘座 thinking 表情 - 用橘座大头照做参考保持画风
# ============================================

REFERENCE = r"e:\UnityProject\wuziqi\Assets\Art\Cat\Portraits\Portrait_橘座.png"

PROMPT = (
    "传统中国水墨工笔画风格，淡彩设色，宣纸笔墨质感。"
    "一只胖嘟嘟的橘色虎斑猫，戴着红色项圈挂着金色铃铛。"
    "坐姿，一只爪子抬到下巴处做出思考的动作，眼睛向上看，表情疑惑好奇。"
    "橘色和奶油白色相间的虎斑花纹，白肚皮。"
    "背景纯绿色幕布(#00FF00)，纯色无纹理无渐变无阴影。"
    "笔触写意墨色浓淡变化。古风手绘可爱风。"
    "猫占画面七成，居中构图。无文字无其他物件。"
)

SAVE_PATH = r"e:\UnityProject\wuziqi\Assets\Art\Cat\juzuo_thinking_seedream.png"

result = generate_image(PROMPT, SAVE_PATH, reference_image=REFERENCE)
print(f"\nDone! File: {result}")
