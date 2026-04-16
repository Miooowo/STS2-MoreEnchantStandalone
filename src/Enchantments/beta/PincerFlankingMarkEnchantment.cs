using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MoreEnchant.Enchantments;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments.Beta;

/// <summary>夹击：耗能 +1；对目标施加夹击（其他玩家对该敌人的攻击伤害翻倍）。仅多人。</summary>
public sealed class PincerFlankingMarkEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity, IBetaGatedRewardEnchantment
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromPower<FlankingPower>() };

	public override bool CanEnchant(CardModel card)
	{
		if (!base.CanEnchant(card))
			return false;
		return RunManager.Instance?.NetService.Type.IsMultiplayer() == true;
	}

	public override void RecalculateValues()
	{
		if (Card == null || Card.EnergyCost.CostsX)
			return;

		var canonical = Card.EnergyCost.Canonical;
		if (canonical < 0)
			return;

		Card.EnergyCost.SetCustomBaseCost(canonical + 1);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner?.Creature?.CombatState == null)
			return;

		var targets = DebuffTargetUtil.Resolve(Card, cardPlay, Card.CombatState!);
		if (targets == null || targets.Count == 0)
			return;

		await CreatureCmd.TriggerAnim(Card.Owner.Creature, "Cast", Card.Owner.Character.CastAnimDelay);

		foreach (var t in targets)
			await PowerCmd.Apply<FlankingPower>(t, 2m, Card.Owner.Creature, Card);
	}
}
