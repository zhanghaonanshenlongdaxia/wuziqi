"""
用 ComfyUI API 批量生成猫猫立绘
基于 Qwen-Image-2512 + Lightning 4steps LoRA
"""
import json
import urllib.request
import urllib.parse
import time
import os
import sys
import uuid

COMFYUI_URL = "http://127.0.0.1:8188"
OUTPUT_DIR = os.path.dirname(os.path.abspath(__file__))

# 通用风格描述（参考小白 CatBase.png）
STYLE = (
    "a cute fluffy cat character, full body portrait, sitting pose with one paw raised, "
    "soft watercolor ink wash painting style, semi-realistic with cute proportions, "
    "red collar with golden bell, green solid color background, "
    "game character art, high quality, detailed fur texture, warm lighting"
)

# 负面提示词
NEGATIVE = (
    "realistic, photorealistic, 3d render, ugly, deformed, noisy, blurry, low quality, "
    "worst quality, extra limbs, bad anatomy, text, watermark, signature, dark background"
)

# 6只猫的提示词（小白已有，跳过）
CATS = [
    {
        "name": "Cat_2_橘座",
        "prompt": "a chubby orange tabby cat, confident smug expression, half-closed eyes looking proud, orange and cream white fur, slightly overweight body, regal sitting pose, personality: proud and lazy"
    },
    {
        "name": "Cat_3_黑炭",
        "prompt": "a sleek pure black cat, piercing golden eyes, mysterious aloof expression, elegant sitting pose, shiny black fur with subtle highlights, personality: cool and mysterious"
    },
    {
        "name": "Cat_4_花斑",
        "prompt": "a playful calico cat with white base fur and orange-black patches, bright curious eyes, mischievous grin, energetic dynamic pose, personality: playful and clever"
    },
    {
        "name": "Cat_5_银渐层",
        "prompt": "an elegant silver shaded British shorthair cat, plush silver-gray fur, stunning green eyes, refined aristocratic sitting pose, gentle sophisticated expression, personality: elegant and reserved"
    },
    {
        "name": "Cat_6_玄猫",
        "prompt": "a mystical dark black cat with subtle dark stripe patterns, striking red eyes, ancient Chinese mystical elements, wise otherworldly expression, personality: mystical and wise"
    },
    {
        "name": "Cat_7_仙喵长老",
        "prompt": "a wise ancient white long-haired cat with flowing fur, wearing a tiny traditional Chinese bamboo hat douli, long white whiskers like a sage, kind knowing eyes, elder master aura, personality: wise and ancient"
    },
]


def build_workflow(prompt_text, seed, filename_prefix):
    """构建 ComfyUI API 工作流 JSON"""
    return {
        "1": {
            "class_type": "UnetLoaderGGUF",
            "inputs": {
                "unet_name": "qwen-image-2512-Q4_K_M.gguf"
            }
        },
        "2": {
            "class_type": "CLIPLoader",
            "inputs": {
                "clip_name": "qwen_4b_ace15.safetensors",
                "type": "qwen_image"
            }
        },
        "3": {
            "class_type": "VAELoader",
            "inputs": {
                "vae_name": "qwen_image_vae.safetensors"
            }
        },
        "4": {
            "class_type": "LoraLoader",
            "inputs": {
                "model": ["1", 0],
                "clip": ["2", 0],
                "lora_name": "Qwen-Image-2512-Lightning-4steps-V1.0-bf16.safetensors",
                "strength_model": 1.0,
                "strength_clip": 1.0
            }
        },
        "5": {
            "class_type": "CLIPTextEncode",
            "inputs": {
                "clip": ["4", 1],
                "text": f"{prompt_text}, {STYLE}"
            }
        },
        "6": {
            "class_type": "CLIPTextEncode",
            "inputs": {
                "clip": ["4", 1],
                "text": NEGATIVE
            }
        },
        "7": {
            "class_type": "EmptyLatentImage",
            "inputs": {
                "width": 1024,
                "height": 1024,
                "batch_size": 1
            }
        },
        "8": {
            "class_type": "KSampler",
            "inputs": {
                "model": ["4", 0],
                "seed": seed,
                "steps": 4,
                "cfg": 1.0,
                "sampler_name": "euler",
                "scheduler": "simple",
                "positive": ["5", 0],
                "negative": ["6", 0],
                "latent_image": ["7", 0],
                "denoise": 1.0
            }
        },
        "9": {
            "class_type": "VAEDecode",
            "inputs": {
                "samples": ["8", 0],
                "vae": ["3", 0]
            }
        },
        "10": {
            "class_type": "SaveImage",
            "inputs": {
                "images": ["9", 0],
                "filename_prefix": filename_prefix
            }
        }
    }


def queue_prompt(workflow):
    """提交工作流到 ComfyUI"""
    data = json.dumps({"prompt": workflow}).encode("utf-8")
    req = urllib.request.Request(
        f"{COMFYUI_URL}/prompt",
        data=data,
        headers={"Content-Type": "application/json"}
    )
    try:
        with urllib.request.urlopen(req) as resp:
            return json.loads(resp.read())
    except urllib.error.HTTPError as e:
        error_body = e.read().decode("utf-8")
        print(f"  [ERROR] ComfyUI API error {e.code}: {error_body}")
        raise


def wait_for_completion(prompt_id, timeout=300):
    """等待生成完成"""
    start = time.time()
    while time.time() - start < timeout:
        try:
            req = urllib.request.Request(f"{COMFYUI_URL}/history/{prompt_id}")
            with urllib.request.urlopen(req) as resp:
                history = json.loads(resp.read())
                if prompt_id in history:
                    outputs = history[prompt_id].get("outputs", {})
                    for node_id, node_output in outputs.items():
                        if "images" in node_output:
                            return node_output["images"]
                    return None
        except Exception:
            pass
        time.sleep(2)
    return None


def download_image(filename, subfolder, img_type):
    """下载生成的图片"""
    params = urllib.parse.urlencode({
        "filename": filename,
        "subfolder": subfolder,
        "type": img_type
    })
    req = urllib.request.Request(f"{COMFYUI_URL}/view?{params}")
    with urllib.request.urlopen(req) as resp:
        return resp.read()


def generate_cat(cat_info, index):
    """生成单只猫"""
    print(f"\n[{index+1}/6] 正在生成: {cat_info['name']}...")
    seed = int.from_bytes(os.urandom(8), "little") % (2**32)
    workflow = build_workflow(cat_info["prompt"], seed, cat_info["name"])

    result = queue_prompt(workflow)
    prompt_id = result["prompt_id"]
    print(f"  已提交 (prompt_id: {prompt_id})，等待生成...")

    images = wait_for_completion(prompt_id, timeout=300)
    if not images:
        print(f"  [FAIL] 生成失败: {cat_info['name']}")
        return False

    for img_info in images:
        img_data = download_image(
            img_info["filename"],
            img_info.get("subfolder", ""),
            img_info.get("type", "output")
        )
        save_path = os.path.join(OUTPUT_DIR, f"{cat_info['name']}.png")
        with open(save_path, "wb") as f:
            f.write(img_data)
        print(f"  [OK] 已保存: {save_path} ({len(img_data)} bytes)")

    return True


def main():
    print("=" * 50)
    print("猫猫立绘批量生成器")
    print(f"ComfyUI: {COMFYUI_URL}")
    print(f"输出目录: {OUTPUT_DIR}")
    print("=" * 50)

    # 检查 ComfyUI 是否运行
    try:
        req = urllib.request.Request(f"{COMFYUI_URL}/system_stats")
        urllib.request.urlopen(req, timeout=5)
        print("[OK] ComfyUI 已连接")
    except Exception as e:
        print(f"[ERROR] 无法连接 ComfyUI: {e}")
        sys.exit(1)

    success = 0
    for i, cat in enumerate(CATS):
        if generate_cat(cat, i):
            success += 1
        time.sleep(2)  # 间隔2秒

    print(f"\n{'=' * 50}")
    print(f"生成完成: {success}/{len(CATS)} 成功")
    print(f"图片保存在: {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
