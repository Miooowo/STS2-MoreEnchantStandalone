## 0.7.3

发布日期：2026-04-15

### 变更

- **Beta 附魔**：夹击、恶魔护盾迁至 `src/Enchantments/beta/`，默认不进入随机池；设置 → 常规中「附魔 Beta」开启后参与掷骰，联机以房主为准。
- **精简**：`StreamlineEnchantment` 不再对能力牌生效。
- **文档**：`README.md` 增加 AI 协作说明。
- **工作流**：约定更新 `CHANGELOG` 某发行版本小节时，须同步维护 `release/RELEASE_NOTES_<版本>.md`（与 `MoreEnchantStandalone.csproj` 的 `Version` 一致）；已在 `.cursor/rules/more-enchant-workflow.mdc` 与 `AGENTS.md` 中写明。
- **开发**：新增 Cursor 项目规则 `more-enchant-workflow.mdc`（`alwaysApply: true`），与 `AGENTS.md` 对齐会话与发布流程；`AGENTS.md` 顶部说明与规则文件的主次关系。

### 修复

- **附魔资格**：`CardEnchantEligibility` 恢复格挡/自伤相关判定方法并修复重复定义导致的编译错误。
- **文案（GitHub #6）**：模组 `powers.json`（`zhs` / `eng`）覆盖原版「滑溜」`SLIPPERY_POWER` 描述与 smartDescription，改为「受到伤害 / takes damage」表述，与伤害封顶早于格挡的结算一致。
- **精简（GitHub #5）**：`StreamlineEnchantment` 增加 `CanEnchant`，排除 X 费、负基础费与 0 基础耗能牌。
- **兼容**：铃铛诅咒遗物发放处 `PullNextRelicFromBack` 使用 `Func<RelicModel, bool>` 过滤以匹配新版 sts2。
- **灼热（GitHub #7）**：[`ScorchingEnchantment`](src/Enchantments/ScorchingEnchantment.cs) 用 [`CardEnchantEligibility.CardNextUpgradeImprovesFaceNumbers`](src/CardEnchantEligibility.cs) 模拟升级并比较牌面数理变量与耗能/星耗，取代全文描述对比；仍排除破灭、武装、恶魔护盾、回响形态、杂耍、隐秘匕首等 `Id.Entry`。

### 删除
- **超巨化**附魔的冗余判定。