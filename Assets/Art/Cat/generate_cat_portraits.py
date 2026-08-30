"""
用 ComfyUI API 批量生成猫猫选择头像
基于 Qwen-Image-2512 + Lightning 4steps LoRA
输出：1024x1024 猫头头像，用于 CatItem 选择界面
"""
import json
import urllib.request
import urllib.parse
import time
import os
import sys

COMFYUI_URL = "http://127.0.0.1:8188"
OUTPUT_DIR = os.path.dirname(os.path.abspath(__file__))

# 通用风格描述（水墨画风格）
STYLE = (
    "painterly oil painting style on canvas, thick visible paint brushstrokes, "
    "impasto texture, rich oil paint colors, hand painted look, "
    "fur rendered with thick paint strokes, traditional painting technique, "
    "red collar with golden bell around neck, solid flat color background, "
    "centered composition, masterpiece quality"
)

# 负面提示词
NEGATIVE = (
    "anime, cartoon, cel shading, flat color, vector art, digital illustration, "
    "realistic, photorealistic, 3d render, ugly, deformed, blurry, low quality, "
    "text, watermark, signature, complex background"
)

# 7只猫的头像提示词（先只生成小白测试）
CATS = [
    {
        "name": "portrait_小白",
        "prompt": (
            "a fluffy white kitten head portrait, close-up face shot, "
            "round face with happy closed eyes smiling, pink nose, "
            "long fluffy white fur with light gray tabby stripes on forehead, "
            "raised paw waving, red collar with golden bell, "
            "oil painting on canvas, thick visible brushstrokes, impasto texture, "
            "rich paint colors, hand painted traditional style, "
            "solid green background"
        )
    },
    # 其余猫暂时注释，等小白确认后再生成
    # {
    #     "name": "portrait_橘座",
    #     "prompt": "..."
    # },
]


def build_workflow(prompt_text, seed, filename_prefix):
    """构建 ComfyUI API 工作流 JSON"""
    return {
        "248": {
            "class_type": "UnetLoaderGGUF",
            "inputs": {
                "unet_name": "qwen-image-2512-Q4_K_M.gguf"
            }
        },
        "259": {
            "class_type": "LoraLoaderModelOnly",
            "inputs": {
                "model": ["248", 0],
                "lora_name": "Qwen-Image-2512-Lightning-8steps-V1.0-bf16.safetensors",
                "strength_model": 1.0
            }
        },
        "247": {
            "class_type": "ModelSamplingAuraFlow",
            "inputs": {
                "model": ["259", 0],
                "shift": 3.1
            }
        },
        "245": {
            "class_type": "CLIPLoader",
            "inputs": {
                "clip_name": "qwen_2.5_vl_7b_fp8_scaled.safetensors",
                "type": "qwen_image",
                "device": "default"
            }
        },
        "246": {
            "class_type": "VAELoader",
            "inputs": {
                "vae_name": "qwen_image_vae.safetensors"
            }
        },
        "249p": {
            "class_type": "CLIPTextEncode",
            "inputs": {
                "clip": ["245", 0],
                "text": f"{prompt_text}, {STYLE}"
            }
        },
        "249n": {
            "class_type": "CLIPTextEncode",
            "inputs": {
                "clip": ["245", 0],
                "text": NEGATIVE
            }
        },
        "252": {
            "class_type": "EmptySD3LatentImage",
            "inputs": {
                "width": 1024,
                "height": 1024,
                "batch_size": 1
            }
        },
        "253": {
            "class_type": "KSampler",
            "inputs": {
                "model": ["247", 0],
                "positive": ["249p", 0],
                "negative": ["249n", 0],
                "latent_image": ["252", 0],
                "seed": seed,
                "steps": 8,
                "cfg": 2.5,
                "sampler_name": "euler",
                "scheduler": "simple",
                "denoise": 1.0
            }
        },
        "251": {
            "class_type": "VAEDecode",
            "inputs": {
                "samples": ["253", 0],
                "vae": ["246", 0]
            }
        },
        "255": {
            "class_type": "SaveImage",
            "inputs": {
                "images": ["251", 0],
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
        time.sleep(3)
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


def generate_cat_portrait(cat_info, index):
    """生成单只猫头像"""
    print(f"\n[{index+1}/{len(CATS)}] 正在生成: {cat_info['name']}...")
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
    print("猫猫选择头像生成器")
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
        if generate_cat_portrait(cat, i):
            success += 1
        time.sleep(2)

    print(f"\n{'=' * 50}")
    print(f"生成完成: {success}/{len(CATS)} 成功")
    print(f"图片保存在: {OUTPUT_DIR}")


if __name__ == "__main__":
    main()
