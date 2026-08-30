#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# gen_heitan_videos.py
# 为黑炭生成5个表情视频，使用绿幕首帧

import json
import urllib.request
import os
import time
import shutil
from pathlib import Path

COMFYUI_URL = "http://127.0.0.1:8188"

# 黑炭立绘（已经是绿幕）
PORTRAIT = r"e:\UnityProject\wuziqi\Assets\Art\Cat\Portraits\Portrait_黑炭.png"

EXPRESSIONS = {
    "idle": "A cute black cat sitting calmly, breathing gently, slight ear twitches, peaceful and relaxed expression",
    "happy": "A cute black cat with bright eyes, tail swaying happily, ears perked up with joy, cheerful expression",
    "celebrate": "A cute black cat jumping excitedly, waving paws in celebration, tail held high, triumphant celebration",
    "smug": "A cute black cat with half-closed eyes, slight smirk, tail swaying slowly, confident and proud expression",
    "defeat": "A cute black cat with drooping ears, sad eyes, sighing, looking dejected and disappointed"
}

# 加载模板
WORKFLOW_PATH = Path(r"e:\UnityProject\wuziqi\ComfyUI\models\checkpoints\h3_template.json")

def prepare_first_frame(output_path):
    """立绘已经是绿幕，直接复制并resize"""
    shutil.copy2(PORTRAIT, output_path)
    print(f"[OK] First frame ready: {output_path}")

def gen_expression(expression, prompt):
    """生成单个表情视频"""
    # 准备首帧
    first_frame_dir = Path(r"e:\UnityProject\wuziqi\Assets\Art\Cat\黑炭")
    first_frame_dir.mkdir(exist_ok=True)
    first_frame_path = first_frame_dir / f"{expression}_green_first.png"
    prepare_first_frame(first_frame_path)

    # 加载模板
    with open(WORKFLOW_PATH, 'r', encoding='utf-8') as f:
        workflow = json.load(f)

    # 修改提示词和首帧路径
    prompt_text_node = workflow["6"]
    prompt_text_node["inputs"]["text"] = prompt

    load_image_node = workflow["10"]
    load_image_node["inputs"]["image"] = str(first_frame_path).replace("\\", "/")

    # 随机种子
    import random
    seed_node = workflow["3"]
    seed_node["inputs"]["seed"] = random.randint(1, 2**32 - 1)

    # 发送到ComfyUI
    data = json.dumps({"prompt": workflow}).encode('utf-8')
    req = urllib.request.Request(
        f"{COMFYUI_URL}/prompt",
        data=data,
        headers={"Content-Type": "application/json"}
    )
    resp = urllib.request.urlopen(req)
    prompt_id = json.loads(resp.read())["prompt_id"]

    print(f"[QUEUED] {expression} - prompt_id: {prompt_id}")

    # 等待完成
    while True:
        try:
            resp = urllib.request.urlopen(f"{COMFYUI_URL}/history/{prompt_id}")
            history = json.loads(resp.read())
            if prompt_id in history:
                outputs = history[prompt_id]["outputs"]
                for node_id, node_output in outputs.items():
                    if "gifs" in node_output:
                        for gif_info in node_output["gifs"]:
                            filename = gif_info["filename"]
                            subfolder = gif_info.get("subfolder", "")
                            src_path = Path(r"e:\UnityProject\wuziqi\ComfyUI\output") / subfolder / filename
                            dst_path = first_frame_dir / f"{expression}_video.mp4"
                            shutil.copy2(src_path, dst_path)
                            print(f"[DONE] {expression}: {dst_path}")
                            return str(dst_path)
        except Exception as e:
            pass
        time.sleep(2)

def main():
    print("=" * 60)
    print("Generating HeiTan (Black Charcoal) expression videos")
    print("=" * 60)

    for expr, prompt in EXPRESSIONS.items():
        print(f"\n--- {expr} ---")
        video_path = gen_expression(expr, prompt)

    print("\n" + "=" * 60)
    print("All HeiTan videos generated!")
    print("=" * 60)

if __name__ == "__main__":
    main()
