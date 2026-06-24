# 附魔品质（稀有度）API

本文档说明如何为自定义附魔声明奖励稀有度（品质），并覆盖硬依赖与软依赖两种接入模式。

## 相关公开类型

- 命名空间：`MoreEnchant.Enchantments`
- 枚举：`EnchantmentRewardRarity`
- 接口：`IRewardEnchantRarity`

```csharp
public enum EnchantmentRewardRarity
{
    Common,
    Uncommon,
    Curse,
    Rare,
    Special,
    Hidden
}

public interface IRewardEnchantRarity
{
    EnchantmentRewardRarity RewardRarity { get; }
}
```

> 说明：附魔模板未实现 `IRewardEnchantRarity` 时，默认按 `Common` 处理。
>
> 说明：`Hidden` 不会参与游戏内随机附魔池（奖励/商店/事件等随机路径均不会被抽中）。若同时安装了独立模组 **EnchantmentCompendium**，且附魔已注册并有本地化标题，仍可在附魔图鉴中展示。

## 方案一：硬依赖（推荐）

适用于：你的模组项目直接引用 `MoreEnchantStandalone`。

### 示例

```csharp
using MoreEnchant.Enchantments;

public sealed class MyRareEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
    public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;
}
```

## 方案二：软依赖（主模组不直接引用 MoreEnchant）

`IRewardEnchantRarity` 是编译期接口，不能在“完全无引用”的主程序集里直接实现。  
软依赖推荐采用“主模组 + 可选桥接程序集”：

1. 主模组（不引用 MoreEnchant）保持可独立运行。
2. 单独创建桥接程序集（引用 MoreEnchant 与你的主模组）。
3. 在桥接程序集中提供继承/包装类型并实现 `IRewardEnchantRarity`，仅在检测到 MoreEnchant 已安装时加载桥接逻辑。

### 桥接思路（示意）

```csharp
// Bridge assembly (hard-reference MoreEnchant)
using MoreEnchant.Enchantments;

public sealed class MyRareBridgeEnchantment : MyBaseEnchantment, IRewardEnchantRarity
{
    public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;
}
```

## 常见问题

### 1) 我能否只用反射给现有类型“动态加接口”？

不能。接口实现属于类型元数据，编译后不能通过普通反射追加到现有类。

### 2) 软依赖情况下不做桥接会怎样？

你的附魔不会声明 `IRewardEnchantRarity`，将回落为默认 `Common`。

### 3) `Curse` 与 `Special` 何时使用？

- `Curse`：用于诅咒档，独立于普通 `Common/Uncommon/Rare`。
- `Special`：用于特殊来源或特殊规则档位（非普通奖励权重语义）。

### 4) `Hidden` 适合什么场景？

- 只希望通过脚本/API/特殊联动显式赋予，不希望进入任何随机池的附魔。
- 用于调试、彩蛋、剧情型或开发者专用附魔，同时保留图鉴可见性。
