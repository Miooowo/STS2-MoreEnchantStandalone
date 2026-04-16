using MegaCrit.Sts2.Core.Models;
using MoreEnchant;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>灼热：此牌至少可升级 2 次；+ 与 +2 等显示由 Harmony 补丁处理。</summary>
public sealed class ScorchingEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	public override bool HasExtraCardText => true;

	public override bool CanEnchant(CardModel card)
	{
		if (!base.CanEnchant(card))
			return false;
		if (card.MaxUpgradeLevel <= 0)
			return false;
		if (CardEnchantEligibility.IsScorchingExcludedByCardId(card))
			return false;
		return CardEnchantEligibility.CardNextUpgradeImprovesFaceNumbers(card);
	}
}
