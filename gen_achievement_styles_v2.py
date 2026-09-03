# -*- coding: utf-8 -*-
# 成就图标风格验证 v2：压写实、强水墨手绘（留白+枯笔+淡彩）
import json, shutil, time, urllib.request, os
from PIL import Image

COMFY = 'http://127.0.0.1:8188'
OUT_ROOT = r'H:\ComfyUI_windows_portable\ComfyUI\output'
WORK = r'E:\UnityProject\wuziqi\_ach_styles'
os.makedirs(WORK, exist_ok=True)

BASE = ('Chinese shuimo ink wash painting, xieyi freehand brush work, hand painted on rice paper, '
        'a single young sprout with two small leaves breaking through soil, '
        'large areas of blank space, minimalist composition, soft ink gradation from dark to light, '
        'a few loose expressive brush strokes, slight ink splashes, no outline sketch, no realistic texture, '
        'centered composition, no text')

STYLES = {
    'A2_seal': ('a square red seal stamp shape drawn with loose cinnabar brush strokes, the sprout painted '
                'in white negative space inside the red seal, edges of the seal are rough and bleeding, '
                'hand painted not carved, flat 2d', 52),
    'B2_round': ('a single large enso-style ink circle painted with one dry brush stroke, the sprout inside '
                 'the circle with light green tint, most of the circle is blank rice paper, '
                 'the ink ring is broken and uneven, very loose and sketchy', 53),
    'C2_scroll': ('the sprout painted large and loose with dark ink leaves and a light green wash, '
                  'a few bamboo leaves falling around it, tiny red seal stamp in the corner, '
                  'everything on blank rice paper, no border no frame', 54),
}

NEG = ('realistic, photorealistic, photo, 3d render, texture detail, stone texture, wood grain, '
       'carved, engraved, ornate, intricate details, heavy, dark background, colored background, '
       'text, letters, multiple objects, busy, complex, thick paint, oil painting, glossy')


def post(url, payload):
    req = urllib.request.Request(url, data=json.dumps(payload).encode(),
                                 headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read())


def get(url):
    with urllib.request.urlopen(url, timeout=60) as r:
        return json.loads(r.read())


def make_wf(prompt, neg, seed):
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
        '249p': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['245', 0], 'text': prompt}},
        '249n': {'class_type': 'CLIPTextEncode', 'inputs': {'clip': ['245', 0], 'text': neg}},
        '252': {'class_type': 'EmptySD3LatentImage', 'inputs': {'width': 1024, 'height': 1024, 'batch_size': 1}},
        '253': {'class_type': 'KSampler', 'inputs': {
            'model': ['247', 0], 'positive': ['249p', 0], 'negative': ['249n', 0],
            'latent_image': ['252', 0], 'seed': seed,
            'steps': 4, 'cfg': 1.0, 'sampler_name': 'euler', 'scheduler': 'simple', 'denoise': 1.0}},
        '251': {'class_type': 'VAEDecode', 'inputs': {'samples': ['253', 0], 'vae': ['246', 0]}},
        '255': {'class_type': 'SaveImage', 'inputs': {'images': ['251', 0], 'filename_prefix': 'ach_style2'}},
    }


def main():
    pids = {}
    for key, (style, seed) in STYLES.items():
        prompt = f'{BASE}, {style}'
        resp = post(COMFY + '/prompt', {'prompt': make_wf(prompt, NEG, seed)})
        if 'error' in resp or resp.get('node_errors'):
            print(f'ERROR {key}: ' + str(resp)[:500]); exit(1)
        pids[key] = resp['prompt_id']
        print(f'{key} submitted')

    files = {}
    start = time.time()
    while len(files) < len(STYLES) and time.time() - start < 300:
        time.sleep(4)
        for key, pid in pids.items():
            if key in files: continue
            try: h = get(COMFY + '/history/' + pid)
            except Exception: continue
            entry = h.get(pid)
            if not entry: continue
            if entry.get('status', {}).get('status_str') == 'error':
                print(f'EXEC ERROR {key}'); exit(1)
            for nid, out in entry.get('outputs', {}).items():
                for item in out.get('images', []):
                    fname = item.get('filename', '')
                    if fname.endswith('.png'): files[key] = fname

    sheet = Image.new('RGB', (1024 * 3 + 40 * 4, 1064), (245, 240, 228))
    x = 40
    for key in ['A2_seal', 'B2_round', 'C2_scroll']:
        src = os.path.join(OUT_ROOT, files[key])
        dst = os.path.join(WORK, f'style_{key}.png')
        shutil.copy2(src, dst)
        im = Image.open(dst).convert('RGB').resize((1024, 1024), Image.LANCZOS)
        sheet.paste(im, (x, 20))
        x += 1024 + 40
        print('saved:', dst)
    sheet = sheet.resize((sheet.width // 3, sheet.height // 3), Image.LANCZOS)
    sheet_path = os.path.join(WORK, 'style_sheet_v2.jpg')
    sheet.save(sheet_path, quality=85)
    print('sheet:', sheet_path)
    print('DONE')


if __name__ == '__main__':
    main()
