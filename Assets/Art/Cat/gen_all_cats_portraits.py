"""
批量生成5只猫的5种表情立绘 (Seedream 5.0 lite)
猫: 黑炭、花斑、银渐层、玄猫、仙喵长老
表情: thinking, smug, worried, celebrate, defeat
"""
import json, base64, urllib.request, os, sys, time

API_KEY = "ark-5ac4efb5-b854-4ddc-8637-d83e02688b6e-a80b1"
API_URL = "https://ark.cn-beijing.volces.com/api/plan/v3/images/generations"
MODEL = "doubao-seedream-5.0-lite"
OUTPUT_DIR = r"e:\UnityProject\wuziqi\Assets\Art\Cat"

EXPRESSIONS = {
    "thinking": "坐姿端正，一只爪子托着下巴，眼睛微微眯起，若有所思的表情",
    "smug": "坐姿，一只爪子叉腰，另一只爪子捂嘴偷笑，眼睛眯成月牙形，得意洋洋的表情",
    "worried": "缩着脖子，毛炸起来，眼睛睁大，耳朵向后压，紧张不安的表情",
    "celebrate": "两只前爪高举过头顶，张大嘴巴，眼睛眯成弯月，开心欢呼跳跃的表情",
    "defeat": "趴伏在地上，两只前爪摊开，眼睛半闭，嘴角下撇，沮丧无奈的表情",
}

# 5只猫的特征描述
CATS = [
    {
        "name": "黑炭",
        "portrait": "Portrait_黑炭.png",
        "desc": "一只纯黑色的猫，毛发乌黑发亮，眼睛是明亮的黄色",
    },
    {
        "name": "花斑",
        "portrait": "Portrait_花斑.png",
        "desc": "一只花斑猫，身上有棕色、黑色和白色的混合花纹",
    },
    {
        "name": "银渐层",
        "portrait": "Portrait_银渐层.png",
        "desc": "一只银渐层英短猫，毛发银灰色渐变，圆脸大眼睛",
    },
    {
        "name": "玄猫",
        "portrait": "Portrait_玄猫.png",
        "desc": "一只深黑色的猫，毛发浓密，眼睛深邃神秘",
    },
    {
        "name": "仙喵长老",
        "portrait": "Portrait_仙喵长老.png",
        "desc": "一只古老的神仙猫，白色长毛，有长长的眉毛和胡须，仙风道骨",
    },
]

PROMPT_TEMPLATE = (
    "传统中国水墨工笔画风格，淡彩设色，宣纸笔墨质感。"
    "{cat_desc}，戴着红色项圈挂着金色铃铛。"
    "{pose}。"
    "背景纯绿色幕布(#00FF00)，纯色无纹理无渐变无阴影。"
    "笔触写意墨色浓淡变化。古风手绘可爱风。"
    "猫占画面七成，居中构图。全身完整可见。无文字无其他物件。"
)


def generate(prompt, reference_path, save_path, seed=42):
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
            print(f"  OK {os.path.basename(save_path)} ({len(raw)//1024}KB)")
            return True
    return False


def main():
    print("=" * 60)
    print("批量生成5只猫的表情立绘")
    print(f"ComfyUI: {API_URL}")
    print(f"输出目录: {OUTPUT_DIR}")
    print("=" * 60)

    total = len(CATS) * len(EXPRESSIONS)
    done = 0

    for cat in CATS:
        ref_path = os.path.join(OUTPUT_DIR, "Portraits", cat["portrait"])
        if not os.path.exists(ref_path):
            print(f"[SKIP] 参考图不存在: {ref_path}")
            continue

        print(f"\n【{cat['name']}】开始生成...")

        for expr_name, pose in EXPRESSIONS.items():
            save_name = f"{cat['name']}_{expr_name}_seedream.png"
            save_path = os.path.join(OUTPUT_DIR, save_name)

            if os.path.exists(save_path):
                print(f"  [SKIP] {save_name} 已存在")
                done += 1
                continue

            prompt = PROMPT_TEMPLATE.format(cat_desc=cat["desc"], pose=pose)
            print(f"  [{expr_name}] Generating...")

            try:
                if generate(prompt, ref_path, save_path, seed=42):
                    done += 1
                time.sleep(1)
            except Exception as e:
                print(f"  [FAIL] {expr_name}: {e}")

    print(f"\n{'=' * 60}")
    print(f"完成: {done}/{total}")
    print(f"立绘保存在: {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
