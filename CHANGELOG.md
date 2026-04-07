# 更新日志

格式遵循常见约定：新版本在上；未发行改动可放在 **未发布** 小标题下。

---
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
