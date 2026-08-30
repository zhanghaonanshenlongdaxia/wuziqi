"""
橘座剩余4个表情立绘生成脚本 (Seedream 5.0 lite)
用大头照做参考图保持画风一致
"""
import json, base64, urllib.request, os, sys, time

API_KEY = "ark-5ac4efb5-b854-4ddc-8637-d83e02688b6e-a80b1"
API_URL = "https://ark.cn-beijing.volces.com/api/plan/v3/images/generations"
MODEL = "doubao-seedream-5.0-lite"
REFERENCE = r"e:\UnityProject\wuziqi\Assets\Art\Cat\Portraits\Portrait_橘座.png"
OUTPUT_DIR = r"e:\UnityProject\wuziqi\Assets\Art\Cat"

EXPRESSIONS = {
    "smug": "坐姿，一只爪子叉腰，另一只爪子捂嘴偷笑，眼睛眯成月牙形，得意洋洋的表情，尾巴翘起来",
    "worried": "缩着脖子，毛炸起来，眼睛睁大，耳朵向后压，紧张不安的表情，身体微微发抖",
    "celebrate": "两只前爪高举过头顶，张大嘴巴，眼睛眯成弯月，开心欢呼跳跃的表情，尾巴高高翘起",
    "defeat": "趴伏在地上，两只前爪摊开，眼睛半闭，嘴角下撇，沮丧无奈的表情，尾巴无力地搭在地上",
}

PROMPT_TEMPLATE = (
    "传统中国水墨工笔画风格，淡彩设色，宣纸笔墨质感。"
    "一只胖嘟嘟的橘色虎斑猫，戴着红色项圈挂着金色铃铛。"
    "{pose}。"
    "橘色和奶油白色相间的虎斑花纹，白肚皮。"
    "背景纯绿色幕布(#00FF00)，纯色无纹理无渐变无阴影。"
    "笔触写意墨色浓淡变化。古风手绘可爱风。"
    "猫占画面七成，居中构图。全身完整可见。无文字无其他物件。"
)

def generate(prompt, save_path, seed=42):
    with open(REFERENCE, "rb") as f:
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
            print(f"  OK {os.path.basename(save_path)} ({len(raw)//1024}KB)")
            return True
    return False


if __name__ == "__main__":
    print("=== Juzuo expression batch generation (Seedream 5.0 lite) ===\n")
    for name, pose in EXPRESSIONS.items():
        prompt = PROMPT_TEMPLATE.format(pose=pose)
        save_to = os.path.join(OUTPUT_DIR, f"juzuo_{name}_seedream.png")
        print(f"[{name}] Generating...")
        try:
            generate(prompt, save_to, seed=42)
            time.sleep(1)
        except Exception as e:
            print(f"  FAIL Error: {e}")
    print("\n=== All Done ===")
