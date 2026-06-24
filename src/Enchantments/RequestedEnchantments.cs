using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MoreEnchant.Powers;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>幽暗烛火：仅诅咒牌。可打出原本不可打出的诅咒；打出时失去 1 点生命并消耗。</summary>
public sealed class DarkCandleEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal HpLossOnPlay = 1m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	public override bool CanEnchant(CardModel card) =>
		CardEnchantEligibility.IsCurseLikeCard(card) &&
		(base.CanEnchant(card) || CardEnchantEligibility.IsCurseBaseCanEnchantBypass(card));

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new DamageVar(HpLossOnPlay, ValueProp.Unpowered | ValueProp.Move); }
	}

	protected override void OnEnchant()
	{
		if (Card == null)
			return;

		// 幽暗烛火允许打出原本不可打出的诅咒：附魔时移除 Unplayable 关键词，避免底层拦截。
		try
		{
			var removeKeyword = typeof(CardCmd).GetMethod(
				"RemoveKeyword",
				BindingFlags.Public | BindingFlags.Static,
				binder: null,
				types: new[] { typeof(CardModel), typeof(CardKeyword) },
				modifiers: null);
			removeKeyword?.Invoke(null, new object[] { Card, CardKeyword.Unplayable });
		}
		catch
		{
			// 某些卡牌关键词集合不可变时忽略，交由 IsPlayable 补丁兜底。
		}
		CardCmd.ApplyKeyword(Card, CardKeyword.Exhaust);
	}

	public override void RecalculateValues()
	{
		if (Card == null || Card.EnergyCost.CostsX)
			return;

		var canonical = Card.EnergyCost.Canonical;
		if (canonical < 0)
			Card.EnergyCost.SetCustomBaseCost(0);
	}

	public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType) =>
		!ReferenceEquals(card, Card) || Card?.Owner != null;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner?.Creature is not { } self)
			return;

		await CreatureCmd.Damage(
			choiceContext,
			self,
			HpLossOnPlay,
			ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
			null,
			Card);
	}
}

/// <summary>萦绕：诅咒附魔。仅诅咒牌；回合开始时若在弃牌堆则回到手牌。</summary>
public sealed class HauntingEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => true;

	public override bool CanEnchant(CardModel card) =>
		CardEnchantEligibility.IsCurseLikeCard(card) &&
		(base.CanEnchant(card) || CardEnchantEligibility.IsCurseBaseCanEnchantBypass(card));

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (Card == null || Card.Owner != player)
			return;
		if (Card.Pile?.Type != PileType.Discard)
			return;

		await CardPileCmd.Add(Card, PileType.Hand, CardPilePosition.Bottom, Card);
	}
}

/// <summary>随机附魔：拾取该牌时，为牌组中的一张其他牌添加随机附魔。</summary>
public sealed class RandomEnchantOnPickupEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private int _pickupTriggeredGate;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => false;

	internal bool TryTakePickupTriggerOnce() =>
		Interlocked.CompareExchange(ref _pickupTriggeredGate, 1, 0) == 0;

	internal void ResetPickupTriggerGateForClonedCard() =>
		Interlocked.Exchange(ref _pickupTriggeredGate, 0);
}

/// <summary>我恨桥：罕见；仅单人；拾取该牌时进入滑脚木桥事件。</summary>
public sealed class HateBridgeEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private int _pickupTriggeredGate;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => false;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && RunManager.Instance?.NetService?.Type.IsMultiplayer() != true;

	internal bool TryTakePickupTriggerOnce() =>
		Interlocked.CompareExchange(ref _pickupTriggeredGate, 1, 0) == 0;

	internal void ResetPickupTriggerGateForClonedCard() =>
		Interlocked.Exchange(ref _pickupTriggeredGate, 0);
}

/// <summary>冰镇：每场战斗第一次抽到时，本回合冻结；下次打出前耗能 -1。</summary>
public sealed class ChilledEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private bool _triggeredThisCombat;
	private int _frozenRound = -1;
	private bool _nextPlayDiscountPending;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<FrozenKeywordPower>()
	];

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new EnergyVar(1); }
	}

	public override Task BeforeCombatStart()
	{
		_triggeredThisCombat = false;
		_frozenRound = -1;
		_nextPlayDiscountPending = false;
		return Task.CompletedTask;
	}

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (_triggeredThisCombat || !ReferenceEquals(card, Card))
			return;
		if (Card?.CombatState == null)
			return;

		_triggeredThisCombat = true;
		_frozenRound = Card.CombatState.RoundNumber;
		_nextPlayDiscountPending = true;
		RecalculateValues();
		Card.InvokeEnergyCostChanged();
		await Task.CompletedTask;
	}

	public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
	{
		if (!ReferenceEquals(card, Card))
			return true;
		if (Card?.CombatState == null)
			return true;
		if (_frozenRound < 0)
			return true;
		return Card.CombatState.RoundNumber != _frozenRound;
	}

	public override void RecalculateValues()
	{
		if (Card == null || Card.EnergyCost.CostsX)
			return;

		var canonical = Card.EnergyCost.Canonical;
		if (canonical < 0)
			return;

		var cost = canonical;
		if (_nextPlayDiscountPending)
			cost = Math.Max(0, cost - 1);
		Card.EnergyCost.SetCustomBaseCost(cost);
	}

	public override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (!ReferenceEquals(cardPlay?.Card, Card))
			return Task.CompletedTask;
		if (!_nextPlayDiscountPending || Card == null)
			return Task.CompletedTask;

		_nextPlayDiscountPending = false;
		RecalculateValues();
		Card.InvokeEnergyCostChanged();
		return Task.CompletedTask;
	}
}

/// <summary>交锋（Clash）：仅攻击牌，耗能变为 0；只有手中全是攻击牌时可打出。</summary>
public sealed class ClashEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

	public override void RecalculateValues()
	{
		if (Card == null || Card.EnergyCost.CostsX)
			return;
		Card.EnergyCost.SetCustomBaseCost(0);
	}

	public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
	{
		if (!ReferenceEquals(card, Card))
			return true;
		var hand = Card?.Owner?.PlayerCombatState?.Hand.Cards;
		if (hand == null)
			return true;

		return hand.All(c => c.Type == CardType.Attack);
	}
}

/// <summary>招牌技（Signature Move）：仅攻击牌；耗能 2；伤害 +10 且最低为 30；仅手中唯一攻击牌时可打出。</summary>
public sealed class SignatureMoveEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal DamageBonus = 10m;
	private const decimal MinimumDamage = 30m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && CardEnchantEligibility.CardHasMoveDamageNumbers(card);

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new DynamicVar("SignatureBonusDamage", DamageBonus); }
	}

	public override void RecalculateValues()
	{
		if (Card == null || Card.EnergyCost.CostsX)
			return;
		Card.EnergyCost.SetCustomBaseCost(2);
	}

	public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props)
	{
		if (!props.HasFlag(ValueProp.Move))
			return 0m;
		var target = Math.Max(originalDamage + DamageBonus, MinimumDamage);
		return target - originalDamage;
	}

	public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
	{
		if (!ReferenceEquals(card, Card))
			return true;
		var hand = Card?.Owner?.PlayerCombatState?.Hand.Cards;
		if (hand == null)
			return true;

		var attackCount = hand.Count(c => c.Type == CardType.Attack);
		return attackCount == 1;
	}
}
