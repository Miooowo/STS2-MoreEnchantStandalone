using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
using MegaCrit.Sts2.Core.Runs;
using MoreEnchant.Compat;
using MoreEnchant.Standalone;
using MoreEnchant.Standalone.Compat;

namespace MoreEnchant.Enchantments;

/// <summary>合成：为宿主牌添加酒狐关键词「合成」。</summary>
public sealed class SynthesisEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	public override bool HasExtraCardText => false;

	protected override IEnumerable<IHoverTip> ExtraHoverTips
	{
		get
		{
			if (WineFoxCompat.TryGetCraftKeyword(out var craftKeyword))
				return new IHoverTip[] { HoverTipFactory.FromKeyword(craftKeyword) };
			return [];
		}
	}

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && WineFoxCompat.IsWineFoxModAvailable();

	protected override void OnEnchant()
	{
		EnsureCraftKeywordOnCard();
	}

	public override void RecalculateValues()
	{
		EnsureCraftKeywordOnCard();
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card == null || !WineFoxCompat.TryGetCraftIntoHandMethod(out var craftIntoHandMethod))
			return;

		var parameters = craftIntoHandMethod.GetParameters();
		var args = new object?[parameters.Length];
		for (var i = 0; i < parameters.Length; i++)
		{
			var parameter = parameters[i];
			args[i] = parameter.ParameterType switch
			{
				var t when t == typeof(PlayerChoiceContext) => choiceContext,
				var t when t == typeof(CardModel) => Card,
				_ => parameter.HasDefaultValue ? parameter.DefaultValue : null
			};
		}

		if (craftIntoHandMethod.Invoke(null, args) is Task task)
			await task;
	}

	private void EnsureCraftKeywordOnCard()
	{
		if (Card == null || !WineFoxCompat.IsWineFoxModAvailable())
			return;
		if (!WineFoxCompat.TryGetCraftKeyword(out var craftKeyword))
			return;
		if (Card.Keywords.Contains(craftKeyword))
			return;

		Card.AddKeyword(craftKeyword);
	}
}

/// <summary>物流：多人模式下，每场首次打出时将宿主牌的非附魔复制品加入随机其他玩家手牌。</summary>
public sealed class LogisticsEnchantment : WineFoxFirstPlayEnchantmentBase
{
	public override EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool CanEnchant(CardModel card) =>
		RunManager.Instance?.NetService?.Type.IsMultiplayer() == true
		&& WineFoxCompat.IsWineFoxModAvailable()
		&& (base.CanEnchant(card) || CardEnchantEligibility.IsCurseLikeCard(card));

	protected override async Task<bool> TryApplyFirstPlayEffect(PlayerChoiceContext choiceContext, Player owner, Creature self)
	{
		if (Card == null || self.CombatState == null)
			return false;

		var allies = self.CombatState.GetTeammatesOf(self)
			.Where(c => c is { IsAlive: true, IsPlayer: true } && c != self && c.Player != null)
			.ToList();
		if (allies.Count == 0)
			return false;

		var targetIndex = await ResolveSyncedRandomAllyIndex(owner, allies);
		if (targetIndex is null || targetIndex < 0 || targetIndex >= allies.Count)
			return false;

		var targetPlayer = allies[targetIndex.Value].Player;
		if (targetPlayer == null)
			return false;

		var clone = self.CombatState.CloneCard(Card);
		CardCmd.ClearEnchantment(clone);

		self.CombatState.RemoveCard(clone);
		self.CombatState.AddCard(clone, targetPlayer);
		await CardPileCmdCompat.AddGeneratedCardToCombat(clone, PileType.Hand, true);
		return true;
	}
}

/// <summary>手摇曲柄：每场首次打出时获得 2 层活力。</summary>
public sealed class HandCrankEnchantment : WineFoxFirstPlayEnchantmentBase
{
	private const decimal VigorAmount = 2m;

	public override EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
	[
		HoverTipFactory.FromPower<VigorPower>()
	];

	protected override async Task<bool> TryApplyFirstPlayEffect(PlayerChoiceContext choiceContext, Player owner, Creature self)
	{
		await PowerCmdCompat.Apply<VigorPower>(self, VigorAmount, self, Card, choiceContext);
		return true;
	}
}

public abstract class WineFoxFirstPlayEnchantmentBase : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private bool _pendingFirstPlayInCombat = true;

	public abstract EnchantmentRewardRarity RewardRarity { get; }

	public override bool HasExtraCardText => true;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && WineFoxCompat.IsWineFoxModAvailable();

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

	protected static async Task<int?> ResolveSyncedRandomAllyIndex(Player owner, IReadOnlyList<Creature> allies)
	{
		if (allies.Count == 0)
			return null;

		var isMultiplayer = RunManager.Instance?.NetService?.Type.IsMultiplayer() == true;
		if (!isMultiplayer)
			return null;

		var synchronizer = await WaitForPlayerChoiceSynchronizerAsync();
		if (synchronizer == null)
			return null;

		var choiceId = synchronizer.ReserveChoiceId(owner);
		int? allyIndex;
		if (LocalContext.IsMe(owner))
		{
			var chosen = owner.RunState?.Rng.CombatTargets.NextItem(allies) ?? allies[0];
			allyIndex = FindCreatureIndex(allies, chosen);
			synchronizer.SyncLocalChoice(owner, choiceId, PlayerChoiceResult.FromIndex(allyIndex));
		}
		else
		{
			var remoteChoice = await synchronizer.WaitForRemoteChoice(owner, choiceId);
			allyIndex = remoteChoice.AsIndexOrNull();

			// 联机计数器对齐：本地拥有者端已消耗一次 CombatTargets RNG，
			// 远端也需等量消耗以保持 RunState.Rng 计数一致，避免 checksum 分歧。
			_ = owner.RunState?.Rng.CombatTargets.NextItem(allies);
		}

		return allyIndex;
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
}
