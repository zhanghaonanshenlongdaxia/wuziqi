# -*- coding: utf-8 -*-
# 成就图标批量生成：C2 无框水墨小景风格（无印章），17 个成就
import json, shutil, time, urllib.request, os
from PIL import Image

COMFY = 'http://127.0.0.1:8188'
OUT_ROOT = r'H:\ComfyUI_windows_portable\ComfyUI\output'
WORK = r'E:\UnityProject\wuziqi\_ach_icons'
os.makedirs(WORK, exist_ok=True)

BASE = ('Chinese shuimo ink wash painting, xieyi freehand brush work, hand painted on rice paper, '
        'large areas of blank space, minimalist composition, soft ink gradation from dark to light, '
        'a few loose expressive brush strokes, slight ink splashes, no outline sketch, no realistic texture, '
        'centered composition, no text, no seal stamp')

NEG = ('realistic, photorealistic, photo, 3d render, texture detail, wood grain, carved, engraved, '
       'ornate, intricate details, heavy, dark background, colored background, border, frame, '
       'text, letters, seal stamp, multiple scenes, busy, thick paint, oil painting, glossy')

# (id, 主题元素, seed)
ICONS = [
    ('win_1',    'a single young sprout with two small leaves breaking through soil', 101),
    ('win_5',    'a taller young plant with four lush leaves standing proud', 102),
    ('win_20',   'a young plant with a single blooming flower at its top', 103),
    ('win_50',   'a majestic old pine tree with dense expressive foliage', 104),
    ('game_50',  'a solid rock with water drops, water dripping through the stone', 105),
    ('streak_3', 'three plum blossoms blooming on a single branch', 106),
    ('streak_5', 'five plum blossoms blooming on a branch', 107),
    ('streak_10','a branch fully covered with blooming plum blossoms', 108),
    ('cats_3',   'three cat silhouettes sitting in a row, simple ink shapes', 109),
    ('cats_5',   'five cat silhouettes arranged in a loose circle, simple ink shapes', 110),
    ('cats_7',   'seven cat silhouettes of different sizes walking together, simple ink shapes', 111),
    ('song_3',   'a Chinese guqin zither lying flat, strings visible', 112),
    ('same_20',  'two cats sitting face to face with a small teapot between them', 113),
    ('beat_all', 'a single large cat paw print mark pressed into the paper', 114),
    ('coin_100', 'a string of ancient Chinese copper coins tied with a small cord', 115),
    ('coin_500', 'a pile of ancient Chinese copper coins scattered loosely', 116),
    ('lose_5',   'a cat lying flat on its back completely exhausted, legs up', 117),
]


def post(url, payload):
    req = urllib.request.Request(url, data=json.dumps(payload).encode(),
                                 headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read())


def get(url):
    with urllib.request.urlopen(url, timeout=60) as r:
        return json.loads(r.read())


def make_wf(subject, seed, id0):
    prompt = f'{BASE}, {subject}'
    neg = NEG
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
        '255': {'class_type': 'SaveImage', 'inputs': {'images': ['251', 0], 'filename_prefix': f'ach_{id0}'}},
    }


def main():
    # 提交全部 17 个（ComfyUI 内部排队串行）
    pids = {}
    for id0, subject, seed in ICONS:
        resp = post(COMFY + '/prompt', {'prompt': make_wf(subject, seed, id0)})
        if 'error' in resp or resp.get('node_errors'):
            print(f'ERROR {id0}: ' + str(resp)[:500]); exit(1)
        pids[id0] = resp['prompt_id']
        print(f'{id0} submitted')
        time.sleep(0.3)

    # 收集
    files = {}
    start = time.time()
    while len(files) < len(ICONS) and time.time() - start < 1500:
        time.sleep(6)
        for id0, pid in pids.items():
            if id0 in files: continue
            try: h = get(COMFY + '/history/' + pid)
            except Exception: continue
            entry = h.get(pid)
            if not entry: continue
            if entry.get('status', {}).get('status_str') == 'error':
                print(f'EXEC ERROR {id0}'); files[id0] = 'ERROR'; break
            for nid, out in entry.get('outputs', {}).items():
                for item in out.get('images', []):
                    fname = item.get('filename', '')
                    if fname.endswith('.png'): files[id0] = fname
        print(f'progress: {len(files)}/{len(ICONS)} ({int(time.time()-start)}s)')
    if len(files) < len(ICONS):
        print('TIMEOUT, got:', list(files.keys())); exit(1)

    # 拷贝 + 拼网格
    cols, cell = 5, 512
    rows = (len(ICONS) + cols - 1) // cols
    sheet = Image.new('RGB', (cols * cell + 20 * (cols + 1), rows * cell + 20 * (rows + 1)), (238, 231, 216))
    for i, (id0, subject, seed) in enumerate(ICONS):
        src = os.path.join(OUT_ROOT, files[id0])
        dst = os.path.join(WORK, f'{id0}.png')
        shutil.copy2(src, dst)
        im = Image.open(dst).convert('RGB').resize((cell, cell), Image.LANCZOS)
        r, c = divmod(i, cols)
        sheet.paste(im, (20 + c * (cell + 20), 20 + r * (cell + 20)))
    sheet = sheet.resize((sheet.width // 2, sheet.height // 2), Image.LANCZOS)
    sheet_path = os.path.join(WORK, 'all_icons_sheet.jpg')
    sheet.save(sheet_path, quality=85)
    print('sheet:', sheet_path)
    print('DONE')


if __name__ == '__main__':
    main()
