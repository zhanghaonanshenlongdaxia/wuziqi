# APK 打包流程手册

> 适用于本项目（团结引擎 Tuanjie 2022.3.62f2c1）
> 最后成功打包：2026-09-01，产出 226MB（release签名）

---

## 一、前置条件（已完成，无需重复）

| 项 | 值 | 配置方式 |
|---|---|---|
| 包名 | `com.zhanghaonan.xianmiao.wuziqi` | Editor 菜单 `仙喵五子棋/配置Android打包` |
| 脚本后端 | IL2CPP | 同上 |
| 架构 | ARMv7 + ARM64 | 同上 |
| Min SDK | 24（Android 7.0） | 同上 |
| 签名 | 正式release签名（见下方） | Player Settings → Android → Publishing Settings |
| Android 模块 | SDK + NDK + BuildTools 34.0.0 | 已安装 |

### 签名 Keystore 信息（重要，务必保管）

| 项 | 值 |
|---|---|
| Keystore 文件 | `release.jks`（项目根目录） |
| Keystore 密码 | `wuziqi2026` |
| 别名（Alias） | `wuziqi` |
| 别名密码 | `wuziqi2026` |
| 算法 | RSA 2048位，SHA256withRSA |
| 有效期 | 36500天（约100年，至2126年） |
| 主体 | CN=CangLongQiXiu Studio, OU=GameDev, O=CangLongQiXiu, L=Beijing, ST=Beijing, C=CN |

> **⚠ 重要：** `release.jks` 是应用签名密钥，**丢失后无法更新应用**。
> 请备份到安全位置，不要提交到公开仓库。后续所有版本更新必须使用同一keystore。

---

## 二、打包步骤

### 1. 保存场景

确认 Unity 场景无未保存修改（Ctrl+S）。

### 2. 执行打包

```
Unity 菜单栏 → 仙喵五子棋 → 打包APK
```

或者用命令行 / MCP 工具：

```
unity_menu execute 仙喵五子棋/打包APK
```

### 3. 等待完成

- IL2CPP 首次编译约 10-20 分钟（后续增量构建约 5 分钟）
- Unity 底部有进度条，构建期间编辑器会卡顿，属正常
- APK 输出到：`apk/FiveChess-v2.0.0-release.apk`

### 4. 验证

Console 输出 `APK BUILD SUCCESS: ... size=XXXMB` 即成功。
确认 `apk/FiveChess-v2.0.0-release.apk` 存在且大小正常（~226MB）。
如果输出 `APK BUILD FAILED: Failed`，按下方第三节排查。

---

## 三、常见错误排查

### 错误 1：Burst x86 库链接失败

```
ld.lld: error: Packages/com.unity.burst/libs/burstRTL_l32.a(...) is incompatible with armelf_linux_eabi
```

**原因**：本地化 burst 包的 `libs/` 目录包含 x86 架构静态库，被误链到 ARM 构建。

**修复**：已删除 `Packages/com.unity.burst/libs/` 整个目录。如果恢复 burst 包后再次出现，删掉该目录即可。

### 错误 2：Duplicate class

```
Duplicate class com.tapsdk.tapad.xxx found in modules jetified-DirichletAD_Mediation_5.1.2.3-runtime and jetified-DirichletAD_Mediation_5.2.1.4-runtime
```

**原因**：Dirichlet SDK 新旧两版 AAR 同时存在（更新时没删旧版）。

**修复**：删除 `Assets/Plugins/Android/DirichletMediation/libs/` 下的旧版本 AAR（如 `*5.1.2.3*`），只保留最新版。

### 错误 3：模态弹窗阻塞

如果 MCP 工具报 `A modal dialog is blocking the Unity editor main thread`，说明有弹窗（如 Build failure），用 `unity_dialog click` 关掉再继续。

### 通用排查方法

详细错误信息在 Editor.log 里：

```powershell
# 查看最近的构建错误
Get-Content "$env:LOCALAPPDATA\Unity\Editor\Editor.log" -Tail 300 | Where-Object { $_ -match "error|Error|failed|Failed" }
```

---

## 四、APK 优化（可选）

当前 APK 777MB 偏大，主要因为：
- 猫表情视频序列帧（6 情绪 × 多只猫 × 31 帧）
- 9 首 BGM + 全套音效

可做的优化：

| 优化 | 预估收益 |
|---|---|
| 未使用猫表情的帧删除或降分辨率 | -200~300MB |
| BGM 已压缩（Vorbis），音效已压缩（ADPCM） | 已完成 |
| AssetBundle 分离资源热更 | 可选（架构改动大） |
| 仅保留 ARM64（去掉 ARMv7） | -15% |

---

## 五、更新迭代

改代码/资源后再打包：
1. 保存场景
2. 执行 `仙喵五子棋/打包APK`
3. APK 覆盖输出到 `Builds/Android/XianMiaoWuZiQi.apk`

版本号更新（可选）：`Assets/Scripts/Editor/AndroidBuildSetup.cs` 里改 `bundleVersionCode`。

---

## 六、安装测试

```powershell
# USB 连接手机后
adb install -r "E:\UnityProject\wuziqi\Builds\Android\XianMiaoWuZiQi.apk"
```

或直接把 APK 文件传到手机上点击安装（需开启"允许未知来源"）。
