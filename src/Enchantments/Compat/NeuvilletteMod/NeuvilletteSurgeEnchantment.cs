using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
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

/// <summary>贴纸：每场战斗首次打出时，获得 1 张随机美露莘贴纸。</summary>
public sealed class MelusineStickerEnchantment : NeuvilletteFirstPlayEnchantmentBase
{
	public override EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		NeuvilletteCompat.TryGetMelusineStickerKeyword(out var stickerKeyword)
			? new[] { HoverTipFactory.FromKeyword(stickerKeyword) }
			: Array.Empty<IHoverTip>();

	protected override async Task<bool> TryApplyFirstPlayEffect(PlayerChoiceContext choiceContext, Player owner, Creature self) =>
		await NeuvilletteCompat.TryGrantRandomMelusineSticker(owner, Card);
}

/// <summary>旧案呈堂：每场战斗首次打出时，从弃牌堆选择 1 张牌进行呈堂。</summary>
public sealed class OldCaseSubmittedEnchantment : NeuvilletteFirstPlayEnchantmentBase
{
	public override EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		NeuvilletteCompat.TryGetSubmitKeyword(out var submitKeyword)
			? new[] { HoverTipFactory.FromKeyword(submitKeyword) }
			: Array.Empty<IHoverTip>();

	protected override async Task<bool> TryApplyFirstPlayEffect(PlayerChoiceContext choiceContext, Player owner, Creature self) =>
		await NeuvilletteCompat.TrySubmitCardFromDiscard(choiceContext, owner, Card);
}

/// <summary>最终裁定：费用不会低于 2；虚无；消耗；对非 Boss 敌人直接消灭，对 Boss 改为失去当前生命一半。</summary>
public sealed class FinalJudgmentEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

	protected override void OnEnchant()
	{
		if (Card == null)
			return;

		CardCmd.ApplyKeyword(Card, CardKeyword.Exhaust);
		CardCmd.ApplyKeyword(Card, CardKeyword.Ethereal);
	}

	public override void RecalculateValues()
	{
		if (Card == null || Card.EnergyCost.CostsX)
			return;
		Card.EnergyCost.SetCustomBaseCost(Math.Max(2, Card.EnergyCost.Canonical));
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner?.RunState?.CurrentRoom == null || cardPlay?.Target == null)
			return;

		var isBossTarget = Card.Owner.RunState.CurrentRoom.RoomType == RoomType.Boss && cardPlay.Target.IsPrimaryEnemy;
		if (isBossTarget)
		{
			var hpLoss = Math.Floor(cardPlay.Target.CurrentHp / 2m);
			if (hpLoss > 0m)
			{
				await CreatureCmd.Damage(
					choiceContext,
					cardPlay.Target,
					hpLoss,
					ValueProp.Unblockable | ValueProp.Unpowered,
					null,
					Card);
			}
			return;
		}

		await CreatureCmd.Kill(cardPlay.Target, true);
	}
}

public abstract class NeuvilletteFirstPlayEnchantmentBase : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private bool _pendingFirstPlayInCombat = true;

	public abstract EnchantmentRewardRarity RewardRarity { get; }

	public override bool HasExtraCardText => true;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && NeuvilletteCompat.IsNeuvilletteModAvailable();

	public override Task BeforeCombatStart()
	{
		_pendingFirstPlayInCombat = true;
		return Task.CompletedTask;
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (!_pendingFirstPlayInCombat)
			return;
		if (Card?.Owner is not { Creature: { } self } owner || self.CombatState == null)
			return;

		if (!await TryApplyFirstPlayEffect(choiceContext, owner, self))
			return;

		_pendingFirstPlayInCombat = false;
	}

	protected abstract Task<bool> TryApplyFirstPlayEffect(PlayerChoiceContext choiceContext, Player owner, Creature self);
}
