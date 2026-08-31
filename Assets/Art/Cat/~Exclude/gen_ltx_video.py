"""LTX-Video 图生视频测试 - 使用完整 checkpoint"""
import requests
import json
from pathlib import Path
import time

COMFYUI_URL = "http://127.0.0.1:8188"

def generate_ltx_video(image_path, prompt, output_name="test"):
    """使用 LTX-Video 生成视频"""
    workflow = {
        "6": {
            "class_type": "CheckpointLoaderSimple",
            "inputs": {
                "ckpt_name": "ltxv-2b-0.9.8-distilled.safetensors"
            }
        },
        "7": {
            "class_type": "CLIPTextEncode",
            "inputs": {
                "text": prompt,
                "clip": ["6", 1]
            }
        },
        "8": {
            "class_type": "CLIPTextEncode",
            "inputs": {
                "text": "blurry, distorted, low quality, artifacts",
                "clip": ["6", 1]
            }
        },
        "9": {
            "class_type": "LoadImage",
            "inputs": {
                "image": "test.png"
            }
        },
        "10": {
            "class_type": "LTXVConditioning",
            "inputs": {
                "positive": ["7", 0],
                "negative": ["8", 0],
                "frame_rate": 24.0
            }
        },
        "11": {
            "class_type": "LTXVImgToVideo",
            "inputs": {
                "model": ["6", 0],
                "positive": ["10", 0],
                "negative": ["10", 1],
                "vae": ["6", 2],
                "image": ["9", 0],
                "width": 768,
                "height": 768,
                "length": 25,
                "batch_size": 1,
                "strength": 1.0
            }
        },
        "12": {
            "class_type": "VAEDecode",
            "inputs": {
                "samples": ["11", 2],
                "vae": ["6", 2]
            }
        },
        "13": {
            "class_type": "SaveAnimatedWEBP",
            "inputs": {
                "images": ["12", 0],
                "filename_prefix": f"ltx_{output_name}",
                "fps": 24,
                "lossless": False,
                "quality": 90,
                "method": "default"
            }
        }
    }

    # 上传图片
    print(f"上传图片: {image_path}")
    with open(image_path, "rb") as f:
        resp = requests.post(
            f"{COMFYUI_URL}/upload/image",
            files={"image": (Path(image_path).name, f, "image/png")}
        )
    uploaded_name = resp.json()["name"]
    print(f"上传成功: {uploaded_name}")

    # 更新工作流中的图片引用
    workflow["9"]["inputs"]["image"] = uploaded_name

    # 提交任务
    print("提交 LTX-Video 任务...")
    resp = requests.post(
        f"{COMFYUI_URL}/prompt",
        json={"prompt": workflow}
    )
    result = resp.json()
    if "error" in result:
        print(f"错误: {result['error']}")
        if "node_errors" in result:
            print(f"节点错误: {json.dumps(result['node_errors'], indent=2)}")
        return None
    prompt_id = result["prompt_id"]
    print(f"任务ID: {prompt_id}")

    # 轮询等待完成
    start_time = time.time()
    while True:
        resp = requests.get(f"{COMFYUI_URL}/history/{prompt_id}")
        history = resp.json()
        if prompt_id in history:
            outputs = history[prompt_id].get("outputs", {})
            if "13" in outputs:
                elapsed = time.time() - start_time
                print(f"生成完成！耗时: {elapsed:.1f}秒")
                return outputs["13"]
        time.sleep(2)

if __name__ == "__main__":
    # 测试用黑炭立绘生成视频
    test_image = r"e:\UnityProject\wuziqi\Assets\Art\Cat\Frames\黑炭\idle\f_0001.png"
    prompt = "cute cartoon cat sitting calmly, gentle breathing, subtle tail sway, peaceful expression"

    result = generate_ltx_video(test_image, prompt, "heitang_test")
    if result:
        print(f"输出: {result}")
