import json, shutil, time, urllib.request, os, sys

COMFY = "http://127.0.0.1:8188"
OUT_ROOT = r"H:\ComfyUI_windows_portable\ComfyUI\output"

PROMPT = (
    "A chubby orange tabby kitten in Chinese ink wash painting style, chibi kawaii art. "
    "The kitten is sitting with one paw raised to its chin in a thinking pose, "
    "eyes looking upward with a curious puzzled expression, "
    "orange and cream white fur with tabby stripes, wearing a red collar with small gold bell. "
    "Pure green chroma-key background #00FF00, solid flat green, no texture no gradient. "
    "Game character expression sheet, cute, soft watercolor ink brush strokes, "
    "centered composition, no text no watermark."
)

WF = {
    "248": {"class_type": "UnetLoaderGGUF", "inputs": {"unet_name": "qwen-image-2512-Q4_K_M.gguf"}},
    "259": {"class_type": "LoraLoaderModelOnly", "inputs": {
        "model": ["248", 0],
        "lora_name": "Qwen-Image-2512-Lightning-4steps-V1.0-bf16.safetensors",
        "strength_model": 1.0}},
    "247": {"class_type": "ModelSamplingAuraFlow", "inputs": {"model": ["259", 0], "shift": 3.1}},
    "245": {"class_type": "CLIPLoader", "inputs": {
        "clip_name": "qwen_2.5_vl_7b_fp8_scaled.safetensors",
        "type": "qwen_image", "device": "default"}},
    "246": {"class_type": "VAELoader", "inputs": {"vae_name": "qwen_image_vae.safetensors"}},
    "249p": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["245", 0], "text": PROMPT}},
    "249n": {"class_type": "CLIPTextEncode", "inputs": {"clip": ["245", 0],
        "text": "low quality, blurry, distorted, extra objects, text, watermark, complex background, realistic, photorealistic"}},
    "252": {"class_type": "EmptySD3LatentImage", "inputs": {"width": 1024, "height": 1024, "batch_size": 1}},
    "253": {"class_type": "KSampler", "inputs": {
        "model": ["247", 0], "positive": ["249p", 0], "negative": ["249n", 0],
        "latent_image": ["252", 0], "seed": 42,
        "steps": 4, "cfg": 4.0,
        "sampler_name": "euler", "scheduler": "simple", "denoise": 1.0}},
    "251": {"class_type": "VAEDecode", "inputs": {"samples": ["253", 0], "vae": ["246", 0]}},
    "255": {"class_type": "SaveImage", "inputs": {"images": ["251", 0], "filename_prefix": "juzuo_thinking"}},
}

def post(url, payload):
    req = urllib.request.Request(url, data=json.dumps(payload).encode(),
                                  headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read())

def get(url):
    with urllib.request.urlopen(url, timeout=60) as r:
        return json.loads(r.read())

print("Submitting workflow...")
resp = post(f"{COMFY}/prompt", {"prompt": WF})
if "error" in resp or resp.get("node_errors"):
    print("REJECTED:", json.dumps(resp, ensure_ascii=False)[:1000])
    sys.exit(1)
pid = resp["prompt_id"]
print(f"prompt_id: {pid}")

start = time.time()
while True:
    time.sleep(5)
    try:
        h = get(f"{COMFY}/history/{pid}")
    except Exception:
        continue
    entry = h.get(pid)
    if not entry:
        elapsed = int(time.time() - start)
        print(f"Waiting... {elapsed}s")
        if elapsed > 300:
            print("TIMEOUT")
            sys.exit(1)
        continue
    if entry.get("status", {}).get("status_str") == "error":
        print("EXEC ERROR:", entry["status"].get("messages"))
        sys.exit(1)
    for nid, out in entry.get("outputs", {}).items():
        for item in out.get("images", []):
            fname = item.get("filename")
            if fname and fname.endswith(".png"):
                src = os.path.join(OUT_ROOT, item.get("subfolder", ""), fname)
                save_to = r"e:\UnityProject\wuziqi\Assets\Art\Cat\juzuo_thinking_test.png"
                shutil.copy2(src, save_to)
                print(f"Done! Saved to {save_to}")
                print(f"Source: {src}")
                sys.exit(0)
    if time.time() - start > 300:
        print("TIMEOUT")
        sys.exit(1)
