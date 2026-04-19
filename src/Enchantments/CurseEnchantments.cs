using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>笨拙：首回合无法打出；在手时每当你打出另一张牌，随机弃掉一张其他手牌。</summary>
public sealed class ClumsyCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => true;

	public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
	{
		if (!ReferenceEquals(card, Card))
			return true;
		if (Card?.CombatState == null)
			return true;
		return Card.CombatState.RoundNumber > 1;
	}

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		CurseFirstCombatRoundEnergyCost.AfterPlayerTurnStart(this, player);
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (Card == null || cardPlay.Card == Card)
			return;

		var owner = Card.Owner;
		if (owner?.PlayerCombatState == null)
			return;

		var hand = owner.PlayerCombatState.Hand.Cards;
		if (!hand.Contains(Card))
			return;

		var pool = hand.Where(c => !ReferenceEquals(c, Card)).ToList();
		if (pool.Count == 0)
			return;

		var pick = owner.RunState.Rng.CombatCardSelection.NextItem(pool);
		if (pick != null)
			await CardCmd.Discard(context, pick);
	}
}

/// <summary>执迷：耗能+1；抽到该牌时获得能量；手牌中有执迷时须先打出执迷牌（见 <see cref="MoreEnchant.Patches.ObsessionCurseIsPlayablePatch"/>）。</summary>
public sealed class ObsessionCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const int CostIncrease = 1;

	private const int DrawEnergyAmount = 2;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => true;

	public override bool ShouldGlowRed => true;

	/// <summary>两个 <see cref="EnergyVar"/>：默认名用于费用 +1 文案；<c>ObsessionDrawEnergy</c> 用于抽牌得能（须为 <see cref="EnergyVar"/>，<c>energyIcons()</c> 格式化器不接受普通 <see cref="DynamicVar"/>）。</summary>
	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get
		{
			yield return new EnergyVar(CostIncrease);
			yield return new EnergyVar("ObsessionDrawEnergy", DrawEnergyAmount);
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

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		CurseFirstCombatRoundEnergyCost.AfterPlayerTurnStart(this, player);
		return Task.CompletedTask;
	}

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (!ReferenceEquals(card, Card))
			return;

		if (Card?.Owner is not Player player)
			return;

		int e = (int)DynamicVars["ObsessionDrawEnergy"].BaseValue;
		if (e <= 0)
			return;

		await PlayerCmd.GainEnergy(e, player);
	}
}

/// <summary>孢子：消耗。</summary>
public sealed class SporeCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => false;

	protected override void OnEnchant()
	{
		if (Card != null)
			CardCmd.ApplyKeyword(Card, CardKeyword.Exhaust);
	}
}

/// <summary>凡庸：首回合不可打出；在手时本回合最多打出 3 张牌（由补丁限制可打出性）。</summary>
public sealed class MediocreCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => true;

	public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
	{
		if (!ReferenceEquals(card, Card))
			return true;
		if (Card?.CombatState == null)
			return true;
		return Card.CombatState.RoundNumber > 1;
	}

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		CurseFirstCombatRoundEnergyCost.AfterPlayerTurnStart(this, player);
		MediocreCursePlayLimiter.OnPlayerTurnStart(player);
		return Task.CompletedTask;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		MediocreCursePlayLimiter.RecordPlay(Card?.Owner, cardPlay);
		return Task.CompletedTask;
	}
}

/// <summary>铃铛的诅咒：奖励入手时各获得 1 件普通、罕见、稀有遗物；战斗中不可打出；永恒。</summary>
public sealed class BellCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private int _rewardRelicsGrantedGate;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => false;

	/// <summary>
	/// 玩家真正获得带铃铛诅咒的牌时由 <see cref="MoreEnchant.Patches.RewardSynchronizerBellCurseRelicGrantPatch"/> 调用；
	/// 仅第一次成功；避免用 <see cref="CardModel"/> 引用做待发放集合（奖励展示实例与入手实例可能不是同一引用）。
	/// </summary>
	internal bool TryTakeRewardRelicGrantOnce() =>
		Interlocked.CompareExchange(ref _rewardRelicsGrantedGate, 1, 0) == 0;

	/// <summary>
	/// 牌被 <see cref="MegaCrit.Sts2.Core.Runs.RunState.CloneCard"/> / <see cref="MegaCrit.Sts2.Core.Combat.CombatState.CloneCard"/> 等复制时，
	/// 附魔状态会被 <c>ClonePreservingMutability</c> 一并复制，导致遗物门闩仍为已发放；每张实体牌应各领一次铃铛遗物（如冰梆等复制进牌组）。
	/// </summary>
	internal void ResetRewardRelicGrantGateForClonedCard() =>
		Interlocked.Exchange(ref _rewardRelicsGrantedGate, 0);

	protected override void OnEnchant()
	{
		if (Card == null)
			return;

        try
        {
            CardCmd.ApplyKeyword(Card, CardKeyword.Eternal);
            CardCmd.ApplyKeyword(Card, CardKeyword.Unplayable);
		}
        catch
        {
            // Eternal 不存在则忽略
        }
	}
}

/// <summary>感染：卡牌使用与状态牌 <see cref="MegaCrit.Sts2.Core.Models.Cards.Infection"/> 相同的动态 overlay（由 <see cref="MoreEnchant.Patches.NCardInfectionCurseOverlayPatch"/> 挂载）。</summary>
public sealed class InfectionCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => false;
}

/// <summary>霉运：首回合不可打出；回合结束时若仍在手牌则受到伤害。</summary>
public sealed class BadLuckCurseEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal SelfDamage = 13m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Curse;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get { yield return new DamageVar(SelfDamage, ValueProp.Move); }
	}

	public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
	{
		if (!ReferenceEquals(card, Card))
			return true;
		if (Card?.CombatState == null)
			return true;
		return Card.CombatState.RoundNumber > 1;
	}

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		CurseFirstCombatRoundEnergyCost.AfterPlayerTurnStart(this, player);
		return Task.CompletedTask;
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

		await CreatureCmd.Damage(choiceContext, p.Creature, DynamicVars.Damage.BaseValue, ValueProp.Move, null, Card);
	}
}

/// <summary>
/// 铃铛诅咒遗物发放逻辑；在玩家拾起带铃铛诅咒的牌时由
/// <c>RewardSynchronizer.SyncLocalObtainedCard</c> 补丁调用 <see cref="BellCurseEnchantment.TryTakeRewardRelicGrantOnce"/> 后执行。
/// </summary>
internal static class BellCurseReward
{
	/// <summary>铃铛诅咒遗物中排除 <see cref="Whetstone"/>（磨刀石）。</summary>
	private static readonly RelicModel[] RelicPullBlacklist = [ModelDb.Relic<Whetstone>()];

	/// <summary>
	/// 本体 <c>PullNextRelicFromBack</c> 第三参为 true 时遗物<strong>可</strong>被抽出；磨刀石用 <see cref="ModelId"/> 排除（与 deque 实例引用无关）。
	/// </summary>
	private static bool IsBellRelicPullAllowed(RelicModel r) =>
		!RelicPullBlacklist.Any(b => b.Id == r.Id);

	/// <summary>
	/// 固定各发放 1 件普通、罕见、稀有遗物（经 <see cref="RelicFactory.PullNextRelicFromBack"/> 从对应稀有度池尾抽），并排除磨刀石。
	/// 若某档池在排除后无可抽物，工厂会回落至 <see cref="MegaCrit.Sts2.Core.Models.Relics.Circlet"/>（游戏内置行为）。
	/// </summary>
	internal static async Task GrantCore(Player player)
	{
		RelicRarity[] tiers =
		[
			RelicRarity.Common,
			RelicRarity.Uncommon,
			RelicRarity.Rare,
		];

		foreach (var rarity in tiers)
		{
			var relic = RelicFactory.PullNextRelicFromBack(player, rarity, IsBellRelicPullAllowed).ToMutable();
			await RelicCmd.Obtain(relic, player);
		}
	}

	/// <summary>
	/// 奖励同步路径上立即 <see cref="GrantCore"/> 会与选牌 UI 冲突（卡死、无飞入动画、遗物未发）；延后一帧再发（GitHub #14）。
	/// </summary>
	internal static async Task GrantCoreAfterUiFrame(Player player)
	{
		if (Engine.GetMainLoop() is SceneTree tree)
			await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
		await GrantCore(player);
	}
}

/// <summary>
/// 与原版诅咒牌一致用基础耗能 <c>-1</c> 表示不可打出样式，并以各附魔 <see cref="EnchantmentModel.ShouldPlay"/> 拦首回合；
/// X 费牌仅依赖 <see cref="EnchantmentModel.ShouldPlay"/>。次玩家回合起 <see cref="CardEnergyCost.ResetForDowngrade"/> 并触发附魔 <see cref="EnchantmentModel.RecalculateValues"/>。
/// <see cref="ObsessionCurseEnchantment"/> 不使用首回合 <c>-1</c> 样式，故排除。
/// </summary>
internal static class CurseFirstCombatRoundEnergyCost
{
	internal static void AfterPlayerTurnStart(EnchantmentModel enchant, Player player)
	{
		var card = enchant.Card;
		if (card == null || card.Owner != player || card.CombatState == null)
			return;

		if (card.EnergyCost.CostsX)
			return;

		if (card.CombatState.RoundNumber > 1)
			card.EnergyCost.ResetForDowngrade();

		enchant.RecalculateValues();

		if (card.CombatState.RoundNumber <= 1 && enchant is not ObsessionCurseEnchantment)
			card.EnergyCost.SetCustomBaseCost(-1);

		card.DynamicVars.RecalculateForUpgradeOrEnchant();
		card.InvokeEnergyCostChanged();
	}
}
