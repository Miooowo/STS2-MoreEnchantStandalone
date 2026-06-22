using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Standalone;
using MoreEnchant.Standalone.Compat;

namespace MoreEnchant.Enchantments;

/// <summary>潮涌：打出时先按基础潮涌值回复生命，再施加等量潮落（<c>SurgePower</c>）；含源头活水时与模组 <c>SurgeCard</c> 一致叠加；仅模组已加载时进入奖励池。</summary>
public sealed class SurgeEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal SurgeBase = 3m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
		new DynamicVar[] { new DynamicVar("Surge", SurgeBase) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips
	{
		get
		{
			if (NeuvilletteCompat.TryGetNeuvilletteSurgeKeyword(out var surgeKw))
				return new IHoverTip[] { HoverTipFactory.FromKeyword(surgeKw) };
			return Array.Empty<IHoverTip>();
		}
	}

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && NeuvilletteCompat.IsNeuvilletteModAvailable();

	protected override void OnEnchant()
	{
		if (Card == null || !NeuvilletteCompat.IsNeuvilletteModAvailable())
			return;
		if (NeuvilletteCompat.TryGetNeuvilletteSurgeKeyword(out var surgeKw))
			CardCmd.ApplyKeyword(Card, surgeKw);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var c = Card?.Owner?.Creature;
		if (c == null)
			return;

		await CreatureCmd.TriggerAnim(c, "Cast", Card!.Owner.Character.CastAnimDelay);
		await NeuvilletteCompat.ApplySurgeHealThenTide(c, SurgeBase, Card);
	}
}
