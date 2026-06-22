namespace MoreEnchant.Enchantments;

/// <summary>与 ChimeraTheSpire 的 <c>ChimeraAugmentRarity</c> 对齐，用于奖励附魔分层抽取。</summary>
public enum EnchantmentRewardRarity
{
	Common,
	Uncommon,
	/// <summary>诅咒档：比 <see cref="Uncommon"/> 难随、比 <see cref="Rare"/> 易随（奖励分桶权重介于两者之间）。</summary>
	Curse,
	Rare,
	Special,
	/// <summary>隐藏档：不会参与游戏内随机附魔池，但仍可在附魔图鉴中展示（若满足图鉴展示条件）。</summary>
	Hidden
}

/// <summary>在卡牌奖励等流程中参与按稀有度加权的附魔模板。</summary>
public interface IRewardEnchantRarity
{
	EnchantmentRewardRarity RewardRarity { get; }
}
