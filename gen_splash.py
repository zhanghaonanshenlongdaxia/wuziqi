import json, shutil, time, urllib.request, os

COMFY = 'http://127.0.0.1:8188'
OUT_ROOT = r'H:\ComfyUI_windows_portable\ComfyUI\output'
SAVE_DIR = r'E:\UnityProject\wuziqi\Assets\Art\Icon'
os.makedirs(SAVE_DIR, exist_ok=True)

def post(url, payload):
    req = urllib.request.Request(url, data=json.dumps(payload).encode(),
                                  headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read())

def get(url):
    with urllib.request.urlopen(url, timeout=60) as r:
        return json.loads(r.read())

# 启动画面：横版，水墨山水 + 猫咪剪影 + 棋盘元素
PROMPT = 'A wide landscape splash screen for a mobile game, traditional Chinese ink wash painting style, serene mountains and clouds in misty ink wash, a silhouetted cute cat sitting on a wooden bridge overlooking a misty valley, scattered Gomoku stones floating like falling leaves, warm golden sunrise light breaking through clouds, ethereal and magical atmosphere, rice paper texture, elegant calligraphy brush strokes, peaceful zen mood, no text, high quality art'
NEG = 'low quality, blurry, distorted, text, watermark, realistic photo, 3d render, dark, scary, ugly, busy, cluttered'

WF = {
    '248': {'class_type': 'UnetLoaderGGUF', 'inputs': {'unet_name': 'qwen-image-2512-Q4_K_M.gguf'}},
    '259': {'class_type': 'LoraLoaderModelOnly', 'inputs': {
        'model': ['248', 0],
        'lora_name': 'Qwen-Image-2512-Lightning-4steps-V1.0-bf16.safetensors',
        'strength_model': 1.0}},
    '247': {'class_type': 'ModelSamplingAuraFlow', 'inputs': {'model': ['259', 0], 'shift': 3.1}},
    '245': {'class_type': 'CLIPLoader', 'inputs': {
        'clip_name': 'qwen_2.5_vl_7b_fp8_scaled.safetensors',
        'type': 'qwen_image', 'device': 'default'}},
    '246': {'class_type': 'VAELoader', 'inputs': {'vae_name': 'qwen_image_vae.safetensors'}},
    '249p': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['245', 0], 'text': PROMPT}},
    '249n': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['245', 0], 'text': NEG}},
    '252': {'class_type': 'EmptySD3LatentImage', 'inputs': {'width': 1920, 'height': 1080, 'batch_size': 1}},
    '253': {'class_type': 'KSampler', 'inputs': {
        'model': ['247', 0], 'positive': ['249p', 0], 'negative': ['249n', 0],
        'latent_image': ['252', 0], 'seed': 88,
        'steps': 4, 'cfg': 1.0,
        'sampler_name': 'euler', 'scheduler': 'simple', 'denoise': 1.0}},
    '254': {'class_type': 'VAEDecode', 'inputs': {'samples': ['253', 0], 'vae': ['246', 0]}},
    '255': {'class_type': 'SaveImage', 'inputs': {'images': ['254', 0], 'filename_prefix': 'splash_screen'}},
}

print('Submitting splash screen workflow...')
resp = post(COMFY + '/prompt', {'prompt': WF})
if 'error' in resp or resp.get('node_errors'):
    print('ERROR: ' + str(resp)[:500])
    exit(1)
pid = resp['prompt_id']
print('prompt_id: ' + pid)

start = time.time()
while True:
    time.sleep(5)
    try:
        h = get(COMFY + '/history/' + pid)
    except:
        continue
    entry = h.get(pid)
    if not entry:
        if time.time() - start > 300:
            print('TIMEOUT')
            exit(1)
        continue
    status = entry.get('status', {}).get('status_str', '')
    if status == 'error':
        print('EXEC ERROR')
        exit(1)
    for nid, out in entry.get('outputs', {}).items():
        for item in out.get('images', []):
            fname = item.get('filename', '')
            if fname.endswith('.png'):
                src = os.path.join(OUT_ROOT, item.get('subfolder', ''), fname)
                dst = os.path.join(SAVE_DIR, 'splash_screen.png')
                shutil.copy2(src, dst)
                print('Saved: ' + dst)
                print('Done in ' + str(int(time.time()-start)) + 's')
                exit(0)
    if time.time() - start > 300:
        print('TIMEOUT')
        exit(1)
