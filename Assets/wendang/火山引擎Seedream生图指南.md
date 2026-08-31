# 火山引擎 Seedream 生图指南

> 适用于本项目所有素材生成（角色立绘、图标、UI小物件等）

---

## 一、API 配置

| 项目 | 值 |
|------|-----|
| **API Key** | `YOUR_API_KEY_HERE` |
| **Base URL** | `https://ark.cn-beijing.volces.com/api/plan/v3/images/generations` |
| **模型** | `doubao-seedream-5.0-lite` |

⚠️ **注意**：
- URL 必须用 `/api/plan/v3/`，不要用 `/api/v3/`（会产生额外费用）
- size 参数可选值：`2k`、`3k`、`4k`（不支持 `1K`）
- 图片 URL 24 小时内有效，及时下载

---

## 二、Python 调用模板

```python
import json, base64, urllib.request, os

API_KEY = "YOUR_API_KEY_HERE"
API_URL = "https://ark.cn-beijing.volces.com/api/plan/v3/images/generations"

def generate_image(prompt, save_path, reference_image=None, seed=42):
    headers = {
        "Content-Type": "application/json",
        "Authorization": f"Bearer {API_KEY}"
    }
    body = {
        "model": "doubao-seedream-5.0-lite",
        "prompt": prompt,
        "size": "2k",
        "response_format": "b64_json",
        "watermark": False,
        "seed": seed
    }
    # 图生图：传参考图保持画风
    if reference_image:
        with open(reference_image, "rb") as f:
            img_b64 = base64.b64encode(f.read()).decode()
        ext = os.path.splitext(reference_image)[1].lower()
        mime = {"png": "image/png", "jpg": "image/jpeg", "jpeg": "image/jpeg"}.get(ext, "image/png")
        body["image"] = f"data:{mime};base64,{img_b64}"

    data = json.dumps(body).encode("utf-8")
    req = urllib.request.Request(API_URL, data=data, headers=headers)
    with urllib.request.urlopen(req, timeout=120) as resp:
        result = json.loads(resp.read())

    for img_data in result.get("data", []):
        if "b64_json" in img_data:
            img_bytes = base64.b64decode(img_data["b64_json"])
            with open(save_path, "wb") as f:
                f.write(img_bytes)
            print(f"Saved: {save_path} ({len(img_bytes)//1024}KB)")
            return save_path

# 文生图
generate_image("水墨画风格的古琴图标", "output.png")

# 图生图（传参考图保持画风）
generate_image("思考的表情", "output.png", reference_image="ref_portrait.png")
```

---

## 三、本项目生图工作流

### 角色立绘/表情（推荐流程）

1. **用大头照做参考图**（图生图保持画风一致）
2. **提示词模板**（以橘座为例）：

```
传统中国水墨工笔画风格，淡彩设色，宣纸笔墨质感。
一只胖嘟嘟的橘色虎斑猫，戴着红色项圈挂着金色铃铛。
[姿势/表情描述]。
橘色和奶油白色相间的虎斑花纹，白肚皮。
背景纯绿色幕布(#00FF00)，纯色无纹理无渐变无阴影。
笔触写意墨色浓淡变化。古风手绘可爱风。
猫占画面七成，居中构图。无文字无其他物件。
```

3. **后处理**：色键抠绿 → 缩放到 512×512 → 导入 Unity

### 6 个情绪的姿势参考

| 情绪 | 姿势描述 |
|------|---------|
| idle（待机） | 坐姿闭眼睡觉或慵懒伸懒腰 |
| thinking（思考） | 坐姿，一只爪子抬到下巴处做出思考的动作，眼睛向上看 |
| smug（得意） | 二郎腿坏笑，得意洋洋的表情 |
| worried（担心） | 缩脖炸毛，紧张不安的表情 |
| celebrate（庆祝） | 跳起欢呼，开心的表情 |
| defeat（失败） | 趴伏摊滩，沮丧的表情 |

---

## 四、参数速查

| 参数 | 可选值 | 说明 |
|------|--------|------|
| model | `doubao-seedream-5.0-lite` | 当前使用的模型 |
| size | `2k` / `3k` / `4k` | 图片分辨率 |
| response_format | `url` / `b64_json` | 返回格式，建议用 b64_json |
| watermark | `true` / `false` | 是否加水印 |
| seed | 整数 | 固定 seed 可复现 |
| image | Base64 或 URL | 参考图（图生图） |

---

*文档创建于 2026-08-29*
