# 猫猫动画生成说明

## 当前进度
- ✅ 小白: 5/5 表情完成
- ✅ 橘座: 5/5 表情完成
- ✅ 黑炭: 5/5 表情完成
- ✅ 花斑: 5/5 表情完成
- ✅ 银渐层: 5/5 表情完成
- ✅ 玄猫: 5/5 表情完成
- ✅ 仙喵长老: 5/5 表情完成

**全部完成！** 7只猫 × 5个表情 = 35个动画

## 文件说明

### 生成脚本
- `gen_all_cats_portraits.py` - 批量生成5只猫的Seedream立绘
- `gen_all_cats_h3_videos.py` - 批量生成H3视频（可能因内存问题失败）
- `gen_single_cat_h3.py` - 单只猫H3视频生成
- `gen_video_one.py` - 单个表情视频生成
- `gen_remaining_cats.py` - 批量生成剩余猫动画（推荐）
- `process_videos_manual.py` - 手动处理已完成的视频
- `continue_animations.py` - 继续生成剩余动画（推荐）

### 使用方法

#### 1. 生成立绘（已完成）
```bash
python gen_all_cats_portraits.py
```

#### 2. 生成动画（推荐）
```bash
python continue_animations.py
```
这个脚本会自动跳过已完成的，从断点继续。

#### 3. 如果某个视频生成失败
```bash
python gen_video_one.py 黑炭 defeat
```

#### 4. 手动处理已完成的视频
```bash
python process_videos_manual.py
```

## 技术细节

### 绿幕首帧技巧
H3模型会继承首帧的背景色。必须先将立绘抠图放到纯绿(#00FF00)背景上，再作为H3首帧。

### 视频生成参数
- 模型: MiniMax H3
- 分辨率: 768x768
- 帧数: 124帧 @24fps
- 抽帧: 6fps (每4帧取1，约31帧)
- 最终尺寸: 256x256

### 文件结构
```
Assets/Art/Cat/
├── Portraits/          # 猫猫立绘
├── Frames/             # 动画帧
│   ├── 小白/          # 小白的动画
│   ├── 橘座/          # 橘座的动画
│   ├── 黑炭/          # 黑炭的动画
│   └── ...
└── *.py               # 生成脚本
```

## 注意事项
1. 视频生成需要较长时间（每条约2-5分钟）
2. 如果遇到内存错误，重启ComfyUI后重试
3. 生成过程中不要关闭ComfyUI
4. 每个视频生成后会自动抽帧和抠绿
