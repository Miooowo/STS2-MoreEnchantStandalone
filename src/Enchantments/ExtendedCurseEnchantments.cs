using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>羞耻：费用 -1{Energy}；打出时获得 2 层 <see cref="FrailPower"/>。</summary>
public sealed class ShameCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { HoverTipFactory.FromPower<FrailPower>() };
	private const int FrailStacks = 2;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => true;

	/// <summary>供文案 <c>{Energy:energyIcons()}</c> 解析（减 1 费展示）。</summary>
	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new EnergyVar(1); }
	}

	public override void RecalculateValues()
	{
		if (Card == null || Card.EnergyCost.CostsX)
			return;

		int c = Card.EnergyCost.Canonical;
		if (c < 0)
			return;

		Card.EnergyCost.SetCustomBaseCost(System.Math.Max(0, c - 1));
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner?.Creature is not { } cre)
			return;

		bool had = cre.HasPower<FrailPower>();
		var pow = await PowerCmd.Apply<FrailPower>(cre, FrailStacks, null, Card);
		if (pow != null && !had)
			pow.SkipNextDurationTick = true;
	}
}

/// <summary>疑虑：抽到该牌时额外抽 1 张；打出时获得 1 层 <see cref="WeakPower"/>。</summary>
public sealed class DoubtCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips => new IHoverTip[] { HoverTipFactory.FromPower<WeakPower>() };
	private const int WeakStacks = 1;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => true;

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (!ReferenceEquals(card, Card) || Card?.Owner is not Player p || p.PlayerCombatState == null)
			return;

		await CardPileCmd.Draw(choiceContext, 1, p);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner?.Creature is not { } cre)
			return;

		bool had = cre.HasPower<WeakPower>();
		var pow = await PowerCmd.Apply<WeakPower>(cre, WeakStacks, null, Card);
		if (pow != null && !had)
			pow.SkipNextDurationTick = true;
	}
}

/// <summary>愚行：保留、永恒；战斗内费用从 0 起每打出一次 +1；打出后回到手牌（本战斗）。</summary>
public sealed class FollyCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private int _rampThisCombat;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => true;

	protected override void OnEnchant()
	{
		if (Card == null)
			return;

		CardCmd.ApplyKeyword(Card, CardKeyword.Retain);
		try
		{
			CardCmd.ApplyKeyword(Card, CardKeyword.Eternal);
		}
		catch
		{
			/* Eternal 不可用时跳过 */
		}
	}

	public override void RecalculateValues()
	{
		if (Card == null || Card.EnergyCost.CostsX)
			return;

		Card.EnergyCost.SetCustomBaseCost(System.Math.Max(0, _rampThisCombat));
	}

	public override Task AfterCardGeneratedForCombat(CardModel card, bool addedByPlayer)
	{
		if (!ReferenceEquals(card, Card))
			return Task.CompletedTask;

		_rampThisCombat = 0;
		RecalculateValues();
		return Task.CompletedTask;
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card == null || Card.EnergyCost.CostsX)
			return;

		_rampThisCombat++;
		RecalculateValues();
		Card.InvokeEnergyCostChanged();
		await Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		if (Card?.Pile?.Type == PileType.Deck)
		{
			_rampThisCombat = 0;
			RecalculateValues();
		}

		return Task.CompletedTask;
	}

	public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
		CardModel card,
		bool isAutoPlay,
		ResourceInfo resources,
		PileType pileType,
		CardPilePosition position)
	{
		if (!ReferenceEquals(card, Card) || pileType != PileType.Discard)
			return (pileType, position);
		return (PileType.Hand, position);
	}
}

/// <summary>债务：抽到该牌时获得 2 能量并失去 10 金币。</summary>
public sealed class DebtCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const int EnergyGain = 2;

	private const int GoldLoss = 10;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get
		{
			yield return new EnergyVar(EnergyGain);
			yield return new GoldVar(GoldLoss);
		}
	}

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (!ReferenceEquals(card, Card) || Card?.Owner is not Player p)
			return;

		await PlayerCmd.GainEnergy(EnergyGain, p);

		int lose = Mathf.Min(GoldLoss, p.Gold);
		if (lose > 0)
			await PlayerCmd.LoseGold(lose, p);
	}
}

/// <summary>贪婪：抽到该牌时获得 4 点能量；下一<strong>玩家回合开始</strong>时失去等额能量（须推迟结算：同一次回合开始里先抽牌后才会调 <see cref="AbstractModel.AfterPlayerTurnStart"/>，否则会当场扣掉刚加的能量）。</summary>
public sealed class GreedCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const int Burst = 4;

	/// <summary>待在下一次「晚于 <see cref="_debtIncurredRound"/> 的玩家回合开始」失去的能量总和。</summary>
	private int _pendingEnergyLoss;

	/// <summary>最近一次吃债时的 <see cref="CombatState.RoundNumber"/>（与本次回合开始相等时不结算，避免紧接在抽牌后的 AfterPlayerTurnStart 误扣）。</summary>
	private int _debtIncurredRound = -1;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new EnergyVar(Burst); }
	}

	protected override void OnEnchant()
	{
		_pendingEnergyLoss = 0;
		_debtIncurredRound = -1;
	}

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (!ReferenceEquals(card, Card) || Card?.Owner is not Player p)
			return;

		int round = p.Creature.CombatState?.RoundNumber ?? 0;
		await PlayerCmd.GainEnergy(Burst, p);
		_pendingEnergyLoss += Burst;
		_debtIncurredRound = round;
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (Card?.Owner != player || _pendingEnergyLoss <= 0)
			return;

		int round = player.Creature.CombatState?.RoundNumber ?? 0;
		if (round <= _debtIncurredRound)
			return;

		int pay = _pendingEnergyLoss;
		_pendingEnergyLoss = 0;
		_debtIncurredRound = -1;
		await PlayerCmd.LoseEnergy(pay, player);
	}
}

/// <summary>睡眠不佳：当前阶段（Act）内每打出 1 次，本阶段休息点休息回复生命减少 1（按打出次数累加，进新 Act 重置）。</summary>
/// <remarks>
/// 战斗中打出的是 <see cref="CardModel.DeckVersion"/> 指向的克隆牌；<see cref="ModifyRestSiteHealAmount"/> 只对牌组中的原版牌迭代，
/// 故打出次数必须累计在<strong>原版牌</strong>上附魔的本类实例上（见 <see cref="GetCounterHolder"/>）。
/// </remarks>
public sealed class PoorSleepCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private int _playsThisAct;

	private int _trackedActIndex = -1;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => true;

	public override bool ShowAmount => true;

	public override int DisplayAmount
	{
		get
		{
			var h = GetCounterHolder();
			return h != null ? h._playsThisAct : _playsThisAct;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new DynamicVar("PoorSleepPlays", 0m); }
	}

	protected override void OnEnchant()
	{
		_playsThisAct = 0;
		_trackedActIndex = -1;
		SyncPoorSleepDisplayVar();
	}

	/// <summary>牌组里、会收到休息钩子的那张牌上的睡眠不佳附魔（有 <see cref="CardModel.DeckVersion"/> 时从克隆打出写回此处）。</summary>
	private PoorSleepCurseEnchantment? GetCounterHolder()
	{
		if (Card == null)
			return null;

		var canonicalCard = Card.DeckVersion ?? Card;
		return canonicalCard.Enchantment as PoorSleepCurseEnchantment;
	}

	private void SyncAct(Player? player)
	{
		if (player?.RunState == null)
			return;

		int idx = player.RunState.CurrentActIndex;
		if (idx != _trackedActIndex)
		{
			_trackedActIndex = idx;
			_playsThisAct = 0;
		}

		SyncPoorSleepDisplayVar();
	}

	private void SyncPoorSleepDisplayVar()
	{
		if (!DynamicVars.TryGetValue("PoorSleepPlays", out var v))
			return;
		v.BaseValue = _playsThisAct;
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (GetCounterHolder() is not { } holder || holder.Card?.Owner is not Player p)
			return;

		holder.SyncAct(p);
		holder._playsThisAct++;
		holder.SyncPoorSleepDisplayVar();
		// 战斗中卡面附在克隆上：把计数同步到当前实例的 DynamicVars，避免描述仍显示 0。
		if (!ReferenceEquals(holder, this) && DynamicVars.TryGetValue("PoorSleepPlays", out var cloneV))
			cloneV.BaseValue = holder._playsThisAct;

		await Task.CompletedTask;
	}

	public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
	{
		if (Card?.Owner is not Player owner || creature.Player != owner)
			return amount;

		SyncAct(owner);
		return System.Math.Max(0m, amount - _playsThisAct);
	}

	/// <summary>与 <see cref="MegaCrit.Sts2.Core.Models.Relics.RegalPillow.ModifyExtraRestSiteHealText"/> 相同，在休息「回复生命」选项说明中追加一行（<see cref="MegaCrit.Sts2.Core.Entities.RestSite.HealRestSiteOption.Description"/>）。</summary>
	public override IReadOnlyList<LocString> ModifyExtraRestSiteHealText(Player player, IReadOnlyList<LocString> currentExtraText)
	{
		if (Card?.Owner != player || !LocalContext.IsMe(player))
			return currentExtraText;

		SyncAct(player);

		LocString? extra = LocString.GetIfExists("enchantments", "POOR_SLEEP_CURSE_ENCHANTMENT.additionalRestSiteHealText");
		if (extra == null)
			return currentExtraText;

		DynamicVars.AddTo(extra);

		var merged = new List<LocString>(currentExtraText.Count + 1);
		foreach (LocString item in currentExtraText)
			merged.Add(item);
		merged.Add(extra);
		return merged;
	}
}

/// <summary>腐朽：攻击伤害×2（仅 Powered）；打出时失去 4 生命；回合结束时若在手牌受到 2 点伤害。</summary>
public sealed class DecayCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal HpLossOnPlay = 4m;

	private const decimal TurnEndDamage = 2m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new DamageVar(TurnEndDamage, ValueProp.Unpowered | ValueProp.Move); }
	}

	public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props) =>
		ValuePropUtil.IsPoweredAttack(props) ? 2m : 1m;

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner?.Creature is not { } cre)
			return;

		await CreatureCmd.Damage(choiceContext, cre, HpLossOnPlay,
			ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, null, Card);
	}

	public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side != CombatSide.Player)
			return;
		if (Card?.Owner is not Player p)
			return;

		var hand = p.PlayerCombatState?.Hand.Cards;
		if (hand == null || !hand.Contains(Card))
			return;

		await CreatureCmd.Damage(choiceContext, p.Creature, DynamicVars.Damage.BaseValue,
			ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, null, Card);
	}
}

/// <summary>悔恨：回合结束时若在手牌，失去等同于当前手牌数量的生命。</summary>
public sealed class RegretCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => true;

	public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
	{
		if (side != CombatSide.Player)
			return;
		if (Card?.Owner is not Player p)
			return;

		var hand = p.PlayerCombatState?.Hand.Cards;
		if (hand == null || !hand.Contains(Card))
			return;

		int n = hand.Count;
		if (n <= 0)
			return;

		await CreatureCmd.Damage(choiceContext, p.Creature, n,
			ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, null, Card);
	}
}

/// <summary>苦恼：固有；此牌在手牌中时，你每打出一张其他牌失去 1 点能量。</summary>
public sealed class AnguishCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => true;

	protected override void OnEnchant()
	{
		if (Card != null)
			CardCmd.ApplyKeyword(Card, CardKeyword.Innate);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Card == null || ReferenceEquals(cardPlay.Card, Card))
			return;
		if (Card.Owner is not Player p)
			return;

		var hand = p.PlayerCombatState?.Hand.Cards;
		if (hand == null || !hand.Contains(Card))
			return;

		await PlayerCmd.LoseEnergy(1, p);
	}
}

/// <summary>愧疚：附魔时按 <see cref="MegaCrit.Sts2.Core.Models.Enchantments.Mocks.MockFreeEnchantment"/> 将能量基准归零、辉星基准尽量归零；战斗中再按 <see cref="MegaCrit.Sts2.Core.Models.Potions.TouchOfInsanity"/> 使用 <see cref="CardModel.SetToFreeThisCombat"/>（<see cref="BeforeCombatStart"/> / <see cref="AfterCardGeneratedForCombat"/>）。移除倒计时与 <see cref="MegaCrit.Sts2.Core.Models.Cards.Guilty"/> 相同，使用 <c>Combats</c> 动态变量。</summary>
public sealed class GuiltCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const int CombatsToRemove = 5;

	private static readonly MethodInfo? CardUpgradeStarCostBy = typeof(CardModel).GetMethod(
		"UpgradeStarCostBy",
		BindingFlags.Instance | BindingFlags.NonPublic,
		null,
		[typeof(int)],
		null);

	private int _combatsEndedInDeck;

	/// <summary>已在牌组中结束的战斗次数（与原版愧疚 <see cref="MegaCrit.Sts2.Core.Models.Cards.Guilty.CombatsSeen"/> 语义一致）。</summary>
	[SavedProperty]
	public int CombatsEndedInDeck
	{
		get => _combatsEndedInDeck;
		set
		{
			_combatsEndedInDeck = value;
			SyncCombatsDisplayVar();
		}
	}

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => true;

	/// <summary>与 <see cref="MegaCrit.Sts2.Core.Models.Cards.Guilty"/> 相同键名 <c>Combats</c>，供 <c>{Combats:diff()}</c> 等文案使用。</summary>
	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get
		{
			yield return new DynamicVar("Combats", CombatsToRemove);
			yield return new EnergyVar(0);
		}
	}

	protected override void OnEnchant()
	{
		if (Card == null)
			return;

		// MockFreeEnchantment：非 X 费时把当前能量基准降到 0。
		if (!Card.EnergyCost.CostsX)
			Card.EnergyCost.UpgradeBy(-Card.EnergyCost.GetWithModifiers(CostModifiers.None));

		ZeroBaseStarCostLikeUpgrade(Card);
		SyncCombatsDisplayVar();
	}

	private void SyncCombatsDisplayVar()
	{
		if (!DynamicVars.TryGetValue("Combats", out var v))
			return;
		v.BaseValue = System.Math.Max(0, CombatsToRemove - CombatsEndedInDeck);
	}

	/// <summary>与 <c>CardModel.UpgradeStarCostBy(-n)</c> 等效；API 非 public，故用反射（失败则仅依赖战斗内 SetStarCostThisCombat）。</summary>
	private static void ZeroBaseStarCostLikeUpgrade(CardModel card)
	{
		if (card.HasStarCostX)
			return;

		int stars = card.BaseStarCost;
		if (stars <= 0)
			return;

		try
		{
			CardUpgradeStarCostBy?.Invoke(card, [-stars]);
		}
		catch
		{
			// ignore
		}
	}

	public override Task BeforeCombatStart()
	{
		if (Card?.Pile is not { IsCombatPile: true })
			return Task.CompletedTask;

		Card.SetToFreeThisCombat();
		Card.InvokeEnergyCostChanged();
		return Task.CompletedTask;
	}

	public override Task AfterCardGeneratedForCombat(CardModel card, bool addedByPlayer)
	{
		if (!ReferenceEquals(card, Card))
			return Task.CompletedTask;

		card.SetToFreeThisCombat();
		card.InvokeEnergyCostChanged();
		return Task.CompletedTask;
	}

	public override async Task AfterCombatEnd(CombatRoom room)
	{
		if (Card?.Pile?.Type != PileType.Deck)
			return;

		CombatsEndedInDeck++;
		if (CombatsEndedInDeck >= CombatsToRemove)
			await CardPileCmd.RemoveFromDeck(Card);
	}
}

/// <summary>受伤：耗能 +1；打出后使随机另一张手牌耗能 +1。</summary>
public sealed class InjuryCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const int CostIncreaseSelf = 1;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => true;

	public override void RecalculateValues()
	{
		if (Card == null || Card.EnergyCost.CostsX)
			return;

		int c = Card.EnergyCost.Canonical;
		if (c < 0)
			return;

		Card.EnergyCost.SetCustomBaseCost(c + CostIncreaseSelf);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (!ReferenceEquals(cardPlay.Card, Card) || Card?.Owner is not Player owner)
			return;

		var hand = owner.PlayerCombatState?.Hand.Cards;
		if (hand == null)
			return;

		var pool = hand.Where(c => !ReferenceEquals(c, Card) && !c.EnergyCost.CostsX).ToList();
		var victim = owner.RunState.Rng.CombatCardSelection.NextItem(pool);
		if (victim == null)
			return;

		int next = victim.EnergyCost.GetWithModifiers(CostModifiers.Local) + 1;
		victim.EnergyCost.SetCustomBaseCost(next);
		victim.InvokeEnergyCostChanged();
		await Task.CompletedTask;
	}
}

/// <summary>进阶之灾：虚无 + 永恒。</summary>
public sealed class CalamityCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => false;

	protected override void OnEnchant()
	{
		if (Card == null)
			return;

		CardCmd.ApplyKeyword(Card, CardKeyword.Ethereal);
		try
		{
			CardCmd.ApplyKeyword(Card, CardKeyword.Eternal);
		}
		catch
		{
			/* Eternal 不可用时跳过 */
		}
	}
}
