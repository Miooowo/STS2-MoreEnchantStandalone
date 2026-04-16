## 0.7.3

发布日期：2026-04-15

### 变更

- **Beta 附魔**：夹击、恶魔护盾迁至 `src/Enchantments/beta/`，默认不进入随机池；设置 → 常规中「附魔 Beta」开启后参与掷骰，联机以房主为准。
- **精简**：`StreamlineEnchantment` 不再对能力牌生效。

### 修复

- **附魔资格 / 超巨化**：`CardEnchantEligibility` 去掉重复的 `CardHasMoveDamageNumbers` 并保留 `CardHasMoveDamageOrHpLoss` 等；`ChimeraGiganticEnchantment` 将「仅攻击」并入 `CanEnchant`，去掉单独的 `CanEnchantCardType`。
- **文案（GitHub #6）**：模组 `powers.json`（`zhs` / `eng`）覆盖原版「滑溜」`SLIPPERY_POWER` 描述与 smartDescription，改为「受到伤害 / takes damage」表述，与伤害封顶早于格挡的结算一致。
- **精简（GitHub #5）**：`StreamlineEnchantment` 增加 `CanEnchant`，排除 X 费、负基础费与 0 基础耗能牌。
- **灼热（GitHub #7）**：[`ScorchingEnchantment`](src/Enchantments/ScorchingEnchantment.cs) 用 [`CardEnchantEligibility.CardNextUpgradeImprovesFaceNumbers`](src/CardEnchantEligibility.cs) 模拟升级并比较牌面数理变量与耗能/星耗，取代全文描述对比；仍排除破灭、武装、恶魔护盾、回响形态、杂耍、隐秘匕首等 `Id.Entry`。
