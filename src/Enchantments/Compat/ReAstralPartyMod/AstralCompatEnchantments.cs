using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
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
using MoreEnchant.Compat;
using MoreEnchant.Powers;
using MoreEnchant.Standalone;
using MoreEnchant.Standalone.Compat;

namespace MoreEnchant.Enchantments;

/// <summary>以毒攻毒：失去生命并在接下来回合开始时回复生命。仅星引擎联动可用。</summary>
public sealed class FightFireWithFireEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal HpLoss = 2m;
	private const decimal HealTurns = 2m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
	];

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && ReAstralPartyCompat.IsAstralPartyModAvailable();

	protected override void OnEnchant()
	{
		if (Card != null)
			CardCmd.ApplyKeyword(Card, CardKeyword.Exhaust);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner is not { Creature: { } creature } owner)
			return;

		await CreatureCmd.TriggerAnim(creature, "Cast", owner.Character.CastAnimDelay);
		await CreatureCmd.Damage(
			choiceContext,
			creature,
			HpLoss,
			ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
			Card);
		await PowerCmdCompat.Apply<FightFireWithFireHealPower>(creature, HealTurns, creature, Card, choiceContext);
	}
}

/// <summary>灵魂链接：选择一个敌人，当你失去生命时，该敌人失去等量生命。仅星引擎联动可用。</summary>
public sealed class SoulLinkEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
	];

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && ReAstralPartyCompat.IsAstralPartyModAvailable();

	protected override void OnEnchant()
	{
		if (Card != null)
			CardCmd.ApplyKeyword(Card, CardKeyword.Exhaust);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner is not { Creature: { } self } owner || self.CombatState == null)
			return;

		var target = ResolveLinkedEnemy(cardPlay, self.CombatState);
		if (target == null)
			return;

		await CreatureCmd.TriggerAnim(self, "Cast", owner.Character.CastAnimDelay);
		var link = await PowerCmdCompat.Apply<SoulLinkMirrorDamagePower>(self, 1m, self, Card, choiceContext);
		link?.SetLinkedTarget(target);
	}

	private Creature? ResolveLinkedEnemy(CardPlay? cardPlay, ICombatState state)
	{
		if (cardPlay?.Target is { IsAlive: true, IsEnemy: true } direct)
			return direct;

		var hittable = state.HittableEnemies;
		if (hittable.Count == 0 || Card?.Owner?.RunState == null)
			return null;
		return Card.Owner.RunState.Rng.CombatTargets.NextItem(hittable);
	}
}

/// <summary>错误的目标：费用 +1，视作能力牌；获得负面效果时转移给随机敌人。仅星引擎联动可用。</summary>
public sealed class RandomSelectEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && ReAstralPartyCompat.IsAstralPartyModAvailable();

	public override void RecalculateValues()
	{
		if (Card == null || Card.EnergyCost.CostsX)
			return;

		var canonical = Card.EnergyCost.Canonical;
		if (canonical < 0)
			return;

		Card.EnergyCost.SetCustomBaseCost(canonical + 1);
	}

	public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
		CardModel card,
		bool isAutoPlay,
		ResourceInfo resources,
		PileType pileType,
		CardPilePosition position)
	{
		if (!ReferenceEquals(card, Card))
			return (pileType, position);
		return (PileType.None, position);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner?.Creature is not { } creature)
			return;

		await PowerCmdCompat.Apply<RandomSelectWrongTargetPower>(creature, 1m, creature, Card, choiceContext);
	}
}

/// <summary>支援口香糖：多人模式中，本场首次打出时选择一名其他玩家，令其恢复生命并抽牌。</summary>
public sealed class SupportGumEnchantment : AstralFirstPlayAllyEnchantmentBase
{
	public override EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	protected override async Task ApplyToAlly(
		PlayerChoiceContext choiceContext,
		Player owner,
		Creature self,
		Creature ally)
	{
		await CreatureCmd.Heal(ally, 2m, true);
		if (ally.Player != null)
			await CardPileCmd.Draw(choiceContext, 1m, ally.Player);
	}
}

/// <summary>能量补充棒：多人模式中，本场首次打出时选择一名其他玩家，令其获得活力。</summary>
public sealed class EnergySupplementBarEnchantment : AstralFirstPlayAllyEnchantmentBase
{
	public override EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>()
	];

	protected override async Task ApplyToAlly(
		PlayerChoiceContext choiceContext,
		Player owner,
		Creature self,
		Creature ally)
	{
		await PowerCmdCompat.Apply<VigorPower>(ally, 8m, self, Card, choiceContext);
	}
}

public abstract class AstralFirstPlayAllyEnchantmentBase : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private bool _pendingFirstPlayInCombat = true;

	public abstract EnchantmentRewardRarity RewardRarity { get; }

	public override bool HasExtraCardText => true;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card)
		&& ReAstralPartyCompat.IsAstralPartyModAvailable()
		&& RunManager.Instance?.NetService?.Type.IsMultiplayer() == true;

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

		var allies = self.CombatState.GetTeammatesOf(self)
			.Where(c => c is { IsAlive: true, IsPlayer: true } && c != self)
			.ToList();
		if (allies.Count == 0)
			return;

		var isMultiplayer = RunManager.Instance?.NetService?.Type.IsMultiplayer() == true;
		if (!isMultiplayer)
			return;

		var isChoiceOwner = LocalContext.IsMe(owner);
		var synchronizer = await WaitForPlayerChoiceSynchronizerAsync();
		if (synchronizer == null)
			return;

		int? allyIndex;
		var choiceId = synchronizer.ReserveChoiceId(owner);
		if (isChoiceOwner)
		{
			allyIndex = await SelectAllyIndexAsync(self, allies);
			synchronizer.SyncLocalChoice(owner, choiceId, PlayerChoiceResult.FromIndex(allyIndex ?? -1));
		}
		else
		{
			var remoteChoice = await synchronizer.WaitForRemoteChoice(owner, choiceId);
			allyIndex = remoteChoice.AsIndexOrNull();
		}

		if (allyIndex is null || allyIndex < 0 || allyIndex >= allies.Count)
			return;

		_pendingFirstPlayInCombat = false;
		await ApplyToAlly(choiceContext, owner, self, allies[allyIndex.Value]);
	}

	protected abstract Task ApplyToAlly(PlayerChoiceContext choiceContext, Player owner, Creature self, Creature ally);

	private static async Task<int?> SelectAllyIndexAsync(Creature self, IReadOnlyList<Creature> allies)
	{
		var tm = NTargetManager.Instance;
		var room = NCombatRoom.Instance;
		if (tm == null || room == null)
			return null;

		var selfNode = room.GetCreatureNode(self);
		var startPos = selfNode != null
			? selfNode.GlobalPosition + Vector2.Down * 60f
			: Vector2.Zero;

		tm.StartTargeting(TargetType.AnyAlly, startPos, TargetMode.ClickMouseToTarget, ShouldCancelTargeting, null);

		Node? picked;
		try
		{
			picked = await tm.SelectionFinished();
		}
		finally
		{
			room.EnableControllerNavigation();
			NRun.Instance?.GlobalUi.MultiplayerPlayerContainer.UnlockNavigation();
		}

		var mate = CreatureFromTargetNode(picked);
		return mate == null ? null : FindCreatureIndex(allies, mate);
	}

	private static async Task<PlayerChoiceSynchronizer?> WaitForPlayerChoiceSynchronizerAsync()
	{
		var runManager = RunManager.Instance;
		if (runManager == null)
			return null;

		for (var i = 0; i < 60; i++)
		{
			if (runManager.PlayerChoiceSynchronizer != null)
				return runManager.PlayerChoiceSynchronizer;
			await Task.Yield();
		}

		return runManager.PlayerChoiceSynchronizer;
	}

	private static int FindCreatureIndex(IReadOnlyList<Creature> allies, Creature target)
	{
		for (var i = 0; i < allies.Count; i++)
		{
			if (ReferenceEquals(allies[i], target))
				return i;
		}

		return -1;
	}

	private static bool ShouldCancelTargeting()
	{
		if (!CombatManager.Instance.IsInProgress)
			return false;
		if (RunManager.Instance?.NetService?.Type.IsMultiplayer() == true)
			return false;
		if (NOverlayStack.Instance != null && NOverlayStack.Instance.ScreenCount > 0)
			return true;
		return NCapstoneContainer.Instance?.InUse == true;
	}

	private static Creature? CreatureFromTargetNode(Node? node)
	{
		if (node == null)
			return null;
		if (node is NCreature nc)
			return nc.Entity;
		if (node is NMultiplayerPlayerState nps)
			return nps.Player.Creature;
		return null;
	}
}
