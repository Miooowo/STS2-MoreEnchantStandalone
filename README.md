![](https://count.getloli.com/@:MoreEnchantStandalone?theme=minecraft)

# MoreEnchantStandalone

「杀戮尖塔 2」模组：**不依赖 RitsuLib**，在卡牌奖励等环节为牌附加多种自定义附魔，并提供游戏内设置与调试命令。

| 项目 | 说明 |
|------|------|
| Mod ID | `MoreEnchantStandalone` |
| 当前版本 | 见 `MoreEnchantStandalone.json` 中 `version` |
| 作者 | ClockCycas |

---

## 功能概览

- **卡牌奖励附魔**：在获得卡牌奖励时，每张候选牌有概率被随机附魔（**商店获得的牌不会**因此附魔）。概率与稀有度权重可在设置中调整。
- **大量自定义附魔**：含战斗类效果、奇美拉式修饰（打击 / 宝石 / 精小 / 笨重 / 超巨化 / 固化 / 重刃 / 开悟 / 巩固等）、锐锋、铸剑、灼热、抢救等（详见游戏内附魔说明）。
- **类型过滤**：部分附魔仅会出现在「能造成伤害」或「能获得格挡」的卡上（如锐锋、超巨化、重刃、固化）。
- **游戏内设置**：主菜单 → **设置 → 常规**，滚到底部 **More Enchant（更多附魔）** 区块，可改概率、奇美拉曲线开关与自定义四档权重；修改会写入配置文件。
- **调试控制台**：在开启游戏调试命令的前提下，可使用 `enchantdeck` 对**牌库**中按索引的牌附魔（见下文）。

---

## 安装

1. 将本模组文件夹复制到游戏目录 `mods/MoreEnchantStandalone/`（或你使用的模组加载路径）。
2. 确保至少包含：`MoreEnchantStandalone.json`、`MoreEnchantStandalone.dll`、`MoreEnchantStandalone.pck`（由工程构建导出）。
3. 在游戏中启用模组后重新开局或按游戏要求重载。

构建与导出 PCK 请参考工程内 `MoreEnchantStandalone.csproj`（需配置本机 `Sts2Dir` / `GodotPath`）。

## 调试控制台命令

需在允许调试指令的环境下使用（与原版 `enchant` 等命令相同前提）。

| 命令 | 说明 |
|------|------|
| `enchant <附魔ID> [层数] [手牌索引]` | **原版**：仅在**战斗中**，给**手牌**附魔。 |
| `enchantdeck <附魔ID> [层数] [牌库索引]` | **本模组**：**不要求战斗**，只要**本局已开始**，给**牌库（Deck）**中 `0` 起始下标的那张牌附魔。 |

附魔 ID 为大写下划线形式（与 `ModelDb` 中 entry 一致，如 `KEEN_EDGE_ENCHANTMENT`）。若目标牌类型或已有附魔冲突，命令会返回错误说明而不会附带魔成功。

---

## 本地化

附魔名称与描述位于 `MoreEnchantStandalone/localization/` 下各语言的 `enchantments.json`。模组另包含对灾厄「腐化」等原版键的补充条目（如 `BLIGHT_CORRUPTED_ENCHANTMENT`），以避免合并本地化后缺键崩溃。

---

## AI 协作

*本仓库使用CursorIDE协作代码处理。*
若使用Cursor、GitHub Copilot等工具在本项目上调整代码，请阅读 [`.cursor/rules/more-enchant-workflow.mdc`](.cursor/rules/more-enchant-workflow.mdc)：行为或资源变更后应更新 `CHANGELOG.md`、与 `MoreEnchantStandalone.csproj` 中 `Version` 一致的 `release/RELEASE_NOTES_<版本>.md`，并按该规则执行构建与打包验证。

---

## 依赖与兼容性

- `MoreEnchantStandalone.json` 中 `dependencies` 为空：不依赖 RitsuLib。
- 使用 Harmony 与游戏公开程序集；若游戏大版本升级，需自行验证并重新编译/导出。

---

## 更新记录

见 **`CHANGELOG.md`**。


## Star History

<a href="https://www.star-history.com/?repos=Miooowo%2FSTS2-MoreEnchantStandalone&type=date&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=Miooowo/STS2-MoreEnchantStandalone&type=date&theme=dark&legend=top-left" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=Miooowo/STS2-MoreEnchantStandalone&type=date&legend=top-left" />
   <img alt="Star History Chart" src="https://api.star-history.com/chart?repos=Miooowo/STS2-MoreEnchantStandalone&type=date&legend=top-left" />
 </picture>
</a>

## Release History

![Release History](https://raw.githubusercontent.com/Miooowo/STS2-MoreEnchantStandalone/main/docs/release-chart.svg)
