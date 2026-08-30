"""
逐个生成H3视频（一个一个处理）
用法: python gen_video_one.py <猫名> <表情>
背景策略：黑猫用白色背景，白猫/三花用黑色背景
"""
import json, shutil, time, urllib.request, os, sys, subprocess
from PIL import Image
from rembg import remove, new_session
os.environ['OMP_NUM_THREADS'] = '4'
rembg_session = new_session('u2net')

COMFY = "http://127.0.0.1:8188"
OUT_ROOT = r"H:\ComfyUI_windows_portable\ComfyUI\output"
INPUT = r"H:\ComfyUI_windows_portable\ComfyUI\input"
FFMPEG = r"C:\Users\zhn\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0-full_build\bin\ffmpeg.exe"

SRC_DIR = r"e:\UnityProject\wuziqi\Assets\Art\Cat"
DST_ROOT = r"e:\UnityProject\wuziqi\Assets\Art\Cat\Frames"

CAT_DESCS = {
    "黑炭": "a pure black cat with shiny black fur and bright yellow eyes",
    "花斑": "a calico cat with brown, black and white mixed patches",
    "银渐层": "a British Shorthair cat with silver gradient fur, round face and big eyes",
    "玄猫": "a deep black cat with dense fur and mysterious deep eyes",
    "仙喵长老": "an ancient immortal cat with white long fur, long eyebrows and whiskers, sage-like appearance",
}

# 背景色映射：黑猫用白色，白/三花用黑色
BACKGROUND_MAP = {
    "黑炭": "SOLID WHITE background, flat uniform color, no texture no gradient no shadow",
    "玄猫": "SOLID WHITE background, flat uniform color, no texture no gradient no shadow",
    "花斑": "SOLID BLACK background, flat uniform color, no texture no gradient no shadow",
    "银渐层": "SOLID BLACK background, flat uniform color, no texture no gradient no shadow",
    "仙喵长老": "SOLID BLACK background, flat uniform color, no texture no gradient no shadow",
}

VIDEO_PROMPTS = {
    "idle": "sitting calmly, relaxed posture, gentle breathing, subtle tail sway, peaceful expression",
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
    "{background}. "
    "Ink brush strokes with varying density. No text no other objects."
)


def post(url, payload):
    req = urllib.request.Request(url, data=json.dumps(payload).encode(), headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read())

def get(url):
    with urllib.request.urlopen(url, timeout=60) as r:
        return json.loads(r.read())


def submit_video(first_frame_name, prompt, cat_name, exp, seed=31):
    WF = {
        "6":   {"class_type": "UNETLoader", "inputs": {"unet_name": "minimax_h3_fl2va_pruned_int8_convrot.safetensors", "weight_dtype": "default"}},
        "121": {"class_type": "LoraLoaderModelOnly", "inputs": {"model": ["6", 0], "lora_name": "minimax_h3_fl2v_turbo_8step_v1.0_comfyui_bf16.safetensors", "strength_model": 1.0}},
        "13":  {"class_type": "CLIPLoader", "inputs": {"clip_name": "qwen3vl_32b_minimax_h3_nvfp4_awq.safetensors", "type": "minimax", "device": "default"}},
        "11":  {"class_type": "VAELoader", "inputs": {"vae_name": "minimax_h3_video_vae_fp16.safetensors"}},
        "15":  {"class_type": "RandomNoise", "inputs": {"noise_seed": seed, "control_after_generate": "fixed"}},
        "200": {"class_type": "LoadImage", "inputs": {"image": first_frame_name, "upload": "image"}},
        "104": {"class_type": "MiniMaxH3ImageToVideo", "inputs": {"clip": ["13", 0], "vae": ["11", 0], "first_frame": ["200", 0], "prompt": prompt, "width": 768, "height": 768, "length": 48}},  # 2秒视频，测试是否支持更短
        "16":  {"class_type": "BasicGuider", "inputs": {"model": ["121", 0], "conditioning": ["104", 0]}},
        "17":  {"class_type": "KSamplerSelect", "inputs": {"sampler_name": "res_multistep"}},
        "9":   {"class_type": "BasicScheduler", "inputs": {"model": ["121", 0], "scheduler": "simple", "steps": 8, "denoise": 1.0}},
        "14":  {"class_type": "SamplerCustomAdvanced", "inputs": {"noise": ["15", 0], "guider": ["16", 0], "sampler": ["17", 0], "sigmas": ["9", 0], "latent_image": ["104", 1]}},
        "10":  {"class_type": "VAEDecode", "inputs": {"samples": ["14", 0], "vae": ["11", 0]}},
        "91":  {"class_type": "CreateVideo", "inputs": {"images": ["10", 0], "fps": 24, "bit_depth": 8}},
        "92":  {"class_type": "SaveVideo", "inputs": {"video": ["91", 0], "filename_prefix": f"{cat_name}_{exp}", "format": "mp4", "codec": "auto"}},
    }
    resp = post(f"{COMFY}/prompt", {"prompt": WF})
    if "error" in resp or resp.get("node_errors"):
        print(f"REJECTED: {json.dumps(resp, ensure_ascii=False)[:500]}")
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
            if elapsed % 60 == 0:
                print(f"Waiting... {elapsed}s")
            if elapsed > timeout:
                print("TIMEOUT!")
                return None
            continue
        if entry.get("status", {}).get("status_str") == "error":
            print(f"ERROR: {entry['status'].get('messages')}")
            return None
        for nid, out in entry.get("outputs", {}).items():
            for item in out.get("videos", []):
                fname = item.get("filename")
                if fname and fname.endswith(".mp4"):
                    return os.path.join(OUT_ROOT, item.get("subfolder", ""), fname)
        if time.time() - start > timeout:
            print("TIMEOUT!")
            return None


def extract_frames(video_path, output_dir, target_fps=6):
    """用ffmpeg切帧，然后用rembg抠图"""
    os.makedirs(output_dir, exist_ok=True)
    raw_dir = os.path.join(output_dir, "_raw")
    os.makedirs(raw_dir, exist_ok=True)
    subprocess.run([FFMPEG, "-y", "-i", video_path, "-vf", f"fps={target_fps}", os.path.join(raw_dir, "f_%04d.png")], capture_output=True)
    raw_files = sorted([f for f in os.listdir(raw_dir) if f.endswith(".png")])
    print(f"Extracted {len(raw_files)} frames")

    for i, fname in enumerate(raw_files):
        raw_path = os.path.join(raw_dir, fname)
        img = Image.open(raw_path).convert("RGBA")
        # 用rembg抠图
        result = remove(img, session=rembg_session)
        result = result.resize((256, 256), Image.LANCZOS)
        result.save(os.path.join(output_dir, f"f_{i:04d}.png"))
    return len(raw_files)


if __name__ == "__main__":
    if len(sys.argv) < 3:
        print("Usage: python gen_video_one.py <猫名> <表情>")
        print("Example: python gen_video_one.py 黑炭 celebrate")
        sys.exit(1)

    cat_name = sys.argv[1]
    exp = sys.argv[2]

    if cat_name not in CAT_DESCS:
        print(f"Unknown cat: {cat_name}")
        sys.exit(1)
    if exp not in VIDEO_PROMPTS:
        print(f"Unknown expression: {exp}")
        sys.exit(1)

    cat_desc = CAT_DESCS[cat_name]
    print(f"=== {cat_name} - {exp} ===")

    # 检查是否已有帧
    frame_dir = os.path.join(DST_ROOT, cat_name, exp)
    if os.path.exists(frame_dir) and len([f for f in os.listdir(frame_dir) if f.endswith(".png")]) >= 28:
        print("Frames already exist, skipping")
        sys.exit(0)

    # 准备对比色首帧（直接用已处理的contrast立绘）
    portrait_path = os.path.join(SRC_DIR, cat_name, f"{cat_name}_{exp}_contrast.png")
    if not os.path.exists(portrait_path):
        print(f"Portrait not found: {portrait_path}")
        sys.exit(1)

    first_frame_name = f"{cat_name}_{exp}_firstframe.png"
    shutil.copy2(portrait_path, os.path.join(INPUT, first_frame_name))
    print(f"Using: {cat_name}_{exp}_contrast.png")

    # 提交H3视频
    background = BACKGROUND_MAP.get(cat_name, BACKGROUND_MAP["黑炭"])
    prompt = H3_PROMPT_TEMPLATE.format(cat_desc=cat_desc, pose=VIDEO_PROMPTS[exp], background=background)
    print("Submitting H3 video...")
    pid = submit_video(first_frame_name, prompt, cat_name, exp, seed=31)
    if not pid:
        print("Submit failed")
        sys.exit(1)
    print(f"Submitted: {pid}")

    video_path = wait_for_video(pid, timeout=900)
    if not video_path:
        print("Generation timeout")
        sys.exit(1)
    print(f"Video: {os.path.basename(video_path)}")

    count = extract_frames(video_path, frame_dir, target_fps=6)
    print(f"Done: {count} frames")
