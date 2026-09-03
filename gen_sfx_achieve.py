# -*- coding: utf-8 -*-
# 成就解锁音效：AceStep 1.5 本地生成（铃声双音，后续 ffmpeg 裁剪）
import json, shutil, time, urllib.request, os

COMFY = 'http://127.0.0.1:8188'
OUT_ROOT = r'H:\ComfyUI_windows_portable\ComfyUI\output'
WORK = r'E:\UnityProject\wuziqi\_sfx'
os.makedirs(WORK, exist_ok=True)

TAGS = ('achievement unlock chime, bright metallic bell, ascending two tone arpeggio, '
        'clean crisp, short jingle, positive rewarding, no drums, no vocals, no melody')
LYRICS = ''
DURATION = 10  # AceStep 最短时长限制，生成后 ffmpeg 裁剪


def post(url, payload):
    req = urllib.request.Request(url, data=json.dumps(payload).encode(),
                                 headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=120) as r:
        return json.loads(r.read())


def get(url):
    with urllib.request.urlopen(url, timeout=60) as r:
        return json.loads(r.read())


WF = {
    '105': {'class_type': 'DualCLIPLoader', 'inputs': {
        'clip_name1': 'qwen_0.6b_ace15.safetensors',
        'clip_name2': 'qwen_4b_ace15.safetensors',
        'type': 'ace', 'device': 'default'}},
    '106': {'class_type': 'VAELoader', 'inputs': {'vae_name': 'ace_1.5_vae.safetensors'}},
    '104': {'class_type': 'UNETLoader', 'inputs': {
        'unet_name': 'acestep_v1.5_turbo.safetensors', 'weight_dtype': 'default'}},
    '78':  {'class_type': 'ModelSamplingAuraFlow', 'inputs': {'model': ['104', 0], 'shift': 3.1}},
    '98':  {'class_type': 'EmptyAceStep1.5LatentAudio', 'inputs': {'seconds': DURATION, 'batch_size': 1}},
    '94':  {'class_type': 'TextEncodeAceStepAudio1.5', 'inputs': {
        'clip': ['105', 0], 'tags': TAGS, 'lyrics': LYRICS,
        'seed': 42, 'bpm': 120, 'duration': DURATION,
        'timesignature': '4', 'language': 'en', 'keyscale': 'E minor',
        'generate_audio_codes': True, 'cfg_scale': 2.0,
        'temperature': 0.85, 'top_p': 0.9, 'top_k': 0, 'min_p': 0}},
    '47':  {'class_type': 'ConditioningZeroOut', 'inputs': {'conditioning': ['94', 0]}},
    '3':   {'class_type': 'KSampler', 'inputs': {
        'model': ['78', 0], 'positive': ['94', 0], 'negative': ['47', 0],
        'latent_image': ['98', 0], 'seed': 42,
        'steps': 8, 'cfg': 1.0,
        'sampler_name': 'euler', 'scheduler': 'simple', 'denoise': 1.0}},
    '18':  {'class_type': 'VAEDecodeAudio', 'inputs': {'samples': ['3', 0], 'vae': ['106', 0]}},
    '17':  {'class_type': 'SaveAudio', 'inputs': {'audio': ['18', 0], 'filename_prefix': 'ach_sfx'}},
}


def main():
    resp = post(COMFY + '/prompt', {'prompt': WF})
    if 'error' in resp or resp.get('node_errors'):
        print('ERROR: ' + str(resp)[:800]); exit(1)
    pid = resp['prompt_id']
    print('prompt_id:', pid)

    start = time.time()
    fname = None
    while fname is None and time.time() - start < 600:
        time.sleep(5)
        try: h = get(COMFY + '/history/' + pid)
        except Exception: continue
        entry = h.get(pid)
        if not entry: continue
        if entry.get('status', {}).get('status_str') == 'error':
            print('EXEC ERROR'); exit(1)
        for nid, out in entry.get('outputs', {}).items():
            for item in out.get('audio', []):
                f = item.get('filename', '')
                if f.endswith('.wav'): fname = f

    if fname is None:
        print('TIMEOUT'); exit(1)

    src = os.path.join(OUT_ROOT, fname)
    dst = os.path.join(WORK, 'ach_sfx_raw.wav')
    shutil.copy2(src, dst)
    print('saved:', dst)
    print('DONE')


if __name__ == '__main__':
    main()
