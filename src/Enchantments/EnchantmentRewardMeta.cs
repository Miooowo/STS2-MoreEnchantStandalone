namespace MoreEnchant.Enchantments;

/// <summary>与 ChimeraTheSpire 的 <c>ChimeraAugmentRarity</c> 对齐，用于奖励附魔分层抽取。</summary>
public enum EnchantmentRewardRarity
{
	Common,
	Uncommon,
	Rare,
	Special
}

/// <summary>在卡牌奖励等流程中参与按稀有度加权的附魔模板。</summary>
public interface IRewardEnchantRarity
{
	EnchantmentRewardRarity RewardRarity { get; }
}
