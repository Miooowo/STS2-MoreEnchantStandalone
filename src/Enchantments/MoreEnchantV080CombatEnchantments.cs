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
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MoreEnchant;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>狂宴：消耗。本牌攻击击杀非爪牙敌人时获得 3 点最大生命（仅带打出伤害的牌；爪牙见 <see cref="MinionPower"/>）。</summary>
public sealed class FeedEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal MaxHpPerKill = 3m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] {
			HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
			HoverTipFactory.Static(StaticHoverTip.Fatal)
		};


	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && CardEnchantEligibility.CardHasMoveDamageNumbers(card);

	protected override void OnEnchant()
	{
		if (Card != null)
			CardCmd.ApplyKeyword(Card, CardKeyword.Exhaust);
	}

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
		if (dealer is not { } attacker || attacker != Card.Owner?.Creature)
			return;
		if (!ValuePropCombatUtil.IsPoweredAttackMove(props))
			return;
		if (result.TotalDamage <= 0 || !result.WasTargetKilled)
			return;
		if (!target.IsEnemy || target.HasPower<MinionPower>())
			return;

		await CreatureCmd.GainMaxHp(attacker, MaxHpPerKill);
	}
}

/// <summary>巨像：打出后给予 <see cref="ColossusPower"/>（与 <see cref="Colossus"/> 卡牌一致）。</summary>
public sealed class ColossusEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal ColossusStacks = 1m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromPower<ColossusPower>() };

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner is not { Creature: { } creature } owner)
			return;

		await CreatureCmd.TriggerAnim(creature, "Cast", owner.Character.CastAnimDelay);
		await PowerCmd.Apply<ColossusPower>(creature, ColossusStacks, creature, Card);
	}
}

/// <summary>本回合打出后：每次你抽到另一张牌时触发子类效果（与原版 <see cref="CorrosiveWavePower"/> 一致，不依赖 <c>fromHandDraw</c>；<c>CardPileCmd.Draw</c> 默认其为 false）。</summary>
public abstract class DrawWaveEnchantmentBase : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private bool _listenDrawsThisTurn;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	/// <summary>战斗中打出克隆时 <c>cardPlay.Card</c> 可能与附魔宿主 <see cref="EnchantmentModel.Card"/> 引用不同；用 <see cref="CardModel.DeckVersion"/> 对齐。</summary>
	private static bool IsPlayedEnchantHost(CardModel? host, CardModel? played)
	{
		if (host == null || played == null)
			return false;
		if (ReferenceEquals(played, host))
			return true;
		if (ReferenceEquals(played.DeckVersion, host))
			return true;
		if (ReferenceEquals(played, host.DeckVersion))
			return true;
		return false;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (IsPlayedEnchantHost(Card, cardPlay.Card))
			_listenDrawsThisTurn = true;
		return Task.CompletedTask;
	}

	public override Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
	{
		if (Card?.Owner == player)
			_listenDrawsThisTurn = false;
		return Task.CompletedTask;
	}

	protected bool ShouldHandleDraw(CardModel drawn)
	{
		if (!_listenDrawsThisTurn || Card == null)
			return false;
		if (ReferenceEquals(drawn, Card))
			return false;
		if (IsPlayedEnchantHost(Card, drawn))
			return false;
		if (drawn.Owner != Card.Owner)
			return false;
		return drawn.Owner?.Creature?.CombatState != null;
	}
}

/// <summary>地狱狂徒：本场战斗中第一次打出时获得 <see cref="HellraiserPower"/>（与 <see cref="Hellraiser"/> 卡牌一致）。</summary>
public sealed class HellraiserEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal HellraiserStacks = 1m;

	private bool _pendingFirstPlayInCombat = true;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
		new DynamicVar[] { new PowerVar<HellraiserPower>(HellraiserStacks) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromPower<HellraiserPower>() };

	public override Task BeforeCombatStart()
	{
		_pendingFirstPlayInCombat = true;
		return Task.CompletedTask;
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (!_pendingFirstPlayInCombat)
			return;
		if (Card?.Owner?.Creature is not { } creature)
			return;

		_pendingFirstPlayInCombat = false;
		await PowerCmd.Apply<HellraiserPower>(creature, HellraiserStacks, creature, Card);
	}
}

/// <summary>腐蚀波：打出后本回合内，每当你抽到另一张牌，对所有敌人施加 2 层中毒。</summary>
public sealed class CorrosiveWaveEnchantment : DrawWaveEnchantmentBase
{
	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
		new DynamicVar[] { new PowerVar<PoisonPower>(2m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromPower<PoisonPower>() };

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (!ShouldHandleDraw(card))
			return;

		var applier = Card!.Owner!.Creature;
		foreach (var e in Card.CombatState!.HittableEnemies)
			await PowerCmd.Apply<PoisonPower>(e, 2m, applier, Card);
	}
}

/// <summary>灾厄波：打出后本回合内，每当你抽到另一张牌，对所有敌人施加 3 层灾厄。</summary>
public sealed class CalamityWaveDoomEnchantment : DrawWaveEnchantmentBase
{
	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
		new DynamicVar[] { new PowerVar<DoomPower>(3m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromPower<DoomPower>() };

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (!ShouldHandleDraw(card))
			return;

		var applier = Card!.Owner!.Creature;
		foreach (var e in Card.CombatState!.HittableEnemies)
			await PowerCmd.Apply<DoomPower>(e, 3m, applier, Card);
	}
}

/// <summary>铸剑波：打出后本回合内，每当你抽到另一张牌，铸造 4。</summary>
public sealed class ForgeWaveEnchantment : DrawWaveEnchantmentBase
{
	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars =>
		new DynamicVar[] { new ForgeVar(4) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromForge();

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (!ShouldHandleDraw(card))
			return;

		var player = Card!.Owner!;
		if (player.Creature?.CombatState == null)
			return;

		await ForgeCmd.Forge(4m, player, Card);
	}
}
