# -*- coding: utf-8 -*-
# 隐藏成就问号图标：C2 水墨小景风格
import json, shutil, time, urllib.request, os
from PIL import Image

COMFY = 'http://127.0.0.1:8188'
OUT_ROOT = r'H:\ComfyUI_windows_portable\ComfyUI\output'
DST_DIR = r'E:\UnityProject\wuziqi\Assets\Art\Achievements'
os.makedirs(DST_DIR, exist_ok=True)

PROMPT = ('Chinese shuimo ink wash painting, xieyi freehand brush work, hand painted on rice paper, '
          'a single large question mark painted with one loose dark ink brush stroke, '
          'expressive dry brush texture with flying white, large areas of blank space, '
          'minimalist composition, centered, no text, no seal stamp')
NEG = ('realistic, photorealistic, photo, 3d render, texture detail, wood grain, carved, engraved, '
       'ornate, intricate details, heavy, dark background, colored background, border, frame, '
       'text, letters, seal stamp, multiple objects, busy, thick paint, oil painting, glossy')


def post(url, payload):
    req = urllib.request.Request(url, data=json.dumps(payload).encode(),
                                 headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read())


def get(url):
    with urllib.request.urlopen(url, timeout=60) as r:
        return json.loads(r.read())


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
    '252': {'class_type': 'EmptySD3LatentImage', 'inputs': {'width': 1024, 'height': 1024, 'batch_size': 1}},
    '253': {'class_type': 'KSampler', 'inputs': {
        'model': ['247', 0], 'positive': ['249p', 0], 'negative': ['249n', 0],
        'latent_image': ['252', 0], 'seed': 118,
        'steps': 4, 'cfg': 1.0, 'sampler_name': 'euler', 'scheduler': 'simple', 'denoise': 1.0}},
    '251': {'class_type': 'VAEDecode', 'inputs': {'samples': ['253', 0], 'vae': ['246', 0]}},
    '255': {'class_type': 'SaveImage', 'inputs': {'images': ['251', 0], 'filename_prefix': 'ach_hidden'}},
}


def main():
    resp = post(COMFY + '/prompt', {'prompt': WF})
    if 'error' in resp or resp.get('node_errors'):
        print('ERROR: ' + str(resp)[:500]); exit(1)
    pid = resp['prompt_id']
    print('prompt_id:', pid)

    start = time.time()
    fname = None
    while fname is None and time.time() - start < 300:
        time.sleep(4)
        try: h = get(COMFY + '/history/' + pid)
        except Exception: continue
        entry = h.get(pid)
        if not entry: continue
        if entry.get('status', {}).get('status_str') == 'error':
            print('EXEC ERROR'); exit(1)
        for nid, out in entry.get('outputs', {}).items():
            for item in out.get('images', []):
                f = item.get('filename', '')
                if f.endswith('.png'): fname = f

    src = os.path.join(OUT_ROOT, fname)
    dst = os.path.join(DST_DIR, 'ach_hidden.png')
    im = Image.open(src).convert('RGB').resize((256, 256), Image.LANCZOS)
    im.save(dst)
    print('saved:', dst)
    print('DONE')


if __name__ == '__main__':
    main()
