using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
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
using MoreEnchant.Enchantments;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments.Beta;

/// <summary>恶魔护盾：失去 1 生命；将当前格挡给予自选的一名队友。消耗。仅多人。</summary>
public sealed class DemonShieldShareBlockEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity,
	IBetaGatedRewardEnchantment
{
	private const decimal HpLoss = 1m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	public override bool HasExtraCardText => true;

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromKeyword(CardKeyword.Exhaust) };

	public override bool CanEnchant(CardModel card)
	{
		if (!base.CanEnchant(card))
			return false;
		return RunManager.Instance?.NetService.Type.IsMultiplayer() == true;
	}

	protected override void OnEnchant()
	{
		if (Card != null)
			CardCmd.ApplyKeyword(Card, CardKeyword.Exhaust);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		var card = Card;
		var player = card?.Owner;
		var self = player?.Creature;
		var cs = self?.CombatState;
		if (card == null || player == null || self == null || cs == null)
			return;

		await CreatureCmd.TriggerAnim(self, "Cast", player.Character.CastAnimDelay);
		await CreatureCmd.Damage(
			choiceContext,
			self,
			HpLoss,
			ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
			card);

		var block = (decimal)self.Block;
		if (block <= 0m)
			return;

		var allies = cs.GetTeammatesOf(self)
			.Where(c => c is { IsAlive: true, IsPlayer: true } && c != self)
			.ToList();
		if (allies.Count == 0)
			return;

		var tm = NTargetManager.Instance;
		var room = NCombatRoom.Instance;
		if (tm == null || room == null)
			return;

		var selfNode = room.GetCreatureNode(self);
		var startPos = selfNode != null
			? selfNode.GlobalPosition + Vector2.Down * 60f
			: Vector2.Zero;

		// 多人下各端 Overlay/手柄状态可能不一致，会导致 SelectionFinished 与格挡转移结果分叉（checksum 不一致）。
		var useController = NControllerManager.Instance?.IsUsingController == true;
		if (RunManager.Instance?.NetService.Type.IsMultiplayer() == true)
			useController = false;
		var mode = useController ? TargetMode.Controller : TargetMode.ClickMouseToTarget;

		List<Control>? whitelist = null;
		if (useController && CombatManager.Instance.IsInProgress)
		{
			whitelist = allies
				.Select(room.GetCreatureNode)
				.Where(n => n != null)
				.Select(n => n!.Hitbox)
				.ToList();
			if (whitelist.Count > 0)
			{
				room.RestrictControllerNavigation(whitelist);
				whitelist[0].TryGrabFocus();
			}
		}

		tm.StartTargeting(TargetType.AnyAlly, startPos, mode, ShouldCancelDemonShieldTargeting, null);

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
		if (mate == null || !allies.Contains(mate))
			return;

		await CreatureCmd.GainBlock(mate, block, ValueProp.Move, cardPlay);
	}

	private static bool ShouldCancelDemonShieldTargeting()
	{
		if (!CombatManager.Instance.IsInProgress)
			return false;
		// 仅本地 UI 状态：联机时各端计数可能不同，误取消会导致与主机目标选择不一致。
		if (RunManager.Instance?.NetService.Type.IsMultiplayer() == true)
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
