using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>灼热：此牌至少可升级 2 次；+ 与 +2 等显示由 Harmony 补丁处理。</summary>
public sealed class ScorchingEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	public override bool HasExtraCardText => true;
}
