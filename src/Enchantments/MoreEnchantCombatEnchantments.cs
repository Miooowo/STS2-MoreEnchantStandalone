using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>与匕首：打出时将一张 <see cref="Shiv"/> 置入手牌。</summary>
public sealed class LikeDaggerEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		if (player?.Creature?.CombatState == null)
			return;

		await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
		await CardPileCmd.AddToCombatAndPreview<Shiv>(player.Creature, PileType.Hand, 1, true);
	}
}

/// <summary>充电：下回合获得 1 点能量。</summary>
public sealed class ChargeUpEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var c = Card?.Owner?.Creature;
		if (c == null)
			return;

		await CreatureCmd.TriggerAnim(c, "Cast", Card!.Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<EnergyNextTurnPower>(c, 1m, c, Card);
	}
}

/// <summary>先机：下回合获得 2 点能量。</summary>
public sealed class InitiativeEnergyEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var c = Card?.Owner?.Creature;
		if (c == null)
			return;

		await CreatureCmd.TriggerAnim(c, "Cast", Card!.Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<EnergyNextTurnPower>(c, 2m, c, Card);
	}
}

/// <summary>生存者：打出时选择并丢弃一张手牌。</summary>
public sealed class SurvivorDiscardEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		if (player?.PlayerCombatState == null)
			return;

		var hand = player.PlayerCombatState.Hand.Cards;
		if (hand.Count == 0 || hand.All(c => c == Card))
			return;

		var prefs = new CardSelectorPrefs(new LocString("enchantments", "SURVIVOR_DISCARD_ENCHANTMENT.selectPrompt"), 1);
		var picked = await CardSelectCmd.FromHandForDiscard(
			choiceContext,
			player,
			prefs,
			c => c != Card,
			this);

		var toDiscard = picked.FirstOrDefault();
		if (toDiscard != null)
			await CardCmd.Discard(choiceContext, toDiscard);
	}
}

/// <summary>精简：本场战斗中每次打出后耗能 -1（可叠）。</summary>
public sealed class StreamlineEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card != Card || Card == null)
			return Task.CompletedTask;
		if (Card.EnergyCost.CostsX || Card.EnergyCost.Canonical < 0)
			return Task.CompletedTask;

		Card.EnergyCost.AddThisCombat(-1);
		Card.DynamicVars.RecalculateForUpgradeOrEnchant();
		return Task.CompletedTask;
	}
}

/// <summary>电击：唤起 1 个闪电充能球。</summary>
public sealed class ShockChannelEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		if (player?.Creature == null)
			return;

		await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
		await OrbCmd.Channel<LightningOrb>(choiceContext, player);
	}
}

/// <summary>冰霜：唤起 1 个冰霜充能球。</summary>
public sealed class FrostChannelEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		if (player?.Creature == null)
			return;

		await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
		await OrbCmd.Channel<FrostOrb>(choiceContext, player);
	}
}

/// <summary>幽灵：<see cref="CardKeyword.Ethereal"/>。</summary>
public sealed class SpectralEtherealEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => false;

	protected override void OnEnchant()
	{
		if (Card != null)
			CardCmd.ApplyKeyword(Card, CardKeyword.Ethereal);
	}
}

/// <summary>盾化：耗能 +1；打出时获得 7 点格挡。</summary>
public sealed class ShieldPlatingEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	private const decimal BlockGain = 7m;

	public override bool HasExtraCardText => true;

	public override void RecalculateValues()
	{
		if (Card == null || Card.EnergyCost.CostsX)
			return;

		int canonical = Card.EnergyCost.Canonical;
		if (canonical < 0)
			return;

		Card.EnergyCost.SetCustomBaseCost(canonical + 1);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var creature = Card?.Owner?.Creature;
		if (creature == null)
			return;

		await CreatureCmd.GainBlock(creature, BlockGain, ValueProp.Move, cardPlay, fast: false);
	}
}

/// <summary>剑化：耗能 +1；此牌造成伤害时额外 +6（与 <see cref="ChimeraStrikeEnchantment"/> 相同伤害通道）。</summary>
public sealed class SwordArtEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	private const decimal BonusDamage = 6m;

	public override bool HasExtraCardText => true;

	public override void RecalculateValues()
	{
		if (Card == null || Card.EnergyCost.CostsX)
			return;

		int canonical = Card.EnergyCost.Canonical;
		if (canonical < 0)
			return;

		Card.EnergyCost.SetCustomBaseCost(canonical + 1);
	}

	public override decimal EnchantDamageAdditive(decimal originalDamage, ValueProp props) =>
		ChimeraAugmentEnchantments.IsMoveDamage(props) ? BonusDamage : 0m;
}

/// <summary>铸剑：打出时铸造[blue]10[/blue]（与战利品等牌的铸造机制一致）。</summary>
public sealed class ForgeSwordEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	private const decimal ForgeAmount = 10m;
	protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromForge();
	public override bool HasExtraCardText => true;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		if (player?.Creature == null)
			return;

		await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
		await ForgeCmd.Forge(ForgeAmount, player, this);
	}
}

/// <summary>抢救：消耗。打出时回复[blue]4[/blue]点生命。</summary>
public sealed class RescueEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	private const decimal HealAmount = 4m;

	public override bool HasExtraCardText => true;

	protected override void OnEnchant()
	{
		if (Card != null)
			CardCmd.ApplyKeyword(Card, CardKeyword.Exhaust);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		if (player?.Creature == null || player.Osty == null)
			return;

		await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
		await CreatureCmd.Heal(player.Osty, HealAmount);
	}
}
