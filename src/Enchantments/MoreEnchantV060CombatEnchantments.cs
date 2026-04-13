using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using MegaCrit.Sts2.Core.ValueProps;
using MoreEnchant;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>坚毅：此牌获得的格挡 ×4/3；打出时消耗一张手牌。仅可附于牌面带打出格挡数值的牌。</summary>
public sealed class SteadfastExhaustEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal BlockMultiplier = 4m / 3m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && CardEnchantEligibility.CardHasMoveBlockNumbers(card);

	public override decimal EnchantBlockMultiplicative(decimal originalBlock, ValueProp props) =>
		ValuePropUtil.IsPoweredCardOrMonsterMoveBlock(props) ? BlockMultiplier : 1m;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		if (player?.PlayerCombatState == null)
			return;

		var hand = player.PlayerCombatState.Hand.Cards;
		if (hand.Count == 0)
			return;

		var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1);
		var picked = await CardSelectCmd.FromHand(choiceContext, player, prefs, null, this);
		var toExhaust = picked.FirstOrDefault();
		if (toExhaust != null)
			await CardCmd.Exhaust(choiceContext, toExhaust);
	}
}

/// <summary>破灭：打出时打出抽牌堆顶一张牌并将其消耗。</summary>
public sealed class RuinAutoPlayEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		if (player?.Creature?.CombatState == null)
			return;

		await CardPileCmd.AutoPlayFromDrawPile(choiceContext, player, 1, CardPilePosition.Top, forceExhaust: true);
	}
}

/// <summary>快速：打出时抽 1 张牌。</summary>
public sealed class QuickDrawEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		if (player?.Creature?.CombatState == null)
			return;

		await CardPileCmd.Draw(choiceContext, 1m, player);
	}
}

/// <summary>放血：打出时失去 3 生命，获得 2 点能量。</summary>
public sealed class BloodlettingEnergyEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal HpLossAmount = 3m;
	private const decimal EnergyGain = 2m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get
		{
			yield return new HpLossVar(HpLossAmount);
			yield return new EnergyVar(2);
		}
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var card = Card;
		var player = card?.Owner;
		var creature = player?.Creature;
		if (card == null || player == null || creature == null)
			return;

		await CreatureCmd.TriggerAnim(creature, "Cast", player.Character.CastAnimDelay);
		await CreatureCmd.Damage(
			choiceContext,
			creature,
			HpLossAmount,
			ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
			card);
		await PlayerCmd.GainEnergy(EnergyGain, player);
	}
}

/// <summary>武装：打出时升级手牌中的一张牌。</summary>
public sealed class ArmingUpgradeEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		if (player?.Creature?.CombatState == null)
			return;

		var toUpgrade = await CardSelectCmd.FromHandForUpgrade(choiceContext, player, this);
		if (toUpgrade != null)
			CardCmd.Upgrade(toUpgrade);
	}
}

/// <summary>恐怖：消耗。打出时施加 99 层易伤。</summary>
public sealed class TerrorVulnerableEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal VulnerableStacks = 99m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new PowerVar<VulnerablePower>(VulnerableStacks); }
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromKeyword(CardKeyword.Exhaust), HoverTipFactory.FromPower<VulnerablePower>() };

	protected override void OnEnchant()
	{
		if (Card != null)
			CardCmd.ApplyKeyword(Card, CardKeyword.Exhaust);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner?.Creature?.CombatState == null)
			return;

		var targets = DebuffTargetUtil.Resolve(Card, cardPlay, Card.CombatState!);
		if (targets == null || targets.Count == 0)
			return;

		await CreatureCmd.TriggerAnim(Card.Owner.Creature, "Cast", Card.Owner.Character.CastAnimDelay);
		if (targets.Count == 1)
			await PowerCmd.Apply<VulnerablePower>(targets[0], VulnerableStacks, Card.Owner.Creature, Card);
		else
			await PowerCmd.Apply<VulnerablePower>(targets, VulnerableStacks, Card.Owner.Creature, Card);
	}
}

/// <summary>战栗：打出时施加 2 层易伤。</summary>
public sealed class ShiverVulnerableEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal VulnerableStacks = 2m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new PowerVar<VulnerablePower>(VulnerableStacks); }
	}

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
		if (targets.Count == 1)
			await PowerCmd.Apply<VulnerablePower>(targets[0], VulnerableStacks, Card.Owner.Creature, Card);
		else
			await PowerCmd.Apply<VulnerablePower>(targets, VulnerableStacks, Card.Owner.Creature, Card);
	}
}

/// <summary>中和：打出时施加 1 层虚弱。</summary>
public sealed class NeutralWeakEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal WeakStacks = 1m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new PowerVar<WeakPower>(WeakStacks); }
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromPower<WeakPower>() };

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner?.Creature?.CombatState == null)
			return;

		var targets = DebuffTargetUtil.Resolve(Card, cardPlay, Card.CombatState!);
		if (targets == null || targets.Count == 0)
			return;

		await CreatureCmd.TriggerAnim(Card.Owner.Creature, "Cast", Card.Owner.Character.CastAnimDelay);
		if (targets.Count == 1)
			await PowerCmd.Apply<WeakPower>(targets[0], WeakStacks, Card.Owner.Creature, Card);
		else
			await PowerCmd.Apply<WeakPower>(targets, WeakStacks, Card.Owner.Creature, Card);
	}
}

/// <summary>背包：打出时抽 1 张牌，再丢弃 1 张牌。</summary>
public sealed class BackpackDrawDiscardEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner is not { } player || player.PlayerCombatState is not { } pcs || player.Creature?.CombatState == null)
			return;

		await CardPileCmd.Draw(choiceContext, 1m, player);

		var hand = pcs.Hand.Cards;
		if (hand.Count == 0)
			return;

		var prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1);
		var picked = await CardSelectCmd.FromHandForDiscard(choiceContext, player, prefs, null, this);
		var toDiscard = picked.FirstOrDefault();
		if (toDiscard != null)
			await CardCmd.Discard(choiceContext, toDiscard);
	}
}

/// <summary>星光：打出时获得 2 点辉星。</summary>
public sealed class StarlightStarsEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal StarsGain = 2m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new StarsVar((int)StarsGain); }
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		if (player?.Creature == null)
			return;

		await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
		await PlayerCmd.GainStars(StarsGain, player);
	}
}

/// <summary>护卫：打出时召唤 5。</summary>
public sealed class EscortSummonEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal SummonAmount = 5m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new SummonVar(SummonAmount); }
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.Static(StaticHoverTip.SummonDynamic, DynamicVars.Summon) };

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		if (player?.Creature == null)
			return;

		await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
		await OstyCmd.Summon(choiceContext, player, SummonAmount, this);
	}
}

/// <summary>多打：力量攻击的伤害段数 +1（与叠层无关）。</summary>
public sealed class ExtraHitEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	public override bool ShowAmount => false;

	public override bool HasExtraCardText => true;

	public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && CardEnchantEligibility.CardHasMoveDamageNumbers(card);
}

/// <summary>碎屑：力量攻击伤害与由此牌获得的格挡 ×1.33；打出时将一张 <see cref="Debris"/>（碎屑）置入手牌。</summary>
public sealed class ShredDebrisEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal DamageMultiplier = 4m / 3m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && CardEnchantEligibility.CardHasMoveDamageNumbers(card);

	public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props) =>
		ValuePropCombatUtil.IsPoweredAttackMove(props) ? DamageMultiplier : 1m;

	public override decimal EnchantBlockMultiplicative(decimal originalBlock, ValueProp props) =>
		ValuePropCombatUtil.IsPoweredAttackMove(props) ? DamageMultiplier : 1m;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromCard<Debris>() };

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		var state = player?.Creature?.CombatState;
		if (player == null || state == null)
			return;

		await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);

		var debris = state.CreateCard<Debris>(player);
		await CardPileCmd.AddGeneratedCardToCombat(debris, PileType.Hand, addedByPlayer: true);
	}
}

internal static class DebuffTargetUtil
{
	internal static List<Creature>? Resolve(CardModel card, CardPlay? cardPlay, CombatState state)
	{
		var hittable = state.HittableEnemies;
		if (cardPlay?.Target != null && hittable.Contains(cardPlay.Target))
			return new List<Creature> { cardPlay.Target };

		return card.TargetType switch
		{
			TargetType.AllEnemies => hittable.ToList(),
			TargetType.AnyEnemy or TargetType.RandomEnemy =>
				SingleFrom(card.Owner.RunState.Rng.CombatTargets.NextItem(hittable)),
			_ => SingleFrom(card.Owner.RunState.Rng.CombatTargets.NextItem(hittable)),
		};
	}

	private static List<Creature>? SingleFrom(Creature? c) =>
		c != null ? new List<Creature> { c } : null;
}
