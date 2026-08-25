

## Codely Structured Memories

### User
- [2026-08-25 17:06:37] 用户在做可商业化的小型Unity游戏（参考抖音独立游戏案例，如卖出18000份数独猫）。自述美术审美不行，依赖AI生成整套美术并保持风格统一；五子棋项目选了国风水墨风格，偏好"玩法+美术并行一步到位"的工作方式，重视手感反馈（落子动画/音效/特效）。
### Feedback
- [2026-08-25 17:30:48] 用户批评"做的五子棋一点意思也没有"——纯玩法+好美术≠有趣，情感层（陪玩角色）才是小游戏的核心卖点（对标数独猫18000份）。用户选择三个方向全做：角色系统（猫仙人对手）→玩法变体（限时/障碍）→收集进度（连胜解锁）。**Why:** v1只有壳没灵魂，用户明确要的是"和猫下棋"而非"下棋工具"。**How to apply:** 后续小游戏设计优先把AI对手/陪玩角色人格化，规则复杂度排后。


- [2026-08-25 19:26:18] TJGenerators扩展工具 generate_sprite（is_segmentation=true 分割模式）在本项目连续失败6次（服务端报"Task failed"），但 MCP generate_image（seedream，不带分割）成功率100%。**Why:** 扩展分割通道不稳定；且曾因看不到即时结果重复提交同一任务浪费积分。**How to apply:** 生成角色立绘/单图素材直接走 MCP generate_image；异步任务提交后相信回执不重复提交；需要透明背景时用水墨纸纹背景直接融入UI的方案替代抠图分割。


### Project
- [2026-08-25 17:06:37] wuziqi项目（D:\UnityProject\wuziqi，Unity 2022.3.62f2c1内置管线）架构：Wuziqi.Core纯逻辑（GomokuBoard/GomokuAI启发式评分AI）、Wuziqi.Game.GameManager（事件驱动回合流）、Wuziqi.UI（BoardView网格+棋子+点击映射+胜利特效、GameUIController音频弹窗）。Canvas为ScreenSpaceCamera模式（planeDistance=10，为让世界空间粒子纸花渲染在UI之前）。素材在Assets/Art、Assets/Audio、Assets/TJGeneratorLibEffects。**Why:** 后续迭代（AI加强、Spine陪玩角色、平台打包）需沿用此结构。**How to apply:** 改玩法动Core/Game，改视觉动UI层；新粒子特效须放画布前方3单位。
- [2026-08-25 17:06:37] TJGenerators生成子代理（audio-generator/image-generator/game-ui-kit-generator/sprite等）提交异步生成任务后会提前结束回合、不再回来完成导入收尾——主流程必须自己负责：素材落盘位置确认、复制到目标目录、导入设置校正（Sprite类型/Tight打包）、引用接线。**Why:** 本项目5个生成子代理全部出现该模式，等它们收尾会卡死。**How to apply:** 子代理只当"提交器"用，prompt里只要求报告task_id和路径，集成自己做。
- [2026-08-25 17:30:51] 本机环境：winget install Gyan.FFmpeg 会静默失败/挂起（勿再用）；用户有梯子（VPN）可开关，下载外网资源前可提醒开梯子；ffmpeg用直链 https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip 下载到 D:\ffmpeg\ffmpeg.zip 后解压。record_game_view需要ffmpeg在PATH或设CODELY_FFMPEG_PATH。
- [2026-08-25 19:36:39] ffmpeg已就绪：D:\ffmpeg\ffmpeg.exe（77MB，ffmpeg 6.0，2026-08-25从国内npmmirror镜像 https://cdn.npmmirror.com/binaries/ffmpeg-static/b6.0/ffmpeg-win32-x64 下载，18MB/s 4秒完成，gyan.dev国外源仅20KB/s不可用）。已设用户级环境变量 CODELY_FFMPEG_PATH=D:\ffmpeg\ffmpeg.exe。**注意：Unity编辑器需在设置变量之后启动才能读到该变量**——2026-08-25设置时Unity已运行4小时，录屏仍报"ffmpeg not found"，需重启Unity。**How to apply:** 录MP4前确认Unity进程启动时间晚于环境变量设置时间；否则请用户重启Unity后用unity_refresh重连再录。


### Reference

