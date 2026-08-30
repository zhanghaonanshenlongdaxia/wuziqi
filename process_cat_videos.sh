#!/bin/bash
# 批量处理猫猫视频：裁剪2秒 → 31帧 → 色键抠绿 → 512x512
set -e

FFMPEG="C:/Users/zhn/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe/ffmpeg-9.0-full_build/bin/ffmpeg.exe"
SRC="Assets/Videos/Cat"
DST="Assets/Resources/CatFrames"
TMP="/tmp/cat_frames_tmp"
FPS=6      # 12帧 / 2秒
SIZE=512

# 视频映射: 猫名/mood → 视频文件
declare -A VIDEOS=(
  # 小白
  ["小白/idle"]="$SRC/小白/Cat_idle_.mp4"
  ["小白/thinking"]="$SRC/小白/Cat_thinking_.mp4"
  ["小白/smug"]="$SRC/小白/Cat_smug_.mp4"
  ["小白/celebrate"]="$SRC/小白/Cat_celebrate_.mp4"
  ["小白/defeat"]="$SRC/小白/Cat_defeat_.mp4"
  ["小白/worried"]="$SRC/小白/Cat_worried_.mp4"
  # 橘座
  ["橘座/idle"]="$SRC/橘座/idle.mp4"
  ["橘座/thinking"]="$SRC/橘座/juzuo_thinking_v4_.mp4"
  ["橘座/smug"]="$SRC/橘座/juzuo_smug_v2g_.mp4"
  ["橘座/celebrate"]="$SRC/橘座/juzuo_celebrate_v2g_.mp4"
  ["橘座/defeat"]="$SRC/橘座/juzuo_defeat_v2g_.mp4"
  ["橘座/worried"]="$SRC/橘座/juzuo_worried_v2g_.mp4"
)

# 自动填充其他猫（每猫6个标准mood）
for cat in 仙喵长老 玄猫 花斑 银渐层 黑炭; do
  for mood in idle thinking smug celebrate defeat worried; do
    key="$cat/$mood"
    if [[ -z "${VIDEOS[$key]}" ]]; then
      VIDEOS[$key]="$SRC/$cat/${cat}_${mood}_.mp4"
    fi
  done
done

SUCCESS=0
FAIL=0

for key in "${!VIDEOS[@]}"; do
  IFS='/' read -r cat mood <<< "$key"
  vid="${VIDEOS[$key]}"
  outdir="$DST/$cat/$mood"

  if [[ ! -f "$vid" ]]; then
    echo "SKIP $key - video not found: $vid"
    ((FAIL++)) || true
    continue
  fi

  # 清空旧帧
  rm -f "$outdir"/f_*.png

  # ffmpeg: 裁剪2秒 → 15.5fps抽帧 → 色键抠绿 → 缩放 → 透明PNG
  "$FFMPEG" -y -v warning \
    -i "$vid" \
    -t 2 \
    -vf "fps=$FPS,colorkey=0x00FF00:0.15:0.1,scale=$SIZE:$SIZE:flags=lanczos" \
    -pix_fmt rgba \
    -start_number 0 \
    "$outdir/f_%03d.png" 2>&1

  count=$(ls "$outdir"/f_*.png 2>/dev/null | wc -l)
  echo "OK  $key → $count frames"
  ((SUCCESS++)) || true
done

echo ""
echo "Done: $SUCCESS success, $FAIL failed"
