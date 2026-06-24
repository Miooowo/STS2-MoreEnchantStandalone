using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MoreEnchant.Standalone;
using MoreEnchant.Standalone.Compat;

namespace MoreEnchant.Enchantments;

/// <summary>锻造器：拾起带有此附魔的牌时，获得 1 个随机属性锻造器（仅海克斯符文 mod 存在时可用）。</summary>
public sealed class HextechForgeEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private int _pickupGrantedCount;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	public override bool HasExtraCardText => false;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && HextechRunesCompat.IsHextechRunesModAvailable();

	/// <summary>
	/// 以“牌组中同一张卡引用出现次数”判定本次是否应触发。
	/// 可避免同一次入组被多钩子重复触发，同时支持同一实例被重复加入牌组时按次数发放。
	/// </summary>
	internal bool TryTakePickupGrant(Player owner)
	{
		if (Card == null)
			return false;

		var occurrences = owner.Deck.Cards.Count(c => ReferenceEquals(c, Card));
		if (occurrences <= _pickupGrantedCount)
			return false;

		_pickupGrantedCount = occurrences;
		return true;
	}

	internal void ResetPickupGrantGateForClonedCard() =>
		_pickupGrantedCount = 0;
}

/// <summary>战斗中首次打出时：获得 1 个对应阶位的临时海克斯，进入下一个房间后移除。</summary>
public abstract class HextechTemporaryRuneFirstPlayEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private readonly List<ModelId> _temporaryRunes = [];
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
		var runeId = await HextechRunesCompat.TryGrantTemporaryRune(owner, RuneTier);
		if (runeId != null)
			_temporaryRunes.Add(runeId);
	}

	public override async Task AfterRoomEntered(AbstractRoom room)
	{
		if (Card?.Owner is not { } owner)
			return;
		await HextechRunesCompat.RemoveTemporaryRunes(owner, _temporaryRunes);
	}
}

/// <summary>白银海克斯：战斗中首次打出时获得 1 个临时白银海克斯，进入下一个房间后移除。</summary>
public sealed class SilverHextechEnchantment : HextechTemporaryRuneFirstPlayEnchantment
{
	public override EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;
	protected override HextechRuneTier RuneTier => HextechRuneTier.Silver;
}

/// <summary>黄金海克斯：战斗中首次打出时获得 1 个临时黄金海克斯，进入下一个房间后移除。</summary>
public sealed class GoldHextechEnchantment : HextechTemporaryRuneFirstPlayEnchantment
{
	public override EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;
	protected override HextechRuneTier RuneTier => HextechRuneTier.Gold;
}

/// <summary>棱彩海克斯：战斗中首次打出时获得 1 个临时棱彩海克斯，进入下一个房间后移除。</summary>
public sealed class PrismaticHextechEnchantment : HextechTemporaryRuneFirstPlayEnchantment
{
	public override EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Special;
	protected override HextechRuneTier RuneTier => HextechRuneTier.Prismatic;
}
