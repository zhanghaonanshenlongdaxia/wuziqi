"""
生成工作室素材：
1. studio_logo.png  - 工作室 Logo 图标（龙+猫融合，正方形）
2. studio_name.png  - "苍龙七修" 艺术字（透明背景）
3. main_bg.png      - 主界面背景（水墨山水，无手机框，全屏）
"""
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

def wait_result(pid, filename_prefix, timeout=300):
    start = time.time()
    while True:
        time.sleep(5)
        try:
            h = get(COMFY + '/history/' + pid)
        except:
            continue
        entry = h.get(pid)
        if not entry:
            if time.time() - start > timeout:
                print(f'TIMEOUT for {filename_prefix}')
                return None
            continue
        status = entry.get('status', {}).get('status_str', '')
        if status == 'error':
            print(f'EXEC ERROR for {filename_prefix}: ' + str(entry.get('status', ''))[:300])
            return None
        for nid, out in entry.get('outputs', {}).items():
            for item in out.get('images', []):
                fname = item.get('filename', '')
                if fname.endswith('.png'):
                    src = os.path.join(OUT_ROOT, item.get('subfolder', ''), fname)
                    dst = os.path.join(SAVE_DIR, filename_prefix + '.png')
                    shutil.copy2(src, dst)
                    print(f'Saved: {dst}  ({int(time.time()-start)}s)')
                    return dst
        if time.time() - start > timeout:
            print(f'TIMEOUT for {filename_prefix}')
            return None

# 共享模型节点
def base_nodes():
    return {
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
    }

def make_workflow(prompt, neg, width, height, seed, filename_prefix):
    nodes = base_nodes()
    nodes['clip_pos'] = {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['245', 0], 'text': prompt}}
    nodes['clip_neg'] = {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['245', 0], 'text': neg}}
    nodes['latent'] = {'class_type': 'EmptySD3LatentImage', 'inputs': {'width': width, 'height': height, 'batch_size': 1}}
    nodes['sampler'] = {'class_type': 'KSampler', 'inputs': {
        'model': ['247', 0], 'positive': ['clip_pos', 0], 'negative': ['clip_neg', 0],
        'latent_image': ['latent', 0], 'seed': seed,
        'steps': 4, 'cfg': 1.0,
        'sampler_name': 'euler', 'scheduler': 'simple', 'denoise': 1.0}}
    nodes['decode'] = {'class_type': 'VAEDecode', 'inputs': {'samples': ['sampler', 0], 'vae': ['246', 0]}}
    nodes['save'] = {'class_type': 'SaveImage', 'inputs': {'images': ['decode', 0], 'filename_prefix': filename_prefix}}
    return nodes

NEG = 'low quality, blurry, distorted, text, watermark, realistic photo, 3d render, dark, scary, ugly'

# ── 1. 工作室 Logo ──
LOGO_PROMPT = (
    'A square game studio logo icon, featuring a mystical dragon and cat fusion creature, '
    'Chinese ink wash painting style, the dragon coils around a cute dignified cat sitting on clouds, '
    'seven stars of the Azure Dragon constellation glowing in the background, '
    'circular emblem design, gold and deep blue color scheme, elegant and magical, '
    'clean design suitable for app icon, white background, high quality, detailed'
)

# ── 2. 艺术字 "苍龙七修" ──
NAME_PROMPT = (
    'Chinese calligraphy art text "苍龙七修" written with a large ink brush, '
    'bold powerful brush strokes, traditional Chinese calligraphy style (行书), '
    'black ink on pure white background, the four characters arranged horizontally, '
    'elegant and artistic, with subtle ink splatter effects, '
    'high contrast, clean white background, no other elements, calligraphy masterwork'
)

# ── 3. 主界面背景（无手机框）──
BG_PROMPT = (
    'A wide landscape background for a mobile game main menu, traditional Chinese ink wash painting style, '
    'serene mountains and clouds in misty ink wash, bamboo forest on the left, '
    'a gentle stream flowing through a valley, scattered Go/Gomoku stones on a flat rock, '
    'warm golden sunrise light breaking through clouds, ethereal and magical atmosphere, '
    'rice paper texture, elegant calligraphy brush strokes, peaceful zen mood, '
    'no text, no UI elements, no phone frame, seamless full screen game background, high quality art'
)

jobs = [
    (LOGO_PROMPT, NEG, 1024, 1024, 42, 'studio_logo'),
    (NAME_PROMPT, NEG, 1024, 512, 43, 'studio_name'),
    (BG_PROMPT, NEG, 1080, 1920, 44, 'main_bg'),   # 竖屏手机比例
]

for prompt, neg, w, h, seed, prefix in jobs:
    print(f'\n=== Generating {prefix} ({w}x{h}) ===')
    wf = make_workflow(prompt, neg, w, h, seed, prefix)
    resp = post(COMFY + '/prompt', {'prompt': wf})
    if 'error' in resp or resp.get('node_errors'):
        print(f'ERROR submitting {prefix}: ' + str(resp)[:500])
        continue
    pid = resp['prompt_id']
    print(f'prompt_id: {pid}')
    result = wait_result(pid, prefix)
    if not result:
        print(f'FAILED: {prefix}')

print('\n=== All done ===')
