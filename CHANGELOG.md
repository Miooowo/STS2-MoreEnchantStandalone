# 更新日志

格式遵循常见约定：新版本在上；未发行改动可放在 **未发布** 小标题下。

---
## 0.7.2

发布日期：2026-04-13

### 新增
- **附魔图鉴**：图鉴主菜单增加入口；界面风格接近药水研究所与遗物收集（可滚动内容、按奖励品质分段、原版返回键与悬浮说明）；相关代码位于 `src/EnchantmentCompendium/`。
- 简体中文（`zhs`）下，本模组附魔缺失专属图标时使用 `images/enchantments/enchantment_icon.png` 作为兜底；其他语言仍使用原版缺失图。
- **附魔图鉴**ui图片来自clockcycas。
- **超巨化**、**华丽**、**铸剑**附魔图标，来自王筱巫。
- **中和**附魔图标，来自clockcycas。

### 变更
- **附魔图鉴**：相关文案使用 `main_menu_ui` 表（键 `COMPENDIUM_ENCHANT_BROWSER.*`），不再放在 `enchantments`；图鉴入口封面图为 `images/ui/main_menu/enchantment_compendium.png`。
- **幽灵（Spectral / 附魔图鉴 id：`SpectralEthereal`）**
  - **可附魔条件**：攻击牌仅当牌面带有「打出时」**Move** 格挡；技能/能力牌当带有 `PowerVar`，或牌面存在正数格挡动态变量（含 **`Unpowered` 的 `BlockVar`**，如创世之柱、寿衣；此前仅识别 Move 格挡与 `PowerVar`，会漏掉此类牌）。
  - **能力施加/叠层**：与格挡相关的能力数值在 `Hook.ModifyPowerAmountGiven` 中一次性 ×1.5（四舍五入）；仅对白名单内的原版能力类型生效（力量/敏捷等纯数值能力不在此列）。已移除早期对 `Hook.ModifyBlock` 补 `cardSource` 的做法，避免 Unpowered 格挡被二次乘算。
  - **牌面数值**：`EnchantBlockMultiplicative` 除 **Move** 打出格挡外，对技能/能力牌上 **`Unpowered` 的 `Block` / `CalculatedBlock`** 同样 ×1.5，使牌面预览与战斗内层数、格挡收益一致。

### 修复
- **幽灵**：修复创世之柱、寿衣等牌无法附魔、或附魔后牌面格挡/能力数字未反映 ×1.5 的问题。


---
## 0.7.1

发布日期：2026-04-13

### 修复
- **沉眠精华**（原版附魔）：奖励池不再附魔于 0 能（非 X）牌（[#2](https://github.com/Miooowo/STS2-MoreEnchantStandalone/issues/2)）。
- **返回**：附魔不再出现于能力牌（[#3](https://github.com/Miooowo/STS2-MoreEnchantStandalone/issues/3)）。
- **超巨化（奇美拉）**：仅可附于带打出伤害的攻击牌（[#4](https://github.com/Miooowo/STS2-MoreEnchantStandalone/issues/4)）。

---
## 0.7.0

### 新增
- steam验证机制。
- 战斗附魔：**滑溜**（稀有，本场首次打出获得 1 滑溜）、**缓冲**（特殊，本场首次打出获得 1 缓冲）、**复刻**（打出后置入消耗复制品于弃牌堆）、**熔融**（目标易伤层数翻倍）、**劫掠**（罕见，抽至非攻击）、**与我一战**（罕见，你+2 力量、敌人各+1 力量）、**恶魔护盾**（罕见，多人；消耗；自损 1；**自选队友**获得你当前格挡）、**死神**（稀有，仅伤害牌；攻击伤血施加一半伤害的灾厄）、**跃跃欲试**（罕见，耗能+1；按手牌攻击数获能）、**猛扑**（罕见，攻击且基础耗能≥2；下一张技能 0 费）、**夹击**（罕见，多人；耗能+1；夹击/侧袭效果）、**奇巧**（普通，获得奇巧关键词）。
- 设置：变牌随机附魔开关与独立概率（`transform_enchant_enabled` / `transform_enchant_chance_percent`）；旧配置通过 `schema_version` 迁移为与「卡牌奖励附魔概率」一致（无则 10%）。
- 设置：`deck_direct_enchant_enabled` / `deck_direct_enchant_chance_percent`（默认 10%）：非奖励工厂、直接入牌组的牌（巨大扭蛋额外打击/防御、涅奥的苦痛、卷轴箱等）在入组时掷骰；卷轴箱三选一在打开预览前对候选牌掷骰（与入组同一套概率）。`schema_version` 3 迁移写入默认值。
- README.md新增star历史。

### 变更
- **灼热**：`MaxUpgradeLevel` 改为999。

### 修复
- **多打**：无论叠层多少，仅额外增加 1 段力量攻击。
- **变牌继承附魔**：在 <c>CardTransformation.GetReplacement</c> 返回后统一继承原牌附魔；原牌无附魔时再按非战斗奖励概率随机附魔。保留 <c>CardFactory.CreateRandomCardForTransform</c> 后缀作双保险。<c>Hook.ModifyCardBeingAddedToDeck</c> 后缀用 <c>[HarmonyArgument(1)]</c> 绑定第二个参数 <c>CardModel card</c>（首参为 <c>IRunState</c>），避免误绑；蛋遗物克隆升级后再从入参卡补回附魔。

## 0.6.0

发布日期：2026-04-12

### 新增
- 战斗附魔：**坚毅**（打出消耗一张手牌）、**破灭**（打出顶牌并消耗）、**快速**（抽 1）、**放血**（失去 3 生命，获得 2 能量）、**武装**（升级一张手牌）、**恐怖**（稀有，99 层易伤）、**战栗**（2 层易伤）、**中和**（1 层虚弱）、**背包**（抽 1 再丢 1）、**星光**（获得 2 辉星）、**护卫**（召唤 5）、**多打**（罕见，攻击段数 +1）、**碎屑**（攻击伤害+33%，打出添加碎屑至手牌）、**魔法腐化**
- 技术：`Hook.ModifyAttackHitCount` 后缀补丁以支持**多打**；
- 战斗中生成的卡牌可以被添加随机附魔（默认关）

### 变更
- **幽灵**附魔效果改为：此牌获得的格挡增加50%，虚无。

### 修复
- **抢救**附魔在卡牌升级后消耗被移除的bug

### 移除
- **感染**、**超巨化**、**固化**、**打击**附魔的额外文本

---

## 0.5.0

发布日期：2026-04-12

### 新增
- 诅咒附魔：
- - **愚行**
- - **羞耻**
- - **债务**
- - **贪婪**
- - **睡眠不佳**
- - **苦恼**
- - **腐朽**
- - **悔恨**
- - **进阶之灾**
- - **愧疚**
- - **疑虑**
- - **受伤**
- 工程：`MoreEnchantStandalone.csproj` 支持 `/p:ExportModPck=false` 跳过无头 PCK 导出；默认可选 `--display-driver headless --audio-driver Dummy` 以缓解部分环境下 MegaDot 导出崩溃。

### 变更
- **铃铛的诅咒**：遗物发放改为依赖牌上的附魔实例一次性门闩，不再用奖励生成时的 `CardModel` 引用（展示实例与入手实例不一致时也能领到遗物）；`RunState` / `CombatState` 的 `CloneCard` 后为复制牌重置门闩，复制入组可再领一轮。
- **铃铛的诅咒**：对 `CardPileCmd.Add`（含 `CardModel` + `PileType`、返回 `Task` / `Task<T>` 的静态重载）在**加入牌组**完成后补发遗物，覆盖不经 `RewardSynchronizer.SyncLocalObtainedCard` 的路径（如部分遗物复制进牌组）；包装异步返回值时**保留泛型 `Task<T>` 结果**，避免选完卡牌奖励后空引用或流程卡住。

### 修复
- 卡牌奖励选择后 `CardReward.OnSelect` 等处因错误替换 `CardPileCmd.Add` 返回的 `Task<T>` 导致的卡住 / `NullReferenceException`。
- Harmony 补丁在部分游戏版本上找不到固定签名的 `CardPileCmd.Add` 导致 Mod 初始化失败：改为扫描符合条件的重载并配合 `Prepare()`。

## 0.4.0

发布日期：2026-04-08

### 新增
- **感染**诅咒附魔（`INFECTION_CURSE_ENCHANTMENT`）：为卡牌挂载与原版状态牌 **Infection（感染）** 相同的 overlay 场景（`cards/overlays/infection`），由 `NCard.ReloadOverlay` 补丁在附魔存在时覆盖默认 overlay。
- 调试控制台命令 **`forcerandomcursereward`**：战斗中执行后， 卡牌奖励（`CardCreationSource.Encounter`）中首张可接受诅咒的牌必附 **随机诅咒档** 附魔（与 `forcebellreward` 同属 `MoreEnchantCombatRewardDebug`；新战斗开始会清除未消费的请求）。
- **华丽**（附魔 ID 为 `FINALE_CURTAIN`）罕见附魔：战斗中抽牌堆为空时，此牌打出的伤害与获得的格挡 ×3。
- 商店卡牌可配置随机附魔（默认开启），独立概率；先古之民（**Ancient** 稀有度）卡牌奖励可配置随机附魔（默认开启、独立概率），且**不会出现诅咒档附魔**。
- 设置页说明：联机时玩法选项以房主 **Host** 为准。
- **打击**附魔图标：来自卡得。

### 变更
- **诅咒档附魔**：五档曲线中诅咒基础权重下调；按**被附魔奖励牌的稀有度**进一步压低诅咒桶权重（牌越稀有越低）。执迷：抽到该牌时获得 **2** 点能量。
- **剑化**附魔：耗能+1，伤害+12。(此处为错误，本应当是6)→耗能+1，对随机敌人造成6点伤害。
- **剑化**：用 `EnergyVar`（费用 +1）与 `SwordArtDamage`（随机目标伤害）驱动本地化；费用句与 **盾化**、**笨重**、**执迷** 统一为「此牌费用增加1{Energy:energyIcons()}」（英文：`This card's cost increases by 1{Energy:energyIcons()}` / 附卡摘要 `Cost +1{Energy:energyIcons()}.`）。
- **盾化**、**笨重**、**执迷**：`CanonicalVars` 增加与 `RecalculateValues` 一致的 `EnergyVar(1)`，与上述费用句式对齐。
- binary_format/architecture="x86_64" → "msil"，尝试兼容移植版。

### 修复
- 凡庸附魔联机：仅统计本玩家出牌，队友出牌不再错误累加回合内打出张数。
- 卡牌奖励获得的克隆附魔无法克隆的bug。
- **打击**附魔：`CanonicalVars` 使用 `DamageVar` 时，卡牌预览路径会在 `DamageVar.UpdateCardPreview` 内再次调用 `EnchantDamageAdditive`，与附魔自身加伤叠加，导致 +6 显示/结算成 +12。现改为普通动态变量 `StrikeDmg`（仅文案）+ `EnchantDamageAdditive` 固定返回常量，二者不再重复叠加。
- **剑化**附魔：+6结算为+12的bug。

## 0.3.1

### 修复
- 版本号错误

## 0.3.0

### 新增
- 诅咒品质附魔
- 控制台指令：forcebellreward：本场战斗胜利后，下一次按 CardCreationSource.Encounter 生成的卡牌奖励（普通遭遇战奖励）里，会在第一张能接受铃铛诅咒附魔的候选牌上强制附上「铃铛的诅咒」，并走与平时相同的 三段式遗物发放逻辑。

## 0.2.2

### 新增
- README.md浏览量组件
- 蛇咬附魔将卡牌费用变为2
- 蛇咬附魔兼容更多附魔模组

### 修复
- 抢救附魔无法回复生命的bug
- 蛇咬附魔在非指向性卡牌上时无法给予中毒的bug
- 电击和冰霜附魔提示缺失的问题
- 超巨化可能附着在无伤害造成的卡牌上的bug

## 0.2.1

### 修复
- **灼热 / 运行史牌组崩溃**：`NDeckHistory` 等路径在 `CardModel.FromSerializable` 中按 `current_upgrade_level` 循环升级时，若快照里的升级段数大于该牌**模板默认** `MaxUpgradeLevel`（灼热或其它模组多段升级，但附魔块与当前 `Enchantment` 实例不一致、或附魔 ID 无法识别），会触发 `cannot be upgraded past its MaxUpgradeLevel`。反序列化期间对栈顶 `SerializableCard` 若 `CurrentUpgradeLevel >` 模板 `MaxUpgradeLevel` 则临时抬升上限；读取模板上限时用计数器抑制本 postfix，避免递归误判。
- **运行史整体排版偏移**：`MaxUpgradeLevel` postfix 曾抬到极大常数，原版 `CardModel.Title` 在 `MaxUpgradeLevel > 1` 时会走多段升级标题逻辑，运行史牌组条目又按标题宽度计算行宽，易导致整列位置异常。现改为按灼热/当前升级段数/反序列化所需抬到**最小够用**上限（如 `max(2, CurrentUpgradeLevel)` 或存档段数）。

## 0.2.0

发布日期：2026-04-03

### 新增
- 附魔：灼热（多段升级与牌名显示补丁）；抢救（消耗 + 回血）
- **调试命令** `enchantdeck`：对本局牌库按下标附魔；失败时提示原因，避免无效组合抛未处理异常。
- 灾厄相关附魔本地化补键（如 `BLIGHT_CORRUPTED_ENCHANTMENT.extraCardText`），减轻缺键导致的 `LocException`。


## 0.1.0

发布日期：2026-04-02

### 新增

- 卡牌奖励随机附魔（商店除外），概率与四档稀有度权重可配置；支持 Chimera 式按卡牌稀有度分桶。
- 游戏内 **设置 → 常规** 底部 More Enchant 调节项，持久化至 `%AppData%\SlayTheSpire2\MoreEnchantStandalone\more_enchant_settings.json`。
- 大量自定义附魔：锐锋、与匕首、充电 / 先机、生存者、精简、电击 / 冰霜、幽灵、盾化 / 剑化、铸剑、返回、蛇咬、微电流、Kafka 相关；奇美拉系打击 / 宝石 / 启动 / 精小 / 笨重 / 超巨化 / 固化 / 重刃 / 开悟 / 巩固。
- 部分附魔的 **奖励池过滤**：无「打出时」伤害数值的牌不出现锐锋 / 超巨化 / 重刃；无格挡数值的牌不出现固化。
- **开悟** 使用本回合费用修正（`SetThisTurn`），并刷新动态数值显示。
- 说明文档：`README.md`、本 `CHANGELOG.md`。

### 技术说明

- Harmony 补丁：模型序列化、`CardFactory`、打击牌名、设置面板注入、灼热 `MaxUpgradeLevel` / `Title`、图标路径等（以仓库内实际补丁为准）。
- 无 RitsuLib 依赖；附魔通过 `ModelDb.DebugEnchantments` 与自定义注册合并。

---

## 未发布

（在此记录已合并但未打版本号的改动，发布新版本时将对应条目移到上方并写上版本号与日期。）
