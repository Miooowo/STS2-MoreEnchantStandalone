using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MoreEnchant.Enchantments;

namespace MoreEnchant;

internal static class EnchantmentRewardRarityUtil
{
	internal static EnchantmentRewardRarity GetForTemplate(EnchantmentModel template)
	{
		if (template is Clone or TezcatarasEmber or Goopy or Glam)
			return EnchantmentRewardRarity.Special;
		if (template is IRewardEnchantRarity withRarity)
			return withRarity.RewardRarity;
		return EnchantmentRewardRarity.Common;
	}
}
