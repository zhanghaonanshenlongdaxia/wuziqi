"""
猫猫表情动画生成管线（通用模板）
===============================
使用方法：
  1. 修改 CAT_CONFIG 中的猫咪配置
  2. 运行 python cat_animation_pipeline.py
  3. 自动完成：Seedream立绘 -> H3绿幕视频 -> 抽帧抠绿 -> 序列帧

依赖：
  - ComfyUI 运行中 (http://127.0.0.1:8188)
  - 火山引擎 Seedream API Key
  - ffmpeg (在PATH中)
"""
import json, base64, shutil, time, os, sys, subprocess, urllib.request
from PIL import Image

# ============================================================
# 配置区：每次只需要改这里
# ============================================================

CAT_CONFIG = {
    # 猫咪名称（对应 Frames/{猫名}/ 目录）
    "name": "橘座",

    # 大头照路径（用于 Seedream 图生图保持画风）
    "portrait": r"e:\UnityProject\wuziqi\Assets\Art\Cat\Portraits\Portrait_橘座.png",

    # 各表情的姿势提示词（英文，描述动作即可，通用部分自动补全）
    "expressions": {
        "thinking": "one paw raised to chin in thinking pose, eyes looking up curiously, slight head tilt",
        "smug": "one paw on hip, other paw covering mouth while snickering, eyes squinted, tail swaying",
        "worried": "fur standing up, ears pressed back, eyes wide with worry, body trembling slightly",
        "celebrate": "both front paws raised high, mouth open in joyful laugh, eyes closed happily",
        "defeat": "lying flat on ground, front paws spread out, eyes half-closed, mouth turned down",
    },

    # 输出目录（Frames/{猫名}/{表情}/）
    "output_base": r"e:\UnityProject\wuziqi\Assets\Art\Cat\Frames",
}

# ============================================================
# 通用配置（一般不需要改）
# ============================================================

# Seedream API
SEEDREAM_API_KEY = "ark-5ac4efb5-b854-4ddc-8637-d83e02688b6e-a80b1"
SEEDREAM_API_URL = "https://ark.cn-beijing.volces.com/api/plan/v3/images/generations"
SEEDREAM_MODEL = "doubao-seedream-5.0-lite"

# ComfyUI
COMFY_URL = "http://127.0.0.1:8188"
COMFY_OUT = r"H:\ComfyUI_windows_portable\ComfyUI\output"
COMFY_INPUT = r"H:\ComfyUI_windows_portable\ComfyUI\input"

# FFmpeg
FFMPEG = r"C:\Users\zhn\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0-full_build\bin\ffmpeg.exe"

# ============================================================
# 通用提示词模板（自动拼接）
# ============================================================

# 立绘生成提示词（中文，Seedream用）
SEEDREAM_POSE_TEMPLATE = (
    "传统中国水墨工笔画风格，淡彩设色，宣纸笔墨质感。"
    "一只胖嘟嘟的橘色虎斑猫，戴着红色项圈挂着金色铃铛。"
    "{pose}。"
    "橘色和奶油白色相间的虎斑花纹，白肚皮。"
    "背景纯绿色幕布(#00FF00)，纯色无纹理无渐变无阴影。"
    "笔触写意墨色浓淡变化。古风手绘可爱风。"
    "猫占画面七成，居中构图。全身完整可见。无文字无其他物件。"
)

# H3视频提示词（中英文混合：中文保画风+英文保绿幕）
H3_VIDEO_PROMPT_TEMPLATE = (
    "Traditional Chinese ink wash painting, light color wash, rice paper texture. "
    "A chubby orange tabby cat, {pose}. "
    "Wearing red collar with golden bell. Full body visible, centered, cat fills 70 percent of frame. "
    "SOLID BRIGHT GREEN #00FF00 chroma key background, flat uniform color, no texture no gradient no shadow. "
    "Ink brush strokes with varying density. No text no other objects."
)


# ============================================================
# 绿幕首帧生成（关键技巧！H3模型会继承首帧的背景色）
# ============================================================

def make_green_first_frame(portrait_path, output_path, size=768):
    """将立绘抠图后放到纯绿背景上，作为H3视频的首帧。
    这样H3模型会从纯绿背景开始，全程保持绿色幕布。"""
    img = Image.open(portrait_path).convert("RGBA")
    img = img.resize((size, size), Image.LANCZOS)
    w, h = img.size
    px = img.load()

    # 采样四角背景色
    corners = []
    sz = min(20, w // 4, h // 4)
    for (sx, sy) in [(0, 0), (w - sz, 0), (0, h - sz), (w - sz, h - sz)]:
        rs, gs, bs = [], [], []
        for dy in range(sz):
            for dx in range(sz):
                r, g, b = px[sx + dx, sy + dy][:3]
                rs.append(r); gs.append(g); bs.append(b)
        rs.sort(); gs.sort(); bs.sort()
        mid = len(rs) // 2
        corners.append((rs[mid], gs[mid], bs[mid]))
    bg_r = sum(c[0] for c in corners) // 4
    bg_g = sum(c[1] for c in corners) // 4
    bg_b = sum(c[2] for c in corners) // 4

    bg_lum = (bg_r + bg_g + bg_b) / 3
    hard_thr = max(60, bg_lum * 0.6)
    soft_thr = max(25, hard_thr * 0.35)

    GREEN = (0, 255, 0)
    out = Image.new("RGBA", (w, h), GREEN + (255,))
    po = out.load()
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            dist = ((int(r) - bg_r)**2 + (int(g) - bg_g)**2 + (int(b) - bg_b)**2)**0.5
            if dist < soft_thr:
                po[x, y] = GREEN + (255,)
            elif dist < hard_thr:
                t = (dist - soft_thr) / (hard_thr - soft_thr)
                mr = int(GREEN[0] * (1 - t) + r * t)
                mg = int(GREEN[1] * (1 - t) + g * t)
                mb = int(GREEN[2] * (1 - t) + b * t)
                po[x, y] = (mr, mg, mb, 255)
            else:
                po[x, y] = (r, g, b, 255)

    out.save(output_path)
    return output_path


# ============================================================
# Seedream 立绘生成
# ============================================================

def seedream_generate(prompt, save_path, reference_image=None, seed=42):
    """调用火山引擎 Seedream 生成图片"""
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {SEEDREAM_API_KEY}"
    }
    body = {
        "model": SEEDREAM_MODEL,
        "prompt": prompt,
        "size": "2k",
        "response_format": "b64_json",
        "watermark": False,
        "seed": seed
    }
    if reference_image:
        with open(reference_image, "rb") as f:
            img_b64 = base64.b64encode(f.read()).decode()
        ext = os.path.splitext(reference_image)[1].lower()
        mime = {"png": "image/png", "jpg": "image/jpeg", "jpeg": "image/jpeg"}.get(ext, "image/png")
        body["image"] = f"data:{mime};base64,{img_b64}"

    data = json.dumps(body).encode("utf-8")
    req = urllib.request.Request(SEEDREAM_API_URL, data=data, headers=headers)
    with urllib.request.urlopen(req, timeout=120) as resp:
        result = json.loads(resp.read())

    for img in result.get("data", []):
        if "b64_json" in img:
            raw = base64.b64decode(img["b64_json"])
            with open(save_path, "wb") as f:
                f.write(raw)
            return True
    return False


# ============================================================
# ComfyUI H3 视频生成
# ============================================================

def comfy_post(url, payload):
    req = urllib.request.Request(url, data=json.dumps(payload).encode(),
                                  headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read())

def comfy_get(url):
    with urllib.request.urlopen(url, timeout=60) as r:
        return json.loads(r.read())

def h3_submit(first_frame_name, prompt, output_prefix, seed=31):
    """提交 H3 视频工作流（单首帧锁 + 8步turbo）"""
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
            "video": ["91", 0], "filename_prefix": output_prefix,
            "format": "mp4", "codec": "auto"}},
    }
    resp = comfy_post(f"{COMFY_URL}/prompt", {"prompt": WF})
    if "error" in resp or resp.get("node_errors"):
        return None
    return resp["prompt_id"]

def h3_wait(pid, timeout=900):
    """等待 H3 视频生成完成"""
    start = time.time()
    while True:
        time.sleep(10)
        try:
            h = comfy_get(f"{COMFY_URL}/history/{pid}")
        except Exception:
            continue
        entry = h.get(pid)
        if not entry:
            if time.time() - start > timeout:
                return None
            continue
        if entry.get("status", {}).get("status_str") == "error":
            return None
        for nid, out in entry.get("outputs", {}).items():
            for item in out.get("videos", []):
                fname = item.get("filename")
                if fname and fname.endswith(".mp4"):
                    return os.path.join(COMFY_OUT, item.get("subfolder", ""), fname)
        if time.time() - start > timeout:
            return None


# ============================================================
# 抽帧 + 色键抠绿
# ============================================================

def chroma_key_green(img):
    """自适应抠绿：先采样四角背景色，再按与背景色相似度抠图。
    解决H3视频后期背景色偏移问题。"""
    w, h = img.size
    px = img.load()
    # 采样四个角区域（各取10x10中位数，避免边缘噪点）
    corners = []
    sz = min(10, w // 4, h // 4)
    for (sx, sy) in [(0, 0), (w - sz, 0), (0, h - sz), (w - sz, h - sz)]:
        rs, gs, bs = [], [], []
        for dy in range(sz):
            for dx in range(sz):
                r, g, b = px[sx + dx, sy + dy][:3]
                rs.append(r); gs.append(g); bs.append(b)
        rs.sort(); gs.sort(); bs.sort()
        mid = len(rs) // 2
        corners.append((rs[mid], gs[mid], bs[mid]))
    # 取四角均值作为背景色
    bg_r = sum(c[0] for c in corners) // 4
    bg_g = sum(c[1] for c in corners) // 4
    bg_b = sum(c[2] for c in corners) // 4

    def color_dist(r, g, b):
        return ((int(r) - bg_r) ** 2 + (int(g) - bg_g) ** 2 + (int(b) - bg_b) ** 2) ** 0.5

    # 动态阈值：背景越暗/越灰，阈值越小
    bg_lum = (bg_r + bg_g + bg_b) / 3
    hard_thr = max(60, bg_lum * 0.6)     # 完全透明阈值
    soft_thr  = max(25, hard_thr * 0.35)  # 开始过渡阈值

    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    po = out.load()
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            dist = color_dist(r, g, b)
            if dist < soft_thr:
                po[x, y] = (r, g, b, 0)
            elif dist < hard_thr:
                alpha = int((dist - soft_thr) / (hard_thr - soft_thr) * 255)
                po[x, y] = (r, g, b, min(255, alpha))
            else:
                # 猫身上去除残留绿色溢出（绿色通道比红蓝均值高出12以上时修正）
                if g > r + 12 and g > b + 12 and (int(g) - max(int(r), int(b))) > 15:
                    g2 = max(r, b)
                    po[x, y] = (r, g2, b, 255)
                else:
                    po[x, y] = (r, g, b, 255)
    return out

def extract_and_process(video_path, output_dir, target_fps=6, frame_size=512):
    """从视频抽帧 -> 色键抠绿 -> 缩放 -> 保存序列帧"""
    os.makedirs(output_dir, exist_ok=True)
    raw_dir = os.path.join(output_dir, "_raw")
    os.makedirs(raw_dir, exist_ok=True)

    # ffmpeg 抽帧
    subprocess.run([FFMPEG, "-y", "-i", video_path, "-vf", f"fps={target_fps}",
                    os.path.join(raw_dir, "f_%04d.png")], capture_output=True)
    raw_files = sorted([f for f in os.listdir(raw_dir) if f.endswith(".png")])

    # 色键抠绿 + 缩放
    for i, fname in enumerate(raw_files):
        img = Image.open(os.path.join(raw_dir, fname)).convert("RGBA")
        result = chroma_key_green(img)
        result = result.resize((frame_size, frame_size), Image.LANCZOS)
        result.save(os.path.join(output_dir, f"f_{i:04d}.png"))

    shutil.rmtree(raw_dir)
    return len(raw_files)


# ============================================================
# 主流程
# ============================================================

def process_cat(cat_config):
    """处理一只猫的所有表情"""
    cat_name = cat_config["name"]
    portrait = cat_config["portrait"]
    expressions = cat_config["expressions"]
    output_base = cat_config["output_base"]

    print(f"=== Cat: {cat_name} ===")
    print(f"Portrait: {portrait}")
    print(f"Expressions: {list(expressions.keys())}\n")

    for exp_name, pose_desc in expressions.items():
        print(f"[{exp_name}]")

        # 1. Seedream 生成立绘（图生图）
        seedream_prompt = SEEDREAM_POSE_TEMPLATE.format(pose=pose_desc)
        first_frame_path = os.path.join(COMFY_INPUT, f"{cat_name}_{exp_name}_firstframe.png")
        seedream_save = os.path.join(COMFY_INPUT, f"{cat_name}_{exp_name}_seedream.png")

        print(f"  Generating portrait via Seedream...")
        try:
            seedream_generate(seedream_prompt, seedream_save, reference_image=portrait, seed=42)
        except Exception as e:
            print(f"  Seedream failed: {e}")
            continue

        # 复制到 ComfyUI input 作为 H3 首帧
        shutil.copy2(seedream_save, first_frame_path)

        # 2. H3 生成绿幕视频
        h3_prompt = H3_VIDEO_PROMPT_TEMPLATE.format(pose=pose_desc)
        output_prefix = f"{cat_name}_{exp_name}"

        print(f"  Submitting H3 video...")
        pid = h3_submit(os.path.basename(first_frame_path), h3_prompt, output_prefix, seed=31)
        if not pid:
            print(f"  H3 submit failed")
            continue
        print(f"  PID: {pid}")

        print(f"  Waiting for video (~3min)...")
        video_path = h3_wait(pid, timeout=900)
        if not video_path:
            print(f"  H3 video failed")
            continue
        print(f"  Video: {os.path.basename(video_path)}")

        # 3. 抽帧 + 色键抠绿
        frame_dir = os.path.join(output_base, cat_name, exp_name)
        print(f"  Extracting frames...")
        count = extract_and_process(video_path, frame_dir, target_fps=6, frame_size=512)
        print(f"  Done: {count} frames -> {frame_dir}\n")

    print(f"=== {cat_name} All Done ===")


# ============================================================
# 入口
# ============================================================

if __name__ == "__main__":
    process_cat(CAT_CONFIG)
