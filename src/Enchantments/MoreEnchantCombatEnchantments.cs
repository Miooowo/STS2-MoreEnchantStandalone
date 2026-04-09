using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new EnergyVar(1); }
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var c = Card?.Owner?.Creature;
		if (c == null)
			return;

		await CreatureCmd.TriggerAnim(c, "Cast", Card!.Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<EnergyNextTurnPower>(c, DynamicVars.Energy.BaseValue, c, Card);
	}
}

/// <summary>先机：下回合获得 2 点能量。</summary>
public sealed class InitiativeEnergyEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new EnergyVar(2); }
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var c = Card?.Owner?.Creature;
		if (c == null)
			return;

		await CreatureCmd.TriggerAnim(c, "Cast", Card!.Owner.Character.CastAnimDelay);
		await PowerCmd.Apply<EnergyNextTurnPower>(c, DynamicVars.Energy.BaseValue, c, Card);
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

/// <summary>电击：生成 1 个闪电充能球。</summary>
public sealed class ShockChannelEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.Static(StaticHoverTip.Channeling),
		HoverTipFactory.FromOrb<LightningOrb>()
	};

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
	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[]
	{
		HoverTipFactory.Static(StaticHoverTip.Channeling),
		HoverTipFactory.FromOrb<FrostOrb>()
	};

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
	private const int CostIncrease = 1;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get
		{
			yield return new EnergyVar(CostIncrease);
			yield return new BlockVar(7m, ValueProp.Move);
		}
	}

	public override void RecalculateValues()
	{
		if (Card == null || Card.EnergyCost.CostsX)
			return;

		int canonical = Card.EnergyCost.Canonical;
		if (canonical < 0)
			return;

		Card.EnergyCost.SetCustomBaseCost(canonical + CostIncrease);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var creature = Card?.Owner?.Creature;
		if (creature == null)
			return;

		await CreatureCmd.GainBlock(creature, DynamicVars.Block, cardPlay, fast: false);
	}
}

/// <summary>剑化：耗能 +1；打出时对随机敌人造成 6 点伤害。</summary>
public sealed class SwordArtEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const int CostIncrease = 1;

	private const decimal RandomHitDamage = 6m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get
		{
			yield return new EnergyVar(CostIncrease);
			yield return new DynamicVar("SwordArtDamage", RandomHitDamage);
		}
	}

	public override void RecalculateValues()
	{
		if (Card == null || Card.EnergyCost.CostsX)
			return;

		int canonical = Card.EnergyCost.Canonical;
		if (canonical < 0)
			return;

		Card.EnergyCost.SetCustomBaseCost(canonical + CostIncrease);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var card = Card;
		var owner = card?.Owner;
		if (owner?.Creature?.CombatState == null || card == null)
			return;

		var state = card.CombatState!;
		var hittable = state.HittableEnemies;
		if (hittable.Count == 0)
			return;

		var target = owner.RunState.Rng.CombatTargets.NextItem(hittable);
		if (target == null)
			return;

		await DamageCmd.Attack(RandomHitDamage).FromCard(card).Targeting(target)
			.WithHitFx("vfx/vfx_attack_slash", null, "blunt_attack.mp3")
			.Execute(choiceContext);
	}
}

/// <summary>铸剑：打出时铸造[blue]10[/blue]（与战利品等牌的铸造机制一致）。</summary>
public sealed class ForgeSwordEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new ForgeVar(10); }
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromForge();
	public override bool HasExtraCardText => true;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		if (player?.Creature == null)
			return;

		await CreatureCmd.TriggerAnim(player.Creature, "Cast", player.Character.CastAnimDelay);
		await ForgeCmd.Forge(DynamicVars.Forge.BaseValue, player, this);
	}
}

/// <summary>抢救：消耗。打出时回复[blue]4[/blue]点生命。</summary>
public sealed class RescueEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new HealVar(4m); }
	}

	protected override void OnEnchant()
	{
		if (Card != null)
			CardCmd.ApplyKeyword(Card, CardKeyword.Exhaust);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var player = Card?.Owner;
		var creature = player?.Creature;
		if (player == null || creature == null)
			return;

		await CreatureCmd.TriggerAnim(creature, "Cast", player.Character.CastAnimDelay);
		await CreatureCmd.Heal(creature, DynamicVars.Heal.BaseValue);
	}
}
