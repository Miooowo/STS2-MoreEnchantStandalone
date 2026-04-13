using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MoreEnchant.Enchantments;

namespace MoreEnchant;

/// <summary>在「新卡实例替换了旧实例」的流程中把源牌附魔补到新卡上（变牌、蛋遗物克隆升级等）。</summary>
internal static class ModEnchantmentTransferUtil
{
	internal static void CopyEnchantmentToIfMissing(CardModel? source, CardModel? target)
	{
		if (source?.Enchantment == null || target == null || target.Enchantment != null)
			return;

		var enchCopy = (EnchantmentModel)source.Enchantment.ClonePreservingMutability();
		target.EnchantInternal(enchCopy, enchCopy.Amount);
		target.Enchantment!.ModifyCard();
		target.FinalizeUpgradeInternal();

		if (target.Enchantment is BellCurseEnchantment bell)
			bell.ResetRewardRelicGrantGateForClonedCard();
	}
}
