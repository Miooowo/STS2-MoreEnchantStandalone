using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MoreEnchant.Standalone;
using MoreEnchant.Standalone.Compat;

namespace MoreEnchant.Enchantments;

/// <summary>锻造器：拾起带有此附魔的牌时，获得 1 个随机属性锻造器（仅海克斯符文 mod 存在时可用）。</summary>
public sealed class HextechForgeEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private int _pickupGrantedGate;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && HextechRunesCompat.IsHextechRunesModAvailable();

	internal bool TryTakePickupGrantOnce() =>
		Interlocked.CompareExchange(ref _pickupGrantedGate, 1, 0) == 0;

	internal void ResetPickupGrantGateForClonedCard() =>
		Interlocked.Exchange(ref _pickupGrantedGate, 0);
}

/// <summary>战斗中首次打出时：获得 1 个对应阶位的临时海克斯，战斗结束后移除。</summary>
public abstract class HextechTemporaryRuneFirstPlayEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private readonly List<RelicModel> _temporaryRunes = [];
	private bool _pendingFirstPlayInCombat = true;

	public abstract EnchantmentRewardRarity RewardRarity { get; }
	protected abstract HextechRuneTier RuneTier { get; }

	public override bool HasExtraCardText => true;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && HextechRunesCompat.IsHextechRunesModAvailable();

	public override Task BeforeCombatStart()
	{
		_pendingFirstPlayInCombat = true;
		_temporaryRunes.Clear();
		return Task.CompletedTask;
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (!_pendingFirstPlayInCombat)
			return;
		if (Card?.Owner is not { Creature: { } creature } owner)
			return;

		_pendingFirstPlayInCombat = false;
		await CreatureCmd.TriggerAnim(creature, "Cast", owner.Character.CastAnimDelay);
		var rune = await HextechRunesCompat.TryGrantTemporaryRune(owner, RuneTier).ConfigureAwait(false);
		if (rune != null)
			_temporaryRunes.Add(rune);
	}

	public override async Task AfterCombatEnd(CombatRoom room)
	{
		if (Card?.Owner is not { } owner)
			return;
		await HextechRunesCompat.RemoveTemporaryRunes(owner, _temporaryRunes).ConfigureAwait(false);
	}
}

/// <summary>白银海克斯：战斗中首次打出时获得 1 个临时白银海克斯，战斗结束后移除。</summary>
public sealed class SilverHextechEnchantment : HextechTemporaryRuneFirstPlayEnchantment
{
	public override EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;
	protected override HextechRuneTier RuneTier => HextechRuneTier.Silver;
}

/// <summary>黄金海克斯：战斗中首次打出时获得 1 个临时黄金海克斯，战斗结束后移除。</summary>
public sealed class GoldHextechEnchantment : HextechTemporaryRuneFirstPlayEnchantment
{
	public override EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;
	protected override HextechRuneTier RuneTier => HextechRuneTier.Gold;
}

/// <summary>棱彩海克斯：战斗中首次打出时获得 1 个临时棱彩海克斯，战斗结束后移除。</summary>
public sealed class PrismaticHextechEnchantment : HextechTemporaryRuneFirstPlayEnchantment
{
	public override EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Special;
	protected override HextechRuneTier RuneTier => HextechRuneTier.Prismatic;
}
