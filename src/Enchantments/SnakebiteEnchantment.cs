using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MoreEnchant.Standalone;

namespace MoreEnchant.Enchantments;

/// <summary>蛇咬：卡牌获得保留；打出时对目标施加 7 层中毒。</summary>
public sealed class SnakebiteEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	private const decimal PoisonStacks = 7m;

	public override bool HasExtraCardText => true;

	protected override void OnEnchant()
	{
		if (Card != null)
			CardCmd.ApplyKeyword(Card, CardKeyword.Retain);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner?.Creature == null)
			return;

		var state = Card.Owner.Creature.CombatState;
		if (state == null)
			return;

		var targets = ResolvePoisonTargets(cardPlay, state);
		if (targets == null || targets.Count == 0)
			return;

		var applier = Card.Owner.Creature;
		await CreatureCmd.TriggerAnim(applier, "Cast", Card.Owner.Character.CastAnimDelay);

		if (targets.Count == 1)
		{
			VfxCmd.PlayOnCreatureCenter(targets[0], "vfx/vfx_bite");
			await PowerCmd.Apply<PoisonPower>(targets[0], PoisonStacks, applier, Card);
		}
		else
		{
			await PowerCmd.Apply<PoisonPower>(targets, PoisonStacks, applier, Card);
		}
	}

	/// <summary>
	/// <see cref="CardPlay.Target"/> 仅在单点选敌时为非空；全屏/AOE/随机目标打出时常为 null，需按 <see cref="TargetType"/> 解析。
	/// </summary>
	private List<Creature>? ResolvePoisonTargets(CardPlay? cardPlay, CombatState state)
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
