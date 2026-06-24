using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MoreEnchant.Compat;

namespace MoreEnchant.Powers;

/// <summary>以毒攻毒：在拥有者回合开始时回复固定生命，并按回合数衰减。</summary>
public sealed class FightFireWithFireHealPower : PowerModel
{
	private const decimal HealPerTurn = 3m;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override bool ShouldReceiveCombatHooks => true;

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (Owner?.Player != player || Amount <= 0m)
			return;

		await CreatureCmd.Heal(Owner, HealPerTurn, playAnim: true);
		if (Amount <= 1m)
			await PowerCmd.Remove(this);
		else
			await PowerCmdCompat.Apply<FightFireWithFireHealPower>(Owner, -1m, Owner, null, choiceContext, true);
	}
}

/// <summary>灵魂链接：拥有者失去生命时，绑定目标失去等量生命。</summary>
public sealed class MoreEnchantSoulLinkPower : PowerModel
{
	private static bool _transferringDamage;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public override bool ShouldPlayVfx => false;

	public override bool ShouldReceiveCombatHooks => true;

	public void SetLinkedTarget(Creature target)
	{
		Target = target;
	}

	public override async Task AfterDamageGiven(
		PlayerChoiceContext choiceContext,
		Creature? dealer,
		DamageResult result,
		ValueProp props,
		Creature target,
		CardModel? cardSource)
	{
		if (target != Owner || _transferringDamage || result.UnblockedDamage <= 0m)
			return;
		if (Target is not { IsAlive: true } linked)
			return;

		var hpLoss = Math.Max(1, (int)Math.Ceiling((decimal)result.UnblockedDamage));
		_transferringDamage = true;
		try
		{
			await CreatureCmd.Damage(
				choiceContext,
				linked,
				hpLoss,
				ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.SkipHurtAnim,
				Owner,
				cardSource);
		}
		finally
		{
			_transferringDamage = false;
		}
	}
}

/// <summary>灵魂链接目标标记：用于在目标的 Power 栏展示“已被灵魂链接”。</summary>
public sealed class MoreEnchantSoulLinkTargetPower : PowerModel
{
	private sealed class LinkSourceData
	{
		public string SourceName = string.Empty;
	}

	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Single;

	public void SetLinkSourceName(string? sourceName)
	{
		var data = GetInternalData<LinkSourceData>();
		data.SourceName = sourceName?.Trim() ?? string.Empty;
	}

	public string GetLinkSourceName()
	{
		var data = GetInternalData<LinkSourceData>();
		return data.SourceName;
	}

	protected override object InitInternalData()
	{
		return new LinkSourceData();
	}
}

/// <summary>错误的目标：当拥有者获得负面效果时，将该负面转移给随机敌人。</summary>
public sealed class MoreEnchantWrongTargetPower : PowerModel
{
	private sealed class PendingDebuff
	{
		public bool IsTransferring;
		public PowerModel? CanonicalPower;
		public decimal Amount;
		public Creature? Applier;
	}

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public override bool ShouldReceiveCombatHooks => true;

	public override bool TryModifyPowerAmountReceived(
		PowerModel canonicalPower,
		Creature target,
		decimal amount,
		Creature? applier,
		out decimal modifiedAmount)
	{
		modifiedAmount = amount;
		if (target != Owner || amount <= 0m || canonicalPower.Type != PowerType.Debuff)
			return false;

		var data = GetInternalData<PendingDebuff>();
		if (data.IsTransferring)
			return false;

		data.CanonicalPower = canonicalPower.IsMutable ? ModelDb.GetById<PowerModel>(canonicalPower.Id) : canonicalPower;
		data.Amount = amount;
		data.Applier = applier;
		return false;
	}

	public override async Task AfterPowerAmountChanged(
		PlayerChoiceContext choiceContext,
		PowerModel power,
		decimal amount,
		Creature? applier,
		CardModel? cardSource)
	{
		var data = GetInternalData<PendingDebuff>();
		if (data.IsTransferring || data.CanonicalPower == null || data.Amount <= 0m || Owner?.CombatState == null)
			return;
		if (!string.Equals(power.Id.ToString(), data.CanonicalPower.Id.ToString(), StringComparison.Ordinal))
			return;

		var enemies = Owner.CombatState.HittableEnemies;
		if (enemies.Count == 0)
		{
			ClearPending(data);
			return;
		}

		var rng = Owner.Player?.RunState?.Rng?.CombatTargets;
		var randomEnemy = rng != null ? enemies[rng.NextInt(enemies.Count)] : enemies[0];
		var transferPower = data.CanonicalPower;
		var transferAmount = data.Amount;
		data.IsTransferring = true;
		try
		{
			await PowerCmdCompat.Apply(power, Owner, -transferAmount, Owner, cardSource, choiceContext, true);
			await PowerCmdCompat.Apply(
				transferPower.ToMutable(),
				randomEnemy,
				transferAmount,
				Owner,
				cardSource,
				choiceContext);
		}
		finally
		{
			data.IsTransferring = false;
			ClearPending(data);
		}
	}

	protected override object InitInternalData()
	{
		return new PendingDebuff();
	}

	private static void ClearPending(PendingDebuff data)
	{
		data.CanonicalPower = null;
		data.Amount = 0m;
		data.Applier = null;
	}
}
