"""
批量生成5只猫的H3视频
1. 从立绘生成绿幕首帧
2. 用绿幕首帧生成H3视频
3. 抽帧 + 色键抠绿 + 缩放
猫: 黑炭、花斑、银渐层、玄猫、仙喵长老
"""
import json, shutil, time, urllib.request, os, sys, subprocess
from PIL import Image
import numpy as np

COMFY = "http://127.0.0.1:8188"
OUT_ROOT = r"H:\ComfyUI_windows_portable\ComfyUI\output"
INPUT = r"H:\ComfyUI_windows_portable\ComfyUI\input"
FFMPEG = r"C:\Users\zhn\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0-full_build\bin\ffmpeg.exe"

SRC_DIR = r"e:\UnityProject\wuziqi\Assets\Art\Cat"
DST_ROOT = r"e:\UnityProject\wuziqi\Assets\Art\Cat\Frames"

EXPRESSIONS = ["thinking", "smug", "worried", "celebrate", "defeat"]

CATS = [
    {"name": "黑炭", "desc": "a pure black cat with shiny black fur and bright yellow eyes"},
    {"name": "花斑", "desc": "a calico cat with brown, black and white mixed patches"},
    {"name": "银渐层", "desc": "a British Shorthair cat with silver gradient fur, round face and big eyes"},
    {"name": "玄猫", "desc": "a deep black cat with dense fur and mysterious deep eyes"},
    {"name": "仙喵长老", "desc": "an ancient immortal cat with white long fur, long eyebrows and whiskers, sage-like appearance"},
]

VIDEO_PROMPTS = {
    "thinking": "sitting, one paw raised to chin in thinking pose, eyes looking up curiously, slight head tilt and subtle body sway, gentle breathing motion",
    "smug": "sitting, one paw on hip, other paw covering mouth while snickering, eyes squinted in smug expression, tail swaying happily",
    "worried": "sitting, fur standing up, ears pressed back, eyes wide with worry, body trembling slightly, anxious expression",
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


def make_green_first_frame(portrait_path, output_path, size=768):
    """将立绘抠图后放到纯绿背景上"""
    img = Image.open(portrait_path).convert("RGBA")
    img = img.resize((size, size), Image.LANCZOS)

    pixels = np.array(img)
    corners = [
        pixels[0, 0, :3], pixels[0, -1, :3],
        pixels[-1, 0, :3], pixels[-1, -1, :3]
    ]
    bg_color = np.median(corners, axis=0).astype(int)

    rgb = pixels[:, :, :3].astype(float)
    diff = np.sqrt(np.sum((rgb - bg_color) ** 2, axis=2))
    bg_lum = np.mean(bg_color)
    threshold = max(60, min(100, 60 + (200 - bg_lum) * 0.2))

    alpha = np.where(diff < threshold, 0, 255).astype(np.uint8)

    edge_mask = (diff < threshold * 2) & (diff >= threshold)
    alpha[edge_mask] = ((diff[edge_mask] - threshold) / threshold * 255).astype(np.uint8)

    pixels[:, :, 3] = alpha

    green = np.zeros((size, size, 4), dtype=np.uint8)
    green[:, :, 1] = 255
    green[:, :, 3] = 255

    green_img = Image.fromarray(green, 'RGBA')
    cat_img = Image.fromarray(pixels, 'RGBA')
    green_img.paste(cat_img, (0, 0), cat_img)
    green_img.save(output_path)
    return output_path


def post(url, payload):
    req = urllib.request.Request(url, data=json.dumps(payload).encode(),
                                  headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read())

def get(url):
    with urllib.request.urlopen(url, timeout=60) as r:
        return json.loads(r.read())


def submit_video(first_frame_name, prompt, cat_name, exp, seed=31):
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
            "video": ["91", 0], "filename_prefix": f"{cat_name}_{exp}",
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
    """ffmpeg抽帧 -> 色键抠绿 -> 缩放256x256"""
    os.makedirs(output_dir, exist_ok=True)
    raw_dir = os.path.join(output_dir, "_raw")
    os.makedirs(raw_dir, exist_ok=True)

    # 抽帧: 24fps -> 6fps (每4帧取1)
    subprocess.run([FFMPEG, "-y", "-i", video_path, "-vf", f"fps={target_fps}", os.path.join(raw_dir, "f_%04d.png")], capture_output=True)

    raw_files = sorted([f for f in os.listdir(raw_dir) if f.endswith(".png")])
    print(f"  Extracted {len(raw_files)} frames")

    # 色键抠绿 + 缩放
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

        # 缩放256x256
        out = out.resize((256, 256), Image.LANCZOS)
        out.save(os.path.join(output_dir, f"f_{i:04d}.png"))

    return len(raw_files)


def main():
    print("=" * 60)
    print("批量生成5只猫的H3视频 + 抽帧处理")
    print("=" * 60)

    total = len(CATS) * len(EXPRESSIONS)
    done = 0

    for cat in CATS:
        cat_name = cat["name"]
        print(f"\n【{cat_name}】开始处理...")

        for exp in EXPRESSIONS:
            # 检查最终帧是否已存在
            frame_dir = os.path.join(DST_ROOT, cat_name, exp)
            if os.path.exists(frame_dir) and len([f for f in os.listdir(frame_dir) if f.endswith(".png")]) >= 28:
                print(f"  [SKIP] {cat_name}_{exp} 帧已存在")
                done += 1
                continue

            # 1. 准备绿幕首帧
            portrait_name = f"{cat_name}_{exp}_seedream.png"
            portrait_path = os.path.join(SRC_DIR, portrait_name)

            if not os.path.exists(portrait_path):
                print(f"  [SKIP] {portrait_name} 不存在")
                continue

            green_frame_path = os.path.join(SRC_DIR, f"{cat_name}_{exp}_green_first.png")
            print(f"  [{exp}] 生成绿幕首帧...")
            make_green_first_frame(portrait_path, green_frame_path)

            # 2. 复制首帧到ComfyUI input
            first_frame_name = f"{cat_name}_{exp}_firstframe.png"
            shutil.copy2(green_frame_path, os.path.join(INPUT, first_frame_name))

            # 3. 提交H3视频工作流
            prompt = H3_PROMPT_TEMPLATE.format(cat_desc=cat["desc"], pose=VIDEO_PROMPTS[exp])
            print(f"  [{exp}] 提交H3视频...")
            pid = submit_video(first_frame_name, prompt, cat_name, exp, seed=31)
            if not pid:
                print(f"  [FAIL] {cat_name}_{exp} 提交失败")
                continue
            print(f"    已提交: {pid}")

            # 4. 等待完成
            video_path = wait_for_video(pid, timeout=900)
            if not video_path:
                print(f"  [FAIL] {cat_name}_{exp} 生成超时")
                continue
            print(f"    视频完成: {os.path.basename(video_path)}")

            # 5. 抽帧 + 色键抠绿
            count = extract_frames(video_path, frame_dir, target_fps=6)
            print(f"  [OK] {cat_name}_{exp}: {count}帧保存到 {frame_dir}")
            done += 1

    print(f"\n{'=' * 60}")
    print(f"完成: {done}/{total}")
    print(f"帧保存在: {DST_ROOT}")


if __name__ == "__main__":
    main()
