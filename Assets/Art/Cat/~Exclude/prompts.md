# 猫猫立绘 Stable Diffusion 提示词

## 通用参数建议
- **模型：** 国风/水墨/可爱风格模型（如 GuoFeng3、InkPaint 等）
- **尺寸：** 512x512 或 768x768
- **采样器：** DPM++ 2M Karras
- **步数：** 25-30
- **CFG：** 7-8

## 通用负面提示词 (Negative Prompt)
```
realistic, photorealistic, 3d render, ugly, deformed, noisy, blurry, low quality, worst quality, extra limbs, bad anatomy, text, watermark, signature
```

---

## 1. 小白 (Little White)
```
A cute pure white kitten with round face and big sparkling eyes, pink nose, soft fluffy fur, gentle and innocent expression, sitting peacefully, Chinese ink wash painting style, kawaii chibi art, pastel pink and white color palette, simple clean background, game character portrait, masterpiece, best quality
```

## 2. 橘座 (Orange Master)
```
A chubby orange tabby cat with confident smug expression, slightly overweight body, half-closed eyes looking proud, orange and cream white fur pattern, sitting in a regal pose, Chinese ink wash painting style, kawaii chibi art, warm orange and cream color palette, simple clean background, game character portrait, masterpiece, best quality
```

## 3. 黑炭 (Black Charcoal)
```
A sleek pure black cat with piercing golden eyes, mysterious and aloof expression, elegant sitting pose, shiny black fur with subtle highlights, mysterious aura, Chinese ink wash painting style, kawaii chobi art, black and gold color palette, simple clean background, game character portrait, masterpiece, best quality
```

## 4. 花斑 (Calico)
```
A playful calico cat with white base fur and orange-black patches, bright curious eyes, mischievous grin, energetic dynamic pose, Chinese ink wash painting style, kawaii chibi art, white orange and black color palette, simple clean background, game character portrait, masterpiece, best quality
```

## 5. 银渐层 (Silver Shaded)
```
An elegant silver shaded British shorthair cat, plush silver-gray fur with gradient tips, stunning green eyes, refined aristocratic sitting pose, gentle sophisticated expression, Chinese ink wash painting style, kawaii chibi art, silver gray and green color palette, simple clean background, game character portrait, masterpiece, best quality
```

## 6. 玄猫 (Mystic Cat)
```
A mystical dark black cat with subtle dark纹 patterns, striking red eyes, ancient Chinese mystical elements, small talisman or seal mark on forehead, wise otherworldly expression, Chinese ink wash painting style, kawaii chibi art, dark black and crimson red color palette, simple clean background, game character portrait, masterpiece, best quality
```

## 7. 仙喵长老 (Cat Elder)
```
A wise ancient white long-haired cat with flowing fur, wearing a tiny traditional Chinese bamboo hat (斗笠), long white whiskers like a sage, kind knowing eyes with gentle wrinkles, elder master aura, Chinese ink wash painting style, kawaii chibi art, white gold and brown color palette, simple clean background, game character portrait, masterpiece, best quality
```

---

## 使用说明
1. 先生成一张满意的底图
2. 用 img2img 微调细节
3. 统一裁剪为正方形（如 512x512）
4. 导入 Unity 后设置为 Sprite，Pivot 设为 Center
5. 赋值到对应 CatProfile 的 portrait 字段
