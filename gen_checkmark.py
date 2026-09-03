# -*- coding: utf-8 -*-
# 水墨对号图标生成：Qwen-Image 本地工作流（复用 gen_icon_v3 管线）
# 白底生成 -> 亮度抠图(thr245/soft45) -> 紧裁剪 -> 候选 png + 对比图
import json, shutil, time, urllib.request, os
from PIL import Image

COMFY = 'http://127.0.0.1:8188'
OUT_ROOT = r'H:\ComfyUI_windows_portable\ComfyUI\output'
WORK = r'E:\UnityProject\wuziqi\_check_candidates'
RAW = os.path.join(WORK, 'raw')
os.makedirs(RAW, exist_ok=True)

PROMPT = ('中国传统水墨画风格：一个用毛笔写出的对勾符号（勾选标记 ✓），一笔写成，'
          '笔触苍劲有力，墨色浓郁，笔锋自然，深墨黑色，纯白色背景，'
          '符号居中且占画面一半大小，构图简洁，只有这一个符号，没有其他任何元素')
NEG = ('文字, 汉字, 字母, 数字, 多个符号, 彩色, 灰色背景, 阴影, 投影, 边框, '
       '装饰框, 花纹, 复杂背景, 渐变背景, 模糊, 倾斜出画面')

SEED = 77
BATCH = 4
PREFIX = 'icon_check_v1'


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
    '252': {'class_type': 'EmptySD3LatentImage', 'inputs': {'width': 1024, 'height': 1024, 'batch_size': BATCH}},
    '253': {'class_type': 'KSampler', 'inputs': {
        'model': ['247', 0], 'positive': ['249p', 0], 'negative': ['249n', 0],
        'latent_image': ['252', 0], 'seed': SEED,
        'steps': 4, 'cfg': 1.0,
        'sampler_name': 'euler', 'scheduler': 'simple', 'denoise': 1.0}},
    '254': {'class_type': 'VAEDecode', 'inputs': {'samples': ['253', 0], 'vae': ['246', 0]}},
    '255': {'class_type': 'SaveImage', 'inputs': {'images': ['254', 0], 'filename_prefix': PREFIX}},
}


def key_out_white(im, thr=245, soft=45):
    """白底亮度抠图：lum>=thr 全透明，<=thr-soft 全不透明，中间线性过渡。保留原 RGB（墨色）。"""
    im = im.convert('RGB')
    px = im.load()
    w, h = im.size
    out = Image.new('RGBA', (w, h))
    opx = out.load()
    lo = thr - soft
    for y in range(h):
        for x in range(w):
            r, g, b = px[x, y]
            lum = 0.299 * r + 0.587 * g + 0.114 * b
            if lum >= thr:
                a = 0
            elif lum <= lo:
                a = 255
            else:
                a = int(255 * (thr - lum) / soft)
            opx[x, y] = (r, g, b, a)
    return out


def tight_crop(im, pad=10):
    bbox = im.getchannel('A').getbbox()
    if not bbox:
        return im
    l, t, r, b = bbox
    l = max(0, l - pad); t = max(0, t - pad)
    r = min(im.width, r + pad); b = min(im.height, b + pad)
    return im.crop((l, t, r, b))


def main():
    print('submitting...')
    resp = post(COMFY + '/prompt', {'prompt': WF})
    if 'error' in resp or resp.get('node_errors'):
        print('ERROR: ' + str(resp)[:800]); exit(1)
    pid = resp['prompt_id']
    print('prompt_id:', pid)

    start = time.time()
    files = []
    while time.time() - start < 300:
        time.sleep(4)
        try:
            h = get(COMFY + '/history/' + pid)
        except Exception:
            continue
        entry = h.get(pid)
        if not entry:
            continue
        status = entry.get('status', {}).get('status_str', '')
        if status == 'error':
            print('EXEC ERROR'); exit(1)
        for nid, out in entry.get('outputs', {}).items():
            for item in out.get('images', []):
                fname = item.get('filename', '')
                if fname.endswith('.png') and fname not in files:
                    files.append(fname)
        if len(files) >= BATCH:
            break
    if len(files) < BATCH:
        print('TIMEOUT / incomplete, got', files); exit(1)

    cands = []
    for i, fname in enumerate(sorted(files), 1):
        src = os.path.join(OUT_ROOT, fname)
        keyed = key_out_white(Image.open(src))
        cropped = tight_crop(keyed)
        # 统一到 160x160 方形画布（居中）
        canvas = Image.new('RGBA', (160, 160), (0, 0, 0, 0))
        s = min(140 / cropped.width, 140 / cropped.height)
        im2 = cropped.resize((max(1, int(cropped.width * s)), max(1, int(cropped.height * s))), Image.LANCZOS)
        canvas.paste(im2, ((160 - im2.width) // 2, (160 - im2.height) // 2), im2)
        dst = os.path.join(WORK, f'cand_{i}.png')
        canvas.save(dst)
        cands.append(dst)
        print('candidate:', dst)

    # 对比图（白底方便查看墨色）
    sheet = Image.new('RGB', (160 * len(cands) + 30 * (len(cands) + 1), 220), (245, 240, 228))
    for i, p in enumerate(cands):
        im = Image.open(p)
        sheet.paste(im, (30 + i * 190, 30), im)
    sheet_path = os.path.join(WORK, 'sheet.jpg')
    sheet.save(sheet_path, quality=85)
    print('sheet:', sheet_path)
    print('DONE')


if __name__ == '__main__':
    main()
