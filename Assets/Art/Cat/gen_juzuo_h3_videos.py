"""
橘座5个表情 H3 绿幕视频生成 (本地ComfyUI)
单首帧锁 + 8步turbo + 124帧
"""
import json, shutil, time, urllib.request, os, sys, subprocess

COMFY = "http://127.0.0.1:8188"
OUT_ROOT = r"H:\ComfyUI_windows_portable\ComfyUI\output"
INPUT = r"H:\ComfyUI_windows_portable\ComfyUI\input"
FFMPEG = r"C:\Users\zhn\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0-full_build\bin\ffmpeg.exe"

SRC_DIR = r"e:\UnityProject\wuziqi\Assets\Art\Cat"
DST_DIR = r"e:\UnityProject\wuziqi\Assets\Art\Cat\Frames\橘座"

EXPRESSIONS = ["thinking", "smug", "worried", "celebrate", "defeat"]

VIDEO_PROMPTS = {
    "thinking": "A cute orange tabby kitten sitting, one paw raised to chin in thinking pose, eyes looking up curiously, slight head tilt and subtle body sway, gentle breathing motion. Green chroma-key background.",
    "smug": "A cute orange tabby kitten sitting, one paw on hip, other paw covering mouth while snickering, eyes squinted in smug expression, tail swaying happily. Green chroma-key background.",
    "worried": "A cute orange tabby kitten sitting, fur standing up, ears pressed back, eyes wide with worry, body trembling slightly, anxious expression. Green chroma-key background.",
    "celebrate": "A cute orange tabby kitten jumping with both front paws raised high, mouth open in joyful laugh, eyes closed happily, tail wagging excitedly. Green chroma-key background.",
    "defeat": "A cute orange tabby kitten lying flat on ground, front paws spread out, eyes half-closed, mouth turned down in defeat, tail limp. Green chroma-key background.",
}

def post(url, payload):
    req = urllib.request.Request(url, data=json.dumps(payload).encode(),
                                  headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read())

def get(url):
    with urllib.request.urlopen(url, timeout=60) as r:
        return json.loads(r.read())

def submit_video(first_frame_name, prompt, exp, seed=31):
    """提交 H3 视频工作流，单首帧锁"""
    WF = {
        "6":   {"class_type": "UNETLoader", "inputs": {
            "unet_name": "minimax_h3_fl2va_pruned_int8_convrot.safetensors", "weight_dtype": "default"}},
        "121": {"class_type": "LoraLoaderModelOnly", "inputs": {
            "model": ["6", 0],
            "lora_name": "minimax_h3_fl2v_turbo_8step_v1.0_comfyui_bf16.safetensors",
            "strength_model": 1.0}},
        "13":  {"class_type": "CLIPLoader", "inputs": {
            "clip_name": "qwen3vl_32b_minimax_h3_nvfp4_awq.safetensors",
            "type": "minimax", "device": "default"}},
        "11":  {"class_type": "VAELoader", "inputs": {"vae_name": "minimax_h3_video_vae_fp16.safetensors"}},
        "15":  {"class_type": "RandomNoise", "inputs": {"noise_seed": seed, "control_after_generate": "fixed"}},
        "200": {"class_type": "LoadImage", "inputs": {"image": first_frame_name, "upload": "image"}},
        "104": {"class_type": "MiniMaxH3ImageToVideo", "inputs": {
            "clip": ["13", 0], "vae": ["11", 0],
            "first_frame": ["200", 0],
            "prompt": prompt, "width": 768, "height": 768, "length": 124}},
        "16":  {"class_type": "BasicGuider", "inputs": {"model": ["121", 0], "conditioning": ["104", 0]}},
        "17":  {"class_type": "KSamplerSelect", "inputs": {"sampler_name": "res_multistep"}},
        "9":   {"class_type": "BasicScheduler", "inputs": {
            "model": ["121", 0], "scheduler": "simple", "steps": 8, "denoise": 1.0}},
        "14":  {"class_type": "SamplerCustomAdvanced", "inputs": {
            "noise": ["15", 0], "guider": ["16", 0], "sampler": ["17", 0],
            "sigmas": ["9", 0], "latent_image": ["104", 1]}},
        "10":  {"class_type": "VAEDecode", "inputs": {"samples": ["14", 0], "vae": ["11", 0]}},
        "91":  {"class_type": "CreateVideo", "inputs": {"images": ["10", 0], "fps": 24, "bit_depth": 8}},
        "92":  {"class_type": "SaveVideo", "inputs": {
            "video": ["91", 0], "filename_prefix": f"juzuo_{exp}",
            "format": "mp4", "codec": "auto"}},
    }
    resp = post(f"{COMFY}/prompt", {"prompt": WF})
    if "error" in resp or resp.get("node_errors"):
        print(f"  REJECTED: {json.dumps(resp, ensure_ascii=False)[:500]}")
        return None
    return resp["prompt_id"]

def wait_for_video(pid, timeout=900):
    start = time.time()
    while True:
        time.sleep(10)
        try:
            h = get(f"{COMFY}/history/{pid}")
        except Exception:
            continue
        entry = h.get(pid)
        if not entry:
            elapsed = int(time.time() - start)
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
                    src = os.path.join(OUT_ROOT, item.get("subfolder", ""), fname)
                    return src
        if time.time() - start > timeout:
            print("  TIMEOUT!")
            return None

def extract_frames(video_path, output_dir, target_fps=6):
    """ffmpeg抽帧 -> 色键抠绿 -> 缩放512x512"""
    os.makedirs(output_dir, exist_ok=True)
    raw_dir = os.path.join(output_dir, "raw")
    os.makedirs(raw_dir, exist_ok=True)

    # 抽帧: 24fps -> 6fps (每4帧取1)
    subprocess.run([FFMPEG, "-y", "-i", video_path, "-vf", f"fps={target_fps}", os.path.join(raw_dir, "f_%04d.png")], capture_output=True)

    raw_files = sorted([f for f in os.listdir(raw_dir) if f.endswith(".png")])
    print(f"  Extracted {len(raw_files)} frames")

    # 色键抠绿 + 缩放
    from PIL import Image
    for i, fname in enumerate(raw_files):
        img = Image.open(os.path.join(raw_dir, fname)).convert("RGBA")
        w, h = img.size
        px = img.load()
        out = Image.new("RGBA", (w, h), (0,0,0,0))
        po = out.load()

        for y in range(h):
            for x in range(w):
                r, g, b, a = px[x, y]
                dom = int(g) - max(int(r), int(b))
                if dom > 60:
                    po[x, y] = (r, g, b, 0)
                elif dom > 15:
                    alpha = int((dom - 15) * 255 / 45)
                    po[x, y] = (r, g, b, min(255, 255 - alpha))
                else:
                    if g > r + 12 and g > b + 12:
                        g2 = max(r, b)
                        po[x, y] = (r, g2, b, 255)
                    else:
                        po[x, y] = (r, g, b, 255)

        # 缩放512x512
        out = out.resize((512, 512), Image.LANCZOS)
        out.save(os.path.join(output_dir, f"f_{i:04d}.png"))

    # 清理raw目录
    shutil.rmtree(raw_dir)
    return len(raw_files)


if __name__ == "__main__":
    print("=== Juzuo H3 Video Generation ===\n")

    for exp in EXPRESSIONS:
        print(f"[{exp}] Processing...")

        # 1. 复制首帧到ComfyUI input
        src_img = os.path.join(SRC_DIR, f"juzuo_{exp}_seedream.png")
        if not os.path.exists(src_img):
            # thinking用test文件
            src_img = os.path.join(SRC_DIR, "juzuo_thinking_seedream.png")

        first_frame_name = f"juzuo_{exp}_firstframe.png"
        shutil.copy2(src_img, os.path.join(INPUT, first_frame_name))
        print(f"  First frame: {first_frame_name}")

        # 2. 提交H3视频工作流
        prompt = VIDEO_PROMPTS.get(exp, VIDEO_PROMPTS["thinking"])
        pid = submit_video(first_frame_name, prompt, exp, seed=31)
        if not pid:
            print(f"  Failed to submit, skipping")
            continue
        print(f"  Submitted: {pid}")

        # 3. 等待完成
        video_path = wait_for_video(pid, timeout=900)
        if not video_path:
            print(f"  Failed to generate video")
            continue
        print(f"  Video ready: {os.path.basename(video_path)}")

        # 4. 抽帧 + 色键抠绿
        frame_dir = os.path.join(DST_DIR, exp)
        count = extract_frames(video_path, frame_dir, target_fps=6)
        print(f"  Frames: {count} saved to {frame_dir}")
        print(f"  Done!\n")

    print("=== All Done ===")
