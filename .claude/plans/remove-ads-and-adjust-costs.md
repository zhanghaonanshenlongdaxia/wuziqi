# 移除广告 SDK + 调整解锁费用

## Context
Dirichlet 广告平台需要版号，无版号游戏无法获得广告填充（错误码 200003 无填充）。需要移除广告 SDK，隐藏所有广告按钮，将原广告解锁的猫改为金币解锁，并提高各项费用让玩家有更长的游戏目标。

## 当前状态

**猫猫解锁配置：**
| 猫 | unlockType | coinCost | challengeCost | winReward |
|----|-----------|----------|--------------|-----------|
| 小白 | Free | 0 | 2 | 10 |
| 橘座 | Free | 0 | 3 | 15 |
| 黑炭 | Free | 0 | 4 | 20 |
| 花斑 | **Ad** | 0 | 6 | 30 |
| 阿银 | **Ad** | 0 | 8 | 40 |
| 墨隐 | Coins | 50 | 12 | 60 |
| 仙喵长老 | Coins | 100 | 20 | 100 |

**歌曲费用：** `{0,0,0,10,10,10,20,20,20}`

## 修改计划

### 1. 花斑、阿银改为金币解锁 + 提高所有金币猫费用
- 花斑: Ad → Coins, coinCost = 80, challengeCost = 8, winReward = 50
- 阿银: Ad → Coins, coinCost = 150, challengeCost = 12, winReward = 80
- 墨隐: coinCost 50 → 200, challengeCost 12 → 18, winReward 60 → 120
- 仙喵长老: coinCost 100 → 500, challengeCost 20 → 30, winReward 100 → 200
- 小白: challengeCost 2 → 3, winReward 10 → 15
- 橘座: challengeCost 3 → 5, winReward 15 → 25
- 黑炭: challengeCost 4 → 6, winReward 20 → 35

### 2. 提高歌曲费用
`{0,0,0,20,20,30,50,50,80}`

### 3. CatProfile.cs — 移除 Ad 解锁类型
- 删除 `UnlockType.Ad`
- UnlockType 枚举改为 `{ Free, Coins }`
- 删除 `UnlockByAd` 相关引用

### 4. CatManager.cs — 删除 UnlockByAd 方法

### 5. CatSelectPanel.cs — 移除广告解锁按钮
- 删除 `unlockAdButton` 字段和相关逻辑
- `GetUnlockHint` 移除 Ad 分支
- `TryUnlockByAd` 方法删除

### 6. CatDetailPanel.cs — 移除 "看广告" 文案
- unlockStatus 的 Ad 分支改为 Coins

### 7. GameUIController.cs — 悔棋改为直接用（不看广告）
- `OnUndoClicked` 中移除 AdManager 调用，直接调 `gameManager.Undo()`

### 8. EnergyInsufficientPanel.cs — 隐藏看广告按钮
- `watchAdButton` 按钮始终隐藏（`SetActive(false)`）
- tip 文案改为 "等待自然恢复体力"

### 9. RewardPanel.cs — 整个面板改为仅显示信息
- 移除广告按钮逻辑，保留关闭功能
- 或者直接在 TopBarController 中不打开此面板

### 10. TopBarController.cs — 移除 rewardPanel 相关
- `energyAddButton` 和 `coinsAddButton` 不再打开 RewardPanel
- 可以改为显示一个提示 "体力随时间恢复" 或直接不响应

### 11. EconomyManager.cs — 清理广告相关
- 删除 `MaxAdRewardsPerDay`, `EnergyPerAd`, `CoinsPerAd` 常量
- 删除 `CanWatchEnergyAd`, `CanWatchCoinsAd` 属性
- 删除 `GrantEnergyAdReward`, `GrantCoinsAdReward` 方法
- 删除 `EnergyAdCount`, `CoinsAdCount` 字段和相关 PlayerPrefs

### 12. AdManager.cs — 删除文件

### 13. 删除 Dirichlet SDK 文件
- `Assets/DirichletMediation/` 整个目录
- `Assets/Plugins/Android/DirichletMediation/` 整个目录
- `Assets/Plugins/iOS/DirichletMediationUnityBridge.*`
- `Assets/wendang/广告接入手册.md`
- `Assets/StreamingAssets/dirichlet_keys.json`
- `dirichlet_keys.json`（项目根目录）

### 14. Scene 更新
- SampleScene 中的 AdManager GameObject 删除
- RewardPanel 的广告按钮引用清理
- EnergyInsufficientPanel 的 watchAdButton 引用保留但代码中隐藏

## 涉及文件

| 文件 | 操作 |
|------|------|
| `Assets/ScriptableObjects/Cats/Cat_*.asset` (7个) | 修改 unlockType/coinCost/challengeCost/winReward |
| `Assets/Scripts/Game/CatProfile.cs` | 移除 Ad 枚举值 |
| `Assets/Scripts/Game/CatManager.cs` | 删除 UnlockByAd 方法 |
| `Assets/Scripts/Game/AdManager.cs` | 删除 |
| `Assets/Scripts/Game/EconomyManager.cs` | 移除广告相关字段和方法 |
| `Assets/Scripts/UI/CatSelectPanel.cs` | 移除广告按钮和逻辑 |
| `Assets/Scripts/UI/CatDetailPanel.cs` | 移除看广告文案 |
| `Assets/Scripts/UI/GameUIController.cs` | 悔棋直接执行 |
| `Assets/Scripts/UI/EnergyInsufficientPanel.cs` | 隐藏广告按钮 |
| `Assets/Scripts/UI/RewardPanel.cs` | 简化或移除广告逻辑 |
| `Assets/Scripts/UI/TopBarController.cs` | 移除 rewardPanel 打开逻辑 |
| `Assets/Scripts/UI/SongListPanel.cs` | 提高 coinCosts |
| `Assets/DirichletMediation/` | 删除整个目录 |
| `Assets/Plugins/Android/DirichletMediation/` | 删除 |
| `Assets/Plugins/iOS/DirichletMediation*` | 删除 |
| `Assets/wendang/广告接入手册.md` | 删除 |
