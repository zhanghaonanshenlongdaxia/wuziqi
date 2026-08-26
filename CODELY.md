

## Codely Structured Memories

### User
- [2026-08-25 17:06:37] 用户在做可商业化的小型Unity游戏（参考抖音独立游戏案例，如卖出18000份数独猫）。自述美术审美不行，依赖AI生成整套美术并保持风格统一；五子棋项目选了国风水墨风格，偏好"玩法+美术并行一步到位"的工作方式，重视手感反馈（落子动画/音效/特效）。
### Feedback
- [2026-08-25 17:30:48] 用户批评"做的五子棋一点意思也没有"——纯玩法+好美术≠有趣，情感层（陪玩角色）才是小游戏的核心卖点（对标数独猫18000份）。用户选择三个方向全做：角色系统（猫仙人对手）→玩法变体（限时/障碍）→收集进度（连胜解锁）。**Why:** v1只有壳没灵魂，用户明确要的是"和猫下棋"而非"下棋工具"。**How to apply:** 后续小游戏设计优先把AI对手/陪玩角色人格化，规则复杂度排后。


- [2026-08-25 19:26:18] TJGenerators扩展工具 generate_sprite（is_segmentation=true 分割模式）在本项目连续失败6次（服务端报"Task failed"），但 MCP generate_image（seedream，不带分割）成功率100%。**Why:** 扩展分割通道不稳定；且曾因看不到即时结果重复提交同一任务浪费积分。**How to apply:** 生成角色立绘/单图素材直接走 MCP generate_image；异步任务提交后相信回执不重复提交；需要透明背景时用水墨纸纹背景直接融入UI的方案替代抠图分割。
- [2026-08-26 19:57:28] 生成素材优先用本地ComfyUI模型，效果不满意再退回云端MCP工具。**Why:** 用户2026-08-26明确指示"以后生图都用本地大模型生成，效果差了再用unity的"；且云端generate_music(sonilo)生成的BGM被用户评价"太难听"全部删除改用本地AceStep。**How to apply:** 生图→Qwen-Image工作流(GGUF+Lightning4步)；生音乐→AceStep1.5工作流；生视频→H3绿幕方案(单首帧锁+8步turbo，双锁会背景漂白)；投币的云端API只在本地不行时用。

### Project
- [2026-08-25 17:06:37] wuziqi项目（D:\UnityProject\wuziqi，Unity 2022.3.62f2c1内置管线）架构：Wuziqi.Core纯逻辑（GomokuBoard/GomokuAI启发式评分AI）、Wuziqi.Game.GameManager（事件驱动回合流）、Wuziqi.UI（BoardView网格+棋子+点击映射+胜利特效、GameUIController音频弹窗）。Canvas为ScreenSpaceCamera模式（planeDistance=10，为让世界空间粒子纸花渲染在UI之前）。素材在Assets/Art、Assets/Audio、Assets/TJGeneratorLibEffects。**Why:** 后续迭代（AI加强、Spine陪玩角色、平台打包）需沿用此结构。**How to apply:** 改玩法动Core/Game，改视觉动UI层；新粒子特效须放画布前方3单位。
- [2026-08-25 17:06:37] TJGenerators生成子代理（audio-generator/image-generator/game-ui-kit-generator/sprite等）提交异步生成任务后会提前结束回合、不再回来完成导入收尾——主流程必须自己负责：素材落盘位置确认、复制到目标目录、导入设置校正（Sprite类型/Tight打包）、引用接线。**Why:** 本项目5个生成子代理全部出现该模式，等它们收尾会卡死。**How to apply:** 子代理只当"提交器"用，prompt里只要求报告task_id和路径，集成自己做。
- [2026-08-25 17:30:51] 本机环境：winget install Gyan.FFmpeg 会静默失败/挂起（勿再用）；用户有梯子（VPN）可开关，下载外网资源前可提醒开梯子；ffmpeg用直链 https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip 下载到 D:\ffmpeg\ffmpeg.zip 后解压。record_game_view需要ffmpeg在PATH或设CODELY_FFMPEG_PATH。
- [2026-08-26 19:57:10] ffmpeg路径已变更：winget版 ffmpeg 9.0 位于 C:\Users\zhn\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_Microsoft.Winget.Source_8wekyb3d8bbwe\ffmpeg-9.0-full_build\bin\ffmpeg.exe（在PATH中，where.exe ffmpeg 可找到）。旧路径 D:\ffmpeg\ffmpeg.exe 已不存在勿再用。CODELY_FFMPEG_PATH 环境变量指向旧路径已失效——录屏前先 where.exe ffmpeg 确认实际路径。
- [2026-08-26 19:57:37] 仙喵五子棋v3系统（2026-08-26）：货币名"仙喵币"(代码仍用Coins)；7只猫CatProfile(ScriptableObject在Assets/ScriptableObjects/Cats，3免费+2广告+2仙喵币解锁，帧暂用小白占位)；EconomyManager体力5/仙喵币+每日广告3次限制；AdManager Unity Ads占位(simulateAds=true)；猫动画帧目录结构=Assets/Art/Cat/Frames/{情绪}/单层，CharacterController.ReloadFrames两级查找(先Frames/{猫}/{情绪}再回退Frames/{情绪})兼容未来多猫；悔棋需看广告；歌曲列表前3免费+3首10币+3首20币解锁。**Why:** 后续生成新猫动画帧时放Frames/{猫名}/{情绪}/即可被自动识别。**How to apply:** 接Unity Ads SDK时只改AdManager；加新猫时建CatProfile资产+CatManager.cats数组追加。
- [2026-08-26 20:47:49] UI弹窗系统（2026-08-26晚）：5个弹窗（Settings/ExitConfirm/Reward/CatSelect/SongList）已用本地Qwen-Image生成的素材重做完整UI（BgPanelPopup面板底/BgButtonMedium按钮底/IconToggle开关镜像两态/IconCloseX/IconSliderBar）。Item模板化：Assets/Prefabs/UI/SongItem.prefab+CatItem.prefab，面板脚本Instantiate模板填充数据（SongItem.SetTitle/SetStatus、CatItem.SetPortrait/SetName/SetLocked），新增歌曲/猫只需加数据数组不用动场景。**Why:** 用户明确要求"单首歌曲单个猫猫做成item模板，后续新增用模板而不是场景里加"。**How to apply:** 防双构建bug已修（building标志+DestroyImmediate清旧项）；OnEnable构建的面板别在Start里再调BuildGrid；CharacterController.LoadFrameDir需IsValidFolder预检避免FindAssets报错日志。

### Reference
- [2026-08-26 19:57:20] 本地ComfyUI（H:\ComfyUI_windows_portable，API http://127.0.0.1:8188，需手动启动：python_embeded\python.exe main.py --windows-standalone-build --use-sage-attention）模型清单（2026-08-26，共约92GB）：①视频=MiniMax H3 fl2va int8+fl2v turbo LoRA(4步/8步)；②生图=Qwen-Image-2512 Q4 GGUF(UnetLoaderGGUF)+qwen_2.5_vl_7b_fp8编码器+Lightning 4步LoRA；③音乐=AceStep1.5 turbo(DualCLIPLoader双qwen编码器+TextEncodeAceStepAudio1.5+ModelSamplingAuraFlow shift=3.1+ConditioningZeroOut+turbo 8步cfg=1，用错v1.0节点会报NoneType.shape)；④音效=Stable Audio 3；⑤RMBG-2.0抠图、UltraSharp超分。注意：ByteDanceSeedreamNodeV2等API节点本地不可用(400需认证)；ComfyUI网页任务列表只显示本地任务，云端任务看不到。
