#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# regen_heitan_videos.py
# 重新生成黑炭视频

import json
import urllib.request
import os
import time
import shutil
import random
from pathlib import Path
from PIL import Image

COMFYUI_URL = "http://127.0.0.1:8188"
OUTPUT_DIR = r"H:\ComfyUI_windows_portable\ComfyUI\output"
INPUT_DIR = r"H:\ComfyUI_windows_portable\ComfyUI\input"
FFMPEG = r"C:\Users\zhn\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0-full_build\bin\ffmpeg.exe"

# 黑炭描述
CAT_DESC = "a pure black cat with shiny black fur and bright yellow eyes"

# 表情和提示词
EXPRESSIONS = {
    "celebrate": "jumping with both front paws raised high, mouth open in joyful laugh, eyes closed happily, tail wagging excitedly",
    "defeat": "lying flat on ground, front paws spread out, eyes half-closed, mouth turned down in defeat, tail limp",
}

H3_PROMPT_TEMPLATE = (
    "Traditional Chinese ink wash painting, light color wash, rice paper texture. "
    "{cat_desc}, wearing red collar with golden bell. {pose}. "
    "Full body visible, centered, cat fills 70 percent of frame. "
    "SOLID BRIGHT GREEN #00FF00 chroma key background, flat uniform color, no texture no gradient no shadow. "
    "Ink brush strokes with varying density. No text no other objects."
)


def post(url, payload):
    req = urllib.request.Request(url, data=json.dumps(payload).encode(), headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read())

def get(url):
    with urllib.request.urlopen(url, timeout=60) as r:
        return json.loads(r.read())


def submit_video(first_frame_name, prompt, expression):
    """提交视频生成任务"""
    seed = random.randint(1, 2**32 - 1)

    WF = {
        "6":   {"class_type": "UNETLoader", "inputs": {"unet_name": "minimax_h3_fl2va_pruned_int8_convrot.safetensors", "weight_dtype": "default"}},
        "121": {"class_type": "LoraLoaderModelOnly", "inputs": {"model": ["6", 0], "lora_name": "minimax_h3_fl2v_turbo_8step_v1.0_comfyui_bf16.safetensors", "strength_model": 1.0}},
        "13":  {"class_type": "CLIPLoader", "inputs": {"clip_name": "qwen3vl_32b_minimax_h3_nvfp4_awq.safetensors", "type": "minimax", "device": "default"}},
        "11":  {"class_type": "VAELoader", "inputs": {"vae_name": "minimax_h3_video_vae_fp16.safetensors"}},
        "15":  {"class_type": "RandomNoise", "inputs": {"noise_seed": seed, "control_after_generate": "fixed"}},
        "200": {"class_type": "LoadImage", "inputs": {"image": first_frame_name, "upload": "image"}},
        "104": {"class_type": "MiniMaxH3ImageToVideo", "inputs": {"clip": ["13", 0], "vae": ["11", 0], "first_frame": ["200", 0], "prompt": prompt, "width": 768, "height": 768, "length": 124}},
        "16":  {"class_type": "BasicGuider", "inputs": {"model": ["121", 0], "conditioning": ["104", 0]}},
        "17":  {"class_type": "KSamplerSelect", "inputs": {"sampler_name": "res_multistep"}},
        "9":   {"class_type": "BasicScheduler", "inputs": {"model": ["121", 0], "scheduler": "simple", "steps": 8, "denoise": 1.0}},
        "14":  {"class_type": "SamplerCustomAdvanced", "inputs": {"noise": ["15", 0], "guider": ["16", 0], "sampler": ["17", 0], "sigmas": ["9", 0], "latent_image": ["104", 1]}},
        "10":  {"class_type": "VAEDecode", "inputs": {"samples": ["14", 0], "vae": ["11", 0]}},
        "91":  {"class_type": "CreateVideo", "inputs": {"images": ["10", 0], "fps": 24, "bit_depth": 8}},
        "92":  {"class_type": "SaveVideo", "inputs": {"video": ["91", 0], "filename_prefix": f"黑炭_{expression}", "format": "mp4", "codec": "auto"}},
    }

    resp = post(f"{COMFYUI_URL}/prompt", {"prompt": WF})
    if "error" in resp or resp.get("node_errors"):
        print(f"  REJECTED: {json.dumps(resp, ensure_ascii=False)[:500]}")
        return None
    return resp["prompt_id"]


def wait_for_video(pid, timeout=900):
    """等待视频生成完成"""
    start = time.time()
    while True:
        time.sleep(10)
        try:
            h = get(f"{COMFYUI_URL}/history/{pid}")
        except Exception:
            continue
        entry = h.get(pid)
        if not entry:
            elapsed = int(time.time() - start)
            if elapsed % 60 == 0:
                print(f"  Waiting... {elapsed}s")
            if elapsed > timeout:
                print("  TIMEOUT!")
                return None
            continue
        if entry.get("status", {}).get("status_str") == "error":
            print(f"  ERROR: {entry['status'].get('messages')}")
            return None
        for nid, out in entry.get("outputs", {}).items():
            for item in out.get("videos", []):
                fname = item.get("filename")
                if fname and fname.endswith(".mp4"):
                    return os.path.join(OUTPUT_DIR, item.get("subfolder", ""), fname)
        if time.time() - start > timeout:
            print("  TIMEOUT!")
            return None


def main():
    print("=" * 60)
    print("Regenerating HeiTan (Black Charcoal) videos")
    print("=" * 60)

    # 检查首帧
    portrait_path = r"e:\UnityProject\wuziqi\Assets\Art\Cat\Portraits\Portrait_黑炭.png"
    if not os.path.exists(portrait_path):
        print(f"ERROR: Portrait not found: {portrait_path}")
        return

    # 读取首帧
    img = Image.open(portrait_path).convert('RGB')
    first_frame = img.resize((768, 768), Image.LANCZOS)

    for expression, pose in EXPRESSIONS.items():
        print(f"\n--- {expression} ---")

        # 保存首帧到ComfyUI输入目录
        first_frame_name = f"黑炭_{expression}_firstframe.png"
        first_frame_path = os.path.join(INPUT_DIR, first_frame_name)
        first_frame.save(first_frame_path)
        print(f"  First frame saved: {first_frame_name}")

        # 生成提示词
        prompt = H3_PROMPT_TEMPLATE.format(cat_desc=CAT_DESC, pose=pose)
        print(f"  Prompt: {prompt[:100]}...")

        # 提交视频生成
        pid = submit_video(first_frame_name, prompt, expression)
        if not pid:
            print(f"  FAILED to submit")
            continue
        print(f"  Submitted: {pid}")

        # 等待完成
        video_path = wait_for_video(pid, timeout=900)
        if not video_path:
            print(f"  FAILED - timeout or error")
            continue
        print(f"  Video completed: {video_path}")

        # 验证视频内容
        debug_path = r"e:\UnityProject\wuziqi\Assets\Art\Cat\debug_video_check.png"
        os.system(f'"{FFMPEG}" -y -i "{video_path}" -vf "select=eq(n\\,30)" -frames:v 1 "{debug_path}"')

        print(f"  Video generated successfully!")

    print("\n" + "=" * 60)
    print("Done!")
    print("=" * 60)


if __name__ == "__main__":
    main()
