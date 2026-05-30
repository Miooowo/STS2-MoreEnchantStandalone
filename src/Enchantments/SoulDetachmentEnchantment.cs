using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Compat;
using MoreEnchant.Powers;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>
/// 灵魂抽离：仅攻击牌。首次打出时抽离目标灵魂，生成低透明且持续晕眩的灵魂克隆，并建立伤害链接。
/// </summary>
public sealed class SoulDetachmentEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private bool _usedThisCombat;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Rare;

	public override bool HasExtraCardText => true;

	public override bool CanEnchantCardType(CardType cardType) => cardType == CardType.Attack;

	public override bool CanEnchant(CardModel card) =>
		base.CanEnchant(card) && CardEnchantEligibility.CardHasMoveDamageNumbers(card);

	public override Task BeforeCombatStart()
	{
		_usedThisCombat = false;
		return Task.CompletedTask;
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (_usedThisCombat || Card?.Owner?.Creature?.CombatState == null || cardPlay?.Target == null)
			return;

		var soulTarget = ResolveBodyTarget(cardPlay.Target);
		if (!soulTarget.IsEnemy || soulTarget.IsDead || soulTarget.Monster == null)
			return;

		if (await SummonSoulAndLink(choiceContext, soulTarget))
		{
			_usedThisCombat = true;
		}
	}

	private async Task<bool> SummonSoulAndLink(PlayerChoiceContext choiceContext, Creature body)
	{
		if (Card?.Owner?.Creature == null || Card.Owner.Creature.CombatState == null || body.Monster == null)
			return false;

		var combatState = Card.Owner.Creature.CombatState;
		// body.Monster 是战斗中的 mutable 实例，不能直接再 ToMutable()。
		// 必须从 canonical 模板克隆，再作为新敌人加入战场。
		var soulModel = body.Monster.CanonicalInstance.ToMutable();
		var soul = await CreatureCmd.Add(soulModel, combatState, CombatSide.Enemy, slotName: null);
		// 灵魂作为“纯沙包”存在：清空克隆怪物携带的全部原生能力，
		// 仅保留后续显式施加的灵魂链接能力与晕眩控制。
		foreach (var power in soul.Powers.ToList())
			await PowerCmd.Remove(power);

		// 保持灵魂与本体当前血量一致，便于玩家直观理解“受伤联动”。
		int soulMaxHp = Math.Max(1, body.CurrentHp);
		int soulCurrentHp = Math.Max(1, body.CurrentHp);
		await CreatureCmd.SetMaxHp(soul, soulMaxHp);
		await CreatureCmd.SetCurrentHp(soul, soulCurrentHp);

		var link = await PowerCmdCompat.Apply<SoulDetachmentLinkPower>(soul, 1m, Card.Owner.Creature, Card);
		link?.SetBodyTarget(body);

		await CreatureCmd.Stun(soul);
		return true;
	}

	private static Creature ResolveBodyTarget(Creature target) =>
		target.GetPower<SoulDetachmentLinkPower>()?.Target is { } body ? body : target;
}
