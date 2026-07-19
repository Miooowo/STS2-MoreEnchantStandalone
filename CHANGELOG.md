# 更新日志

格式遵循常见约定：新版本在上；未发行改动可放在 **未发布** 小标题下。

## 0.12.3-beta

发布日期：2026-07-19

### 新增
- RitsuLib 设置新增「仅允许指定附魔」白名单（按 ID / 中英标题匹配）与「附魔黑名单」逐项开关，控制随机附魔池。

### 变更
- 打击附魔：中文卡名已含「打击」时只追加「击」，避免「完美打击打击」一类重复。

### 修复
- 适配打出后去向钩子：`ModifyCardPlayResultPileTypeAndPosition` 更名为 `ModifyCardPlayResultLocation`（返回 `CardLocation`）。

## 0.12.2-beta

发布日期：2026-07-08

### 新增
- 新增7个附魔图标：充电，冰霜，合成，微电流，先机，幽灵，与小刀（感谢企鹅）

### 变更
- 简体中文下，无专属图标的附魔兜底图标改为附魔书（`enchanted_book.gif`；因 Godot 无法导入 GIF，游戏内使用同目录导出的 `enchanted_book.png` 纹理）。

### 修复
- 适配新版 `Hook.BeforeSideTurnEnd` / `Hook.AfterSideTurnEnd`（替换已移除的 `BeforeTurnEnd` / `AfterTurnEnd`），修复模组初始化时 Harmony 补丁崩溃；Postfix 第三参须命名为 `participants` 以匹配游戏方法签名。
- 适配 `CreatureCmd.Damage` 与 `AttackCommand.FromCard` 新签名（`CardModel` + `CardPlay?`），修复编译与运行时 API 不兼容。

## 0.12.1

发布日期：2026-06-26

### 新增
- 依赖于RitsuLib的模组设置。

## 0.12.0

发布日期：2026-06-22

### 新增
<!-- - 新增附魔图鉴对外注册 API：`EnchantmentCompendiumApi`，支持 `RegisterEnchantmentType(Type)`、`RegisterEnchantmentType<T>()` 与 `TryRegister...` 软依赖调用。 -->
- 新增星引擎（ReAstralParty）联动附魔 5 个：`以毒攻毒`、`灵魂链接`、`错误的目标`、`支援口香糖`、`能量补充棒`（含多人目标选择与联机同步）。
<!-- - 新增开发者文档目录 `docs/mod-integration/`（含图鉴 API、品质 API、硬/软依赖示例与验证清单）。 -->
- 新增酒狐（`STS2_WineFox`）联动附魔 3 个：`合成`（赋予合成关键词）、`物流`（首打将本牌非附魔复制品加入随机其他玩家手牌）、`手摇曲柄`（首打获得 2 层活力）。
- 新增 6 个附魔：`幽暗烛火`（诅咒可打出并自损 1 后消耗）、`萦绕`（回合开始回手）、`随机附魔`（拾取时给牌组另一张牌随机附魔）、`冰镇`（首抽本回合冻结并下次打出前减费 1）、`交锋`（0 费且仅手牌全攻击可打）、`招牌技`（2 费、伤害 +10 且最低 30、仅手中唯一攻击牌可打）。
- 新增附魔品质 `Hidden`：不会进入游戏随机附魔池，但仍可在附魔图鉴中展示；
<!-- 并补充对应品质 API 文档说明。 -->
- 新增附魔 `我恨桥`（罕见、仅单人）：拾起该牌时触发并进入 `滑脚木桥` 事件。
- 新增那维莱特联动附魔 3 个：`贴纸`（首打获得随机美露莘贴纸）、`旧案呈堂`（首打从弃牌堆选 1 张牌呈堂）、`最终裁定`（攻击牌费用下限 2，附加虚无与消耗；非 Boss 斩杀，Boss 失去当前生命一半）。

### 变更
<!-- - 图鉴条目收集链路支持外部注册来源，统一按 `ModelId` 去重并保持既有过滤规则（排除 mock/deprecated、缺本地化标题不显示）。 -->
- 调整 `HellraiserEnchantment`（地狱狂徒）：拥有该附魔的牌现在按能力牌打出，打出后去向改为 `PileType.None`。

### 移除
- 附魔图鉴 UI、`EnchantmentCompendiumApi` 及图鉴集成文档已拆至独立模组 **EnchantmentCompendium**；需图鉴功能请另行安装该模组。MoreEnchant 附魔仍通过 `ModelDb.DebugEnchantments` 补丁在图鉴中展示（与图鉴模组同时安装时）。

### 修复
- 修复海克斯临时符文联动在“进入下一个房间”后可能无法正确移除的问题：临时符文跟踪从对象引用改为 `ModelId`，并按数量匹配移除，避免跨房间实例重建导致漏删。

---
## 0.11.1-fix2

发布日期：2026-06-19

### 修复
- 修复海克斯临时符文联动在联机中可能导致 checksum 分叉的问题：将临时符文发放/移除链路改为保持主线程上下文执行（移除 `ConfigureAwait(false)`），避免单端异步时序导致遗物状态不一致（如一端残留 `COSPLAY_RUNE`）。
- 修复联机下 **笨拙（Clumsy）** 会被队友出牌误触发弃牌的问题：现在仅当牌主人打出“另一张牌”时才触发。
- 修复联机下 **恶魔护盾（Demon Shield）** 目标选择结果不同步导致格挡发放分叉的问题：改为使用 `PlayerChoiceSynchronizer` 显式同步目标索引（`ReserveChoiceId/SyncLocalChoice/WaitForRemoteChoice`）。
- 腐朽附魔的中文描述问题。

---
## 0.11.1-fix

发布日期：2026-06-16

### 修复
- 修复联机新局创建阶段（初始卡组随机附魔）可能触发的空引用崩溃：Beta 附魔多人判定改为对 `RunManager.Instance.NetService` 进行空安全访问，避免 `CanEnchant` 在网络服务尚未就绪时抛出 `NullReferenceException`。

---
## 0.11.1

发布日期：2026-06-01

### 新增
- 究极打击、究极防御和碎屑附魔的图标（来自礼帽人）

### 移除
- 移除游戏内 More Enchant 设置面板；附魔行为改为固定模式：所有来源（奖励/商店/先古/战斗生牌/变牌/直加牌组/初始卡组）默认开启，统一使用默认附魔概率。

## 0.11.0

发布日期：2026-05-29

### 新增
- 新增稀有附魔 `Soul_Detachment（灵魂抽离）`：仅可附于可造成打出伤害的攻击牌；每场战斗首次打出时，对目标抽离灵魂。
- 新增灵魂链接能力 `SoulDetachmentLinkPower`：灵魂受到的实际伤害会单向同步到本体，并包含防重入保护避免递归伤害。
- 新增灵魂视觉补丁：灵魂实体以低透明度显示，战斗中维持持续晕眩表现。

### 变更
- 版本号升级至 `0.11.0`（`MoreEnchantStandalone.csproj` 与 `MoreEnchantStandalone.json`）。
- 中英文本地化新增 `SOUL_DETACHMENT_ENCHANTMENT.*` 键，覆盖标题、描述与额外卡面文本。

### 修复
- 修复 `SoulDetachmentEnchantment` 在战斗内错误对 mutable 怪物模型再次 `ToMutable()` 导致的运行时异常。
- 修复灵魂持续晕眩控制在战斗收尾边界时可能扰动结算的问题（补晕时机与结算保护收敛）。
- 修复千足虫等多段敌人场景下“本体死亡后灵魂未及时清理且可能出现非晕眩意图”的问题（新增死亡后即时清理补丁）。
- 新增战败结算容错：当历史记录中出现空 `ModelId`（遭遇/角色）时，跳过统计写入并为死亡语录提供安全兜底，避免 `ArgumentNullException` 导致 GameOver 界面崩溃。
- 修复联机 `EVENT.NEOW` 退出时变牌附魔路径仅在本机执行导致 `Transformations` RNG 计数不一致、触发 checksum 分歧的问题（改为各端一致执行）。
- 修复联机战斗中生牌附魔路径仅本机执行导致卡牌附魔状态不同步（例如 `MASTER_OF_STRATEGY`）并引发 `Transformations` RNG 计数分歧的问题（改为各端一致执行）。
- 修复联机“直加牌组随机附魔”（事件/遗物/卷轴箱等）潜在仅本机执行导致 `Rewards` RNG 分歧的风险（改为各端一致执行）。
- 修复联机 `AfterCardGeneratedForCombat` 参数可见性在不同端不一致时可能导致一端跳过“玩家生牌附魔”判定的问题（新增 `card.Owner is Player` 兜底，避免再次出现 `MASTER_OF_STRATEGY` 附魔状态分叉）。
- 修复联机中“他人玩家生牌”场景下 `card.Owner` 可能不可用导致附魔逻辑早退的问题：`AfterCardGeneratedForCombat` 现优先使用 `creator(Player)` 作为 RNG 归属，并以 `Owner` 兜底，避免 `SWORD_SAGE` 这类回合开始生牌出现单端附魔与 `Transformations` 计数漂移。

---
## 0.10.1

### 变更
- 生活质量：`精小（ChimeraCompactEnchantment）` 附魔卡牌视觉缩放为 `0.8x`，`笨重（ChimeraBulkyEnchantment）` 附魔卡牌视觉缩放为 `1.2x`。

### 移除
- 彻底移除 `NeutralWeakEnchantment` 代码实现。

### 修复
- 修复图鉴悬浮文本在 canonical 附魔模型上读取 `Card` 导致 `CanonicalModelException` 的崩溃（`ChimeraCompactEnchantment` 文案补丁增加安全访问兜底）。

---

## 0.10.0

发布日期：2026-05-28

### 新增
- **熔合者事件扩展（AMALGAMATOR）**：新增两个选项——`注入打击`（移除 2 张打击并为 1 张攻击牌附上 **究极打击**）与 `注入防御`（移除 2 张防御并为 1 张带格挡的技能牌附上 **究极防御**）。
- **事件型附魔**：新增 `究极打击`（打出伤害 +14）与 `究极防御`（打出格挡 +11），仅通过事件获取，不进入随机附魔池。

### 变更
- **版本升级**：`MoreEnchantStandalone.csproj` 与 `MoreEnchantStandalone.json` 版本统一提升为 `0.10.0`。
- **附魔平衡**：`宝石` 调整为“每场战斗首次打出时重放 2”。
- **附魔品质**：`imbued`、`instinct` 调整为 **特殊品质**（与 `tezcataras_ember`、`goopy`、`clone`、`glam` 对齐）。
- **皇室认证（royally_approved）**：仅允许在商店随机附魔路径出现。
- **蛇咬重做**：基础中毒为 7、升级后中毒为 10；蛇咬牌升级后不再降费（升级完成后会重新矫正为固定费用）。
- **beta 发布目录**：`PackGithubRelease` 输出目录从 `release/` 迁移为 `pre-release/`，并同步 `.gitignore` 与工作流规则路径及 zip 命名说明。
- **宝石附魔 ID**：将宝石附魔模型 ID 统一为 `GemEnchantment`，同步更新注册与本地化键名。

### 移除
- **附魔移除**：删除 `中和（Neutral）` 附魔的注册与本地化，不再进入附魔池。

### 修复
- **104+ 兼容编译**：适配 `AfterCardGeneratedForCombat` 的 104+ 回调签名，修复相关 `CS0115` 覆写错误。
- **墨影（Inky）随机附魔资格**：墨影仅可附于具备打出伤害数值的牌，避免附到无伤害牌。
- **蛇咬升级预览**：蛇咬牌在“查看升级”时不再显示降费，并会正确预览中毒层数由 7 提升到 10。

---
## 0.9.1

发布日期：2026-05-22

### 新增
- **初始卡组随机附魔设置**：新增 `starting_deck_enchant_enabled`（默认 `false`）与 `starting_deck_enchant_chance_percent`（默认 `10`）。开启后在进入新局时，对初始卡组每张牌按概率尝试随机附魔。
- **海克斯符文联动附魔**：新增 4 个联动附魔（锻造器、白银海克斯、黄金海克斯、棱彩海克斯）。未安装海克斯符文 mod 时不会进入随机附魔池。
- **锻造器附魔拾牌触发**：覆盖奖励、商店、事件/遗物直加、变牌后入组等路径；拾起带该附魔的牌时获得 1 个随机属性锻造器。
- **临时海克斯发放与过房清理**：白银/黄金/棱彩海克斯附魔在每场首次打出时发放 1 个对应阶位临时海克斯，并在进入下一个房间后自动移除。
- **调试控制台命令**：新增 `forcehextechforgereward`，可在当前战斗内强制“本场结束后的遭遇战卡牌奖励”至少出现 1 张锻造器附魔牌（若有可附魔候选）。

### 修复
- **103.2 版本 API 适配**：恢复 `BeforeTurnEnd` / `EnchantBlockMultiplicative` / `AfterCardGeneratedForCombat` 等原生覆写链，移除临时 `IAfterCardGeneratedForCombatCompat` 二次分发，避免效果在 103.2 下缺失或重复触发。
- **战斗内生牌反射兼容参数错位**：`CardPileCmdCompat.AddGeneratedCardToCombat` 改为按参数类型动态装配实参，避免在部分签名下把 `bool addedByPlayer` 误传为 `CardPilePosition` 并导致 `Object of type 'System.Boolean' cannot be converted to type 'CardPilePosition'` 异常。
- **锻造器附魔 + 宾邦双入组少发放**：锻造器拾牌触发由“一次性门闩”改为“按牌组中同卡引用出现次数发放”，修复同一实例被重复加入牌组时只发放 1 次的问题。
- **附魔图鉴悬浮提示空引用告警**：`NHoverTipSet.CreateAndShow(...)` 返回值改为判空后再 `SetFollowOwner()`，消除 `CS8602` 并避免潜在 NRE。

---
## 0.9

发布日期：2026-05-20

### 删除
- **锐锋（Keen Edge）** 附魔：移除注册、实现与中英文文案，不再进入随机附魔池。

### 修复
- **联机 checksum 分叉（Neow 事件后）**：兼容新版 `Hook.AfterCardGeneratedForCombat(..., Player creator)` 时，玩家生成判定改为 `creator != null`，避免跨端对象引用差异导致 RNG 计数不一致。

---
## 0.8

发布日期：2026-04-18

### 新增
- **那维莱特联动**：[`NeuvilletteCompat`](src/Standalone/Compat/NeuvilletteCompat.cs) 检测 CharMod / 资源路径；[`SurgeEnchantment`](src/Enchantments/NeuvilletteSurgeEnchantment.cs) 打出时对玩家施加潮涌（`SurgePower`）3 层，仅模组可用时进入奖励池。
- **稀有附魔**（[`MoreEnchantV080CombatEnchantments`](src/Enchantments/MoreEnchantV080CombatEnchantments.cs)）：狂宴（斩杀攻击伤害致死 +3 最大生命，消耗）、巨像（本回合带易伤的敌人对你伤害 ×0.5）、地狱狂徒（抽到带打击标签的牌时对随机敌人自动打出）、腐蚀波 / 灾厄波 / 铸剑波（打出后本回合每次抽牌：全体敌人中毒 2 / 灾厄 3 / 铸造 4）。

### 修复
- **铃铛诅咒**（[`BellCurseReward`](src/Enchantments/CurseEnchantments.cs)）：`GrantCore` 经 `PullNextRelicFromBack` 固定各发放 1 件普通、罕见、稀有遗物；第三参谓词为 **true 表示可抽出**，以 **`ModelId`** 排除磨刀石（此前误当作「跳过」导致仅磨刀石可抽、其余回落头环）；与 `GrantCoreAfterUiFrame` 一并缓解战后选牌界面卡死、遗物未入账（GitHub #14）。
- **恐怖**附魔在非指向性卡牌上无法给予易伤的bug
- 能力牌和无数值的牌也会获得**笨重**的bug

---

## 0.7.5

发布日期：2026-04-17

### 补充
- **复刻**描述补充说明不复制附魔。

### 修复
- **联机 checksum（GitHub #10）**：[`PlayerCombatStateRecalculateAllPlayersInMultiPatch`](src/Patches/PlayerCombatStateRecalculateAllPlayersInMultiPatch.cs) 在多人下于 `PlayerCombatState.RecalculateCardValues` 之后为**其他玩家**补跑同等刷新，使改费附魔的 `RecalculateValues` / `SetCustomBaseCost` 在双方客户端与宿主一致（本体 `CombatStateTracker` 仅对本机 `GetMe` 调用原方法）。

---

## 0.7.4

发布日期：2026-04-16

### 新增
- **感染**附魔图标，来自铃铛蔷薇

### 修复
- **精小（GitHub #9）**：[`CardEnchantEligibility.CardUsesStarCost`](src/CardEnchantEligibility.cs) 判定牌是否具有辉星机制；[`ChimeraCompactEnchantment`](src/Enchantments/ChimeraAugmentEnchantments.cs) `CanEnchant` 排除「非 X、基础耗能已为 0、且无辉星」的牌；[`ChimeraCompactEnchantmentTextPatch`](src/Patches/ChimeraCompactEnchantmentTextPatch.cs) 在无辉星牌上将说明/卡面紫字改为 `description_noStars` / `extraCardText_noStars`（[`enchantments.json`](MoreEnchantStandalone/localization/zhs/enchantments.json) `zhs` / `eng`），避免 `{Stars:starIcons()}` 为空时辉星句断裂。
---

## 0.7.3

发布日期：2026-04-15

### 变更
- **仓库**：删除根目录 `AGENTS.md`；[`release/`](release/) 仅保留当前版本 `0.7.3` 的 `RELEASE_NOTES_0.7.3.md` 与 `MoreEnchantStandalone-0.7.3.zip`，移除往期 RELEASE_NOTES 与旧版 zip。
- **Beta 附魔**：夹击、恶魔护盾迁至 [`src/Enchantments/beta/`](src/Enchantments/beta/)，实现 [`IBetaGatedRewardEnchantment`](src/Enchantments/beta/IBetaGatedRewardEnchantment.cs)；[`MoreEnchantSettings.BetaRewardEnchantmentsEnabled`](src/MoreEnchantSettings.cs) 默认关闭，[`MoreEnchantCardRewardUtil.RollEnchantmentTemplate`](src/MoreEnchantCardRewardUtil.cs) 在未开启时不纳入随机池；设置页复选框见 [`MoreEnchantGeneralSettingsPanelPatch`](src/Patches/MoreEnchantGeneralSettingsPanelPatch.cs)，联机仍随房主 `InitialGameInfo` 快照。
- **精简**：[`StreamlineEnchantment`](src/Enchantments/MoreEnchantCombatEnchantments.cs) 通过 `CanEnchantCardType` 排除能力牌（`CardType.Power`）。
- **文档**：[`README.md`](README.md) 增加 AI 协作说明。
- **工作流**：约定更新 [`CHANGELOG.md`](CHANGELOG.md) 某发行版本小节时须同步维护 [`release/RELEASE_NOTES_<版本>.md`](release/RELEASE_NOTES_0.8.md)（与 `MoreEnchantStandalone.csproj` 的 `Version` 一致）；完整流程见 [`.cursor/rules/more-enchant-workflow.mdc`](.cursor/rules/more-enchant-workflow.mdc)（`alwaysApply: true`）。

### 修复
- **附魔资格 / 超巨化**：[`CardEnchantEligibility`](src/CardEnchantEligibility.cs) 移除错误的重复 `CardHasMoveDamageNumbers` 定义，保留 `CardHasMoveDamageOrHpLoss`（供华丽等）与 `CardHasMoveBlockNumbers` 等；[`ChimeraGiganticEnchantment`](src/Enchantments/ChimeraAugmentEnchantments.cs) 去掉单独的 `CanEnchantCardType`，将「仅攻击」并入 `CanEnchant`，与 `CardHasMoveDamageNumbers` 一并判定。
- **文案（GitHub #6）**：模组 [`localization/zhs/powers.json`](MoreEnchantStandalone/localization/zhs/powers.json) 与 [`localization/eng/powers.json`](MoreEnchantStandalone/localization/eng/powers.json) 覆盖原版「滑溜」`SLIPPERY_POWER` 的 `description` / `smartDescription`，改为「受到伤害」表述，与伤害封顶早于格挡的结算一致。
- **精简（GitHub #5）**：[`StreamlineEnchantment`](src/Enchantments/MoreEnchantCombatEnchantments.cs) 增加 `CanEnchant`，排除 X 费、负基础费与基础耗能 `Canonical <= 0` 的牌，与 `AfterCardPlayed` 行为一致，避免奖励池无意义附魔。
- **灼热（GitHub #7）**：[`ScorchingEnchantment`](src/Enchantments/ScorchingEnchantment.cs) 在 `CanEnchant` 中调用 [`CardEnchantEligibility.CardNextUpgradeImprovesFaceNumbers`](src/CardEnchantEligibility.cs)：对可升级牌 `MutableClone` 后执行一次 `UpgradeInternal` + `FinalizeUpgradeInternal`，比较耗能（非 X）、星耗（非 X）、各数理 `DynamicVar.BaseValue`（跳过 `StringVar`）及 `HpLoss`（降低视为收益），不再依赖全文描述字符串对比；仍按 `Id.Entry` 表排除破灭、武装、恶魔护盾、回响形态、杂耍、隐秘匕首等。

---

## 0.7.2

发布日期：2026-04-15

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

### 修复
- 修复 `灵魂链接`：补齐打牌选敌箭头流程并持久绑定目标；你失去生命时，已链接目标会失去等量生命。
- 修复 `错误的目标`：打出后正确进入“能力态”，现在能在获得负面效果时将该负面转移给随机敌人。
- 修复 `合成`：增强对 WineFox 新旧关键词定义的兼容；打出时会调用酒狐 `CraftIntoHand` 合成流程，避免“有词条无效果”。
- 修复那维莱特联动兼容探测（程序集/类型名）：`贴纸`、`旧案呈堂`、`最终裁定` 现在可正常附魔到可用卡牌。
- 调整 `随机附魔`：拾取后由“随机选一张其他牌”改为“手动选择一张其他牌，再附加随机附魔”。
- 调整 `灵魂链接` 与 `错误的目标`：改为可见的战斗内 Power 展示，并修复负面转移与链接伤害结算稳定性。
- 修复启动期 `DuplicateModelException`：将上述两个 Power 的内部模型类型名改为模组前缀命名，避免与其他模组同名 Power 冲突。
- 修复 `合成` 在部分情况下仅显示说明不落卡面关键词的问题：现在会强制补齐卡面 `合成` 关键词。
- 修复 `灵魂链接` 目标可见性：现在会对被链接目标施加可见的“目标标记”Power，便于队友识别。
- 修复 `FIGHT_FIRE_WITH_FIRE_HEAL_POWER` 本地化缺失警告：补齐中英标题与描述键。
- 调整 `灵魂链接（目标）` 文案：补充 `smartDescription`，支持显示 `[gold]{OwnerName}[/gold]` 动态名称。
- 调整 `灵魂链接（目标）` 动态名称来源：改为显示施加者名称（`{LinkSourceName}`），不再显示目标自身名称。
- 调整 `合成` 附魔卡面文案：当卡牌带有 `合成` 附魔时，描述首行会自动插入 `[gold]合成[/gold]`。
- 调整 `合成` 附魔首行提示为本地化键：新增 `SYNTHESIS_ENCHANTMENT.craftLeadText`（中英）。
- 修复 `幽暗烛火` 与 `萦绕` 无法附魔到 `进阶之灾（ASCENDERS_BANE）` 的问题；现在该特例可正常附魔并生效。
- 修复 `幽暗烛火` 与 `萦绕` 对部分诅咒类负面牌（如 `受伤/INJURY`）判定过严的问题；现在会按诅咒类卡规则正确允许附魔。
- 修复 `幽暗烛火` 对“不可打出”诅咒的放行不稳定问题：兼容多附魔识别，并在附魔时移除 `Unplayable` 关键词，确保可打出逻辑生效。
- 修复 `幽暗烛火` 可打出判定漏拦截路径：新增对 `CardModel.CanPlay`（含 `reason` 重载）的放行补丁，避免仅改 `IsPlayable` 仍被底层拒绝。
- 修复 `物流（LOGISTICS_ENCHANTMENT）` 联机下首打效果导致的 RNG 计数漂移（`CombatTargets`）：远端在接收同步索引后补做等量 RNG 对齐，避免 checksum 分歧。
- 修复 `物流（LOGISTICS_ENCHANTMENT）` 对诅咒牌附魔判定过严的问题：现在联机下允许为诅咒类卡添加该附魔。
- 修复 RitsuLib 模组设置文案硬编码问题：MoreEnchant 设置页标题与各项描述改为本地化键（中英双语）。
- 调整 RitsuLib 设置顺序：将“奖励附魔概率”置顶，并将“初始卡组附魔”移至第二组；新增五档稀有度权重自定义滑条（普通/罕见/诅咒/稀有/特殊），并修复旧迁移逻辑会覆盖玩家设置的问题；自定义权重仅在关闭“按卡牌稀有度使用默认权重”时可编辑。
- 修复“调整附魔奖励概率却不出现附魔”问题：奖励/商店/先古/战斗生牌/变牌/直加牌组/初始卡组等所有附魔入口改为统一读取当前有效设置（含联机房主同步），并同步应用 Beta 附魔池开关。

### 新增
- 新增调试指令 `forcehatebridgereward`：下一次遭遇战卡牌奖励中必定出现一张可附上 `我恨桥` 的附魔牌（若存在可附目标）。
- 新增调试指令 `forcerandomenchantreward`：下一次遭遇战卡牌奖励中必定出现一张可附上 `随机附魔` 的附魔牌（若存在可附目标）。
- 为 `冰镇` 增加“冻结”悬停说明，并去除附魔描述中的重复括号解释文本。
- 为 `贴纸` 与 `旧案呈堂` 补充那维莱特关键词 HoverTip（`美露莘贴纸`、`呈堂`）。