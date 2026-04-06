using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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

	public override bool HasExtraCardText => true;

	/// <summary>动态变量名 <c>MicroCurrentDamage</c> / <c>MicroCurrentTurns</c> 供附魔文本使用（Kafka 触电无内置 <see cref="PowerVar{T}"/>）。</summary>
	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get
		{
			yield return new DynamicVar("MicroCurrentDamage", 2m);
			yield return new DynamicVar("MicroCurrentTurns", 2m);
		}
	}

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
			await PowerCmd.Apply(power, targets[0], DynamicVars["MicroCurrentDamage"].BaseValue, applier, Card);
		}
		else
		{
			foreach (var target in targets)
			{
				VfxCmd.PlayOnCreatureCenter(target, VfxCmd.lightningPath);
				var power = proto.ToMutable();
				await PowerCmd.Apply(power, target, DynamicVars["MicroCurrentDamage"].BaseValue, applier, Card);
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
