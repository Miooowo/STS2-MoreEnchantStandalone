using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MoreEnchant;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>滑溜：本场战斗中首次打出时获得 1 层滑溜。</summary>
public sealed class SlipperyFirstPlayEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal SlipperyStacks = 1m;

	private bool _pendingFirstPlayInCombat = true;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
		new DynamicVar[] { new PowerVar<SlipperyPower>(SlipperyStacks) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromPower<SlipperyPower>() };

	public override Task BeforeCombatStart()
	{
		_pendingFirstPlayInCombat = true;
		return Task.CompletedTask;
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (!_pendingFirstPlayInCombat)
			return;
		var player = Card?.Owner;
		var c = player?.Creature;
		if (c == null)
			return;

		_pendingFirstPlayInCombat = false;
		await CreatureCmd.TriggerAnim(c, "Cast", player!.Character.CastAnimDelay);
		await PowerCmd.Apply<SlipperyPower>(c, SlipperyStacks, c, Card);
	}
}

/// <summary>缓冲：每场战斗中第一次打出这张牌时，获得 1 层缓冲。</summary>
public sealed class GainBufferPowerEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal BufferStacks = 1m;

	private bool _pendingFirstPlayInCombat = true;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Special;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
		new DynamicVar[] { new PowerVar<BufferPower>(BufferStacks) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromPower<BufferPower>() };

	public override Task BeforeCombatStart()
	{
		_pendingFirstPlayInCombat = true;
		return Task.CompletedTask;
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (!_pendingFirstPlayInCombat)
			return;
		var player = Card?.Owner;
		var c = player?.Creature;
		if (c == null)
			return;

		_pendingFirstPlayInCombat = false;
		await CreatureCmd.TriggerAnim(c, "Cast", player!.Character.CastAnimDelay);
		await PowerCmd.Apply<BufferPower>(c, BufferStacks, c, Card);
	}
}

/// <summary>复刻：打出后将一张此牌的消耗复制品置入弃牌堆。</summary>
public sealed class ReplicaExhaustCopyEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromKeyword(CardKeyword.Exhaust) };

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var card = Card;
		var player = card?.Owner;
		var cs = player?.Creature?.CombatState;
		if (card == null || player == null || cs == null)
			return;

		await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

		var copy = cs.CloneCard(card);
		CardCmd.ClearEnchantment(copy);
		if (!copy.Keywords.Contains(CardKeyword.Exhaust))
			CardCmd.ApplyKeyword(copy, CardKeyword.Exhaust);

		await CardPileCmd.Add(copy, PileType.Discard, CardPilePosition.Bottom, Card);
	}
}

/// <summary>熔融：将该敌人身上的易伤层数翻倍。</summary>
public sealed class MeltDoubleVulnerableEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromPower<VulnerablePower>() };

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner?.Creature?.CombatState == null)
			return;

		var targets = DebuffTargetUtil.Resolve(Card, cardPlay, Card.CombatState!);
		if (targets == null || targets.Count == 0)
			return;

		await CreatureCmd.TriggerAnim(Card.Owner.Creature, "Cast", Card.Owner.Character.CastAnimDelay);

		foreach (var t in targets)
		{
			var v = (decimal)t.GetPowerAmount<VulnerablePower>();
			if (v <= 0m)
				continue;
			await PowerCmd.Apply<VulnerablePower>(t, v, Card.Owner.Creature, Card);
		}
	}
}

/// <summary>劫掠：抽牌直到抽到一张非攻击牌。</summary>
public sealed class PlunderDrawEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const int MaxDraws = 120;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	public override bool HasExtraCardText => true;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		if (player?.Creature?.CombatState == null)
			return;

		await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

		for (var i = 0; i < MaxDraws; i++)
		{
			var c = await CardPileCmd.Draw(choiceContext, player);
			if (c == null)
				break;
			if (c.Type != CardType.Attack)
				break;
		}
	}
}

/// <summary>与我一战：获得 2 点力量；每名目标敌人获得 1 点力量。</summary>
public sealed class DuelStrengthEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal PlayerStrength = 2m;
	private const decimal EnemyStrength = 1m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new PowerVar<StrengthPower>(PlayerStrength); }
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromPower<StrengthPower>() };

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner?.Creature?.CombatState == null)
			return;

		var playerC = Card.Owner.Creature;
		await CreatureCmd.TriggerAnim(playerC, "Cast", Card.Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<StrengthPower>(playerC, PlayerStrength, playerC, Card);

		var targets = DebuffTargetUtil.Resolve(Card, cardPlay, Card.CombatState!);
		if (targets == null || targets.Count == 0)
			return;

		foreach (var t in targets)
			await PowerCmd.Apply<StrengthPower>(t, EnemyStrength, playerC, Card);
	}
}

/// <summary>死神：攻击造成伤害时，施加伤害值一半的灾厄。仅伤害牌。</summary>
public sealed class ReaperDoomOnDamageEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromPower<DoomPower>() };

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && CardEnchantEligibility.CardHasMoveDamageNumbers(card);

	public override async Task AfterDamageGiven(
		PlayerChoiceContext choiceContext,
		Creature? dealer,
		DamageResult result,
		ValueProp props,
		Creature target,
		CardModel? cardSource)
	{
		if (cardSource != Card || Card == null)
			return;
		if (dealer != Card.Owner?.Creature)
			return;
		if (!ValuePropCombatUtil.IsPoweredAttackMove(props))
			return;
		if (result.TotalDamage <= 0)
			return;

		var doom = result.TotalDamage / 2;
		if (doom <= 0)
			return;

		await PowerCmd.Apply<DoomPower>(target, doom, dealer, Card);
	}
}

/// <summary>跃跃欲试：耗能 +1；打出时你手牌中每有一张攻击牌，获得 1 点能量。</summary>
public sealed class EagerPerAttackEnergyEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new EnergyVar(1); }
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

	public override async Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (cardPlay.Card != Card || Card?.Owner is not { } owner || owner.PlayerCombatState == null)
			return;

		var n = owner.PlayerCombatState.Hand.Cards.Count(c => c.Type == CardType.Attack);
		if (n <= 0)
			return;

		await PlayerCmd.GainEnergy(n, owner);
	}
}

/// <summary>猛扑：仅攻击且基础耗能不低于 2；打出后下一张技能耗能为 0。</summary>
public sealed class PounceNextSkillFreeEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromPower<FreeSkillPower>() };

	public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

	public override bool CanEnchant(CardModel card)
	{
		if (!base.CanEnchant(card))
			return false;
		if (card.EnergyCost.CostsX)
			return false;
		return card.EnergyCost.Canonical >= 2;
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		var c = player?.Creature;
		if (player == null || c == null)
			return;

		await CreatureCmd.TriggerAnim(c, "Cast", player.Character.CastAnimDelay);
		await PowerCmd.Apply<FreeSkillPower>(c, 1m, c, Card);
	}
}

/// <summary>奇巧：此牌获得奇巧。</summary>
public sealed class SlyKeywordEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => false;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromKeyword(CardKeyword.Sly) };

	protected override void OnEnchant()
	{
		if (Card != null)
			CardCmd.ApplyKeyword(Card, CardKeyword.Sly);
	}
}
