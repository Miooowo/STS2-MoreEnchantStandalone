using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Kafka;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>
/// 微电流：打出时对目标施加 Kafka「触电」（伤害与持续由 <c>ShockPower</c> 解释；此处施加数值为每跳伤害）。
/// 仅在与 Kafka 同开且其触电已登记到 ModelDb 时才会进入奖励随机池。
/// </summary>
public sealed class KafkaMicroCurrentEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Common;

	/// <summary>传给 <see cref="PowerCmd.Apply"/> 的数值；若 Kafka 触电用层数表示持续、伤害由 Power 内部固定，可在确认其行为后改为 2 等。</summary>
	private const decimal ElectrocuteAmount = 2m;

	public override bool HasExtraCardText => true;

	public override bool CanEnchant(CardModel card)
	{
		if (!KafkaElectrocuteSupport.TryGetElectrocutePowerType(out var powerType) ||
		    powerType == null ||
		    !KafkaElectrocuteSupport.IsElectrocuteRegisteredInModelDb(powerType))
			return false;

		return base.CanEnchant(card);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner?.Creature == null)
			return;

		if (!KafkaElectrocuteSupport.TryGetElectrocutePowerType(out var powerType) || powerType == null)
			return;

		var state = Card.Owner.Creature.CombatState;
		if (state == null)
			return;

		var targets = ResolveElectrocuteTargets(cardPlay, state);
		if (targets == null || targets.Count == 0)
			return;

		if (!KafkaElectrocuteSupport.IsElectrocuteRegisteredInModelDb(powerType))
			return;

		var applier = Card.Owner.Creature;
		await CreatureCmd.TriggerAnim(applier, "Cast", Card.Owner.Character.CastAnimDelay);

		var proto = ModelDb.DebugPower(powerType);
		if (targets.Count == 1)
		{
			VfxCmd.PlayOnCreatureCenter(targets[0], VfxCmd.lightningPath);
			var power = proto.ToMutable();
			await PowerCmd.Apply(power, targets[0], ElectrocuteAmount, applier, Card);
		}
		else
		{
			foreach (var target in targets)
			{
				VfxCmd.PlayOnCreatureCenter(target, VfxCmd.lightningPath);
				var power = proto.ToMutable();
				await PowerCmd.Apply(power, target, ElectrocuteAmount, applier, Card);
			}
		}
	}

	private List<Creature>? ResolveElectrocuteTargets(CardPlay? cardPlay, CombatState state)
	{
		var hittable = state.HittableEnemies;

		if (cardPlay?.Target != null)
		{
			if (!hittable.Contains(cardPlay.Target))
				return null;
			return new List<Creature> { cardPlay.Target };
		}

		return Card!.TargetType switch
		{
			TargetType.AllEnemies => hittable.ToList(),
			TargetType.RandomEnemy or TargetType.AnyEnemy =>
				SingleTargetFrom(Card.Owner.RunState.Rng.CombatTargets.NextItem(hittable)),
			_ => null,
		};
	}

	private static List<Creature>? SingleTargetFrom(Creature? c) =>
		c != null ? new List<Creature> { c } : null;
}
