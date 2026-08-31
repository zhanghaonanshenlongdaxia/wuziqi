"""批量生成视频并切帧"""
import subprocess, os, json, shutil, time, urllib.request
from PIL import Image
from rembg import remove, new_session

os.environ['OMP_NUM_THREADS'] = '4'
session = new_session('u2net')

COMFY = 'http://127.0.0.1:8188'
OUT_ROOT = r'H:\ComfyUI_windows_portable\ComfyUI\output'
INPUT = r'H:\ComfyUI_windows_portable\ComfyUI\input'
FFMPEG = r'C:\Users\zhn\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0-full_build\bin\ffmpeg.exe'
SRC_DIR = r'e:\UnityProject\wuziqi\Assets\Art\Cat'
DST_ROOT = r'e:\UnityProject\wuziqi\Assets\Art\Cat\Frames'

CAT_DESCS = {
    '花斑': 'a calico cat with brown, black and white mixed patches',
    '银渐层': 'a British Shorthair cat with silver gradient fur, round face and big eyes',
    '玄猫': 'a deep black cat with dense fur and mysterious deep eyes',
    '仙喵长老': 'an ancient immortal cat with white long fur, long eyebrows and whiskers, sage-like appearance',
}

BACKGROUND_MAP = {
    '花斑': 'SOLID BLACK background',
    '银渐层': 'SOLID BLACK background',
    '玄猫': 'SOLID WHITE background',
    '仙喵长老': 'SOLID BLACK background',
}

VIDEO_PROMPTS = {
    'idle': 'sitting calmly, relaxed posture, gentle breathing',
    'thinking': 'sitting, one paw raised to chin in thinking pose',
    'smug': 'sitting, one paw on hip, snickering',
    'worried': 'sitting, fur standing up, ears pressed back, worried',
    'celebrate': 'jumping with both front paws raised high, celebrating',
    'defeat': 'lying flat on ground, defeated',
}

def process_video(cat, exp):
    frame_dir = os.path.join(DST_ROOT, cat, exp)
    if os.path.exists(frame_dir):
        f_count = len([f for f in os.listdir(frame_dir) if f.startswith('f_') and f.endswith('.png')])
        if f_count >= 28:
            print(f'  {exp}: already done ({f_count} frames)')
            return True

    portrait = os.path.join(SRC_DIR, cat, f'{cat}_{exp}_contrast.png')
    if not os.path.exists(portrait):
        print(f'  {exp}: no portrait')
        return False

    first_name = f'{cat}_{exp}_firstframe.png'
    shutil.copy2(portrait, os.path.join(INPUT, first_name))

    prompt = f'Traditional Chinese ink wash painting. {CAT_DESCS[cat]}, wearing red collar with golden bell. {VIDEO_PROMPTS[exp]}. Full body visible, centered, cat fills 70 percent of frame. {BACKGROUND_MAP[cat]}. Ink brush strokes. No text.'

    WF = {
        '6':   {'class_type': 'UNETLoader', 'inputs': {'unet_name': 'minimax_h3_fl2va_pruned_int8_convrot.safetensors', 'weight_dtype': 'default'}},
        '121': {'class_type': 'LoraLoaderModelOnly', 'inputs': {'model': ['6', 0], 'lora_name': 'minimax_h3_fl2v_turbo_8step_v1.0_comfyui_bf16.safetensors', 'strength_model': 1.0}},
        '13':  {'class_type': 'CLIPLoader', 'inputs': {'clip_name': 'qwen3vl_32b_minimax_h3_nvfp4_awq.safetensors', 'type': 'minimax', 'device': 'default'}},
        '11':  {'class_type': 'VAELoader', 'inputs': {'vae_name': 'minimax_h3_video_vae_fp16.safetensors'}},
        '15':  {'class_type': 'RandomNoise', 'inputs': {'noise_seed': 31, 'control_after_generate': 'fixed'}},
        '200': {'class_type': 'LoadImage', 'inputs': {'image': first_name, 'upload': 'image'}},
        '104': {'class_type': 'MiniMaxH3ImageToVideo', 'inputs': {'clip': ['13', 0], 'vae': ['11', 0], 'first_frame': ['200', 0], 'prompt': prompt, 'width': 768, 'height': 768, 'length': 124}},
        '16':  {'class_type': 'BasicGuider', 'inputs': {'model': ['121', 0], 'conditioning': ['104', 0]}},
        '17':  {'class_type': 'KSamplerSelect', 'inputs': {'sampler_name': 'res_multistep'}},
        '9':   {'class_type': 'BasicScheduler', 'inputs': {'model': ['121', 0], 'scheduler': 'simple', 'steps': 8, 'denoise': 1.0}},
        '14':  {'class_type': 'SamplerCustomAdvanced', 'inputs': {'noise': ['15', 0], 'guider': ['16', 0], 'sampler': ['17', 0], 'sigmas': ['9', 0], 'latent_image': ['104', 1]}},
        '10':  {'class_type': 'VAEDecode', 'inputs': {'samples': ['14', 0], 'vae': ['11', 0]}},
        '91':  {'class_type': 'CreateVideo', 'inputs': {'images': ['10', 0], 'fps': 24, 'bit_depth': 8}},
        '92':  {'class_type': 'SaveVideo', 'inputs': {'video': ['91', 0], 'filename_prefix': f'{cat}_{exp}', 'format': 'mp4', 'codec': 'auto'}},
    }

    req = urllib.request.Request(f'{COMFY}/prompt', data=json.dumps({'prompt': WF}).encode(), headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=30) as r:
        resp = json.loads(r.read())
        pid = resp.get('prompt_id')
        if not pid:
            print(f'  {exp}: submit failed')
            return False

    print(f'  {exp}: submitted, waiting...')
    start = time.time()
    while True:
        time.sleep(10)
        try:
            with urllib.request.urlopen(f'{COMFY}/history/{pid}', timeout=10) as r:
                h = json.loads(r.read())
                entry = h.get(pid)
                if entry:
                    for nid, out in entry.get('outputs', {}).items():
                        for item in out.get('videos', []):
                            fname = item.get('filename')
                            if fname and fname.endswith('.mp4'):
                                video = os.path.join(OUT_ROOT, item.get('subfolder', ''), fname)
                                os.makedirs(frame_dir, exist_ok=True)
                                raw_dir = os.path.join(frame_dir, '_raw')
                                os.makedirs(raw_dir, exist_ok=True)
                                subprocess.run([FFMPEG, '-y', '-i', video, '-vf', 'fps=6', os.path.join(raw_dir, 'f_%04d.png')], capture_output=True)
                                raw_files = sorted([f for f in os.listdir(raw_dir) if f.endswith('.png')])
                                for i, f in enumerate(raw_files):
                                    img = Image.open(os.path.join(raw_dir, f)).convert('RGBA')
                                    result = remove(img, session=session)
                                    result = result.resize((256, 256), Image.LANCZOS)
                                    result.save(os.path.join(frame_dir, f'f_{i:04d}.png'))
                                print(f'  {exp}: done ({len(raw_files)} frames)')
                                return True
        except: pass
        if time.time() - start > 600:
            print(f'  {exp}: timeout')
            return False

if __name__ == '__main__':
    for cat in ['花斑', '银渐层', '玄猫', '仙喵长老']:
        print(f'\n=== {cat} ===')
        for exp in ['idle', 'celebrate', 'defeat', 'smug', 'thinking', 'worried']:
            process_video(cat, exp)

    print('\n=== ALL DONE ===')
