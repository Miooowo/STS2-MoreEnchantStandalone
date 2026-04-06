using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MoreEnchant.Standalone;
using MoreEnchant.Standalone.Compat;

namespace MoreEnchant.Enchantments;

/// <summary>蛇咬：保留；此牌基础耗能变为 2（非 X 费）；打出时对目标施加中毒（层数由 <see cref="PowerVar{T}"/> 驱动）。</summary>
public sealed class SnakebiteEnchantment : ModEnchantmentTemplate, IRewardEnchantRarity
{
	private const decimal PoisonPerLayer = 7m;

	public EnchantmentRewardRarity RewardRarity => EnchantmentRewardRarity.Uncommon;

	/// <summary>多重蛇咬时仅首张参与紫字，避免 MEM 对每实例各拼一段 <c>extraCardText</c>。</summary>
	public override bool HasExtraCardText =>
		Card == null || MultiEnchantmentCompat.IsCanonicalSnakebiteForOnPlay(this);

	public override bool CanEnchant(CardModel card)
	{
		// MultiEnchantmentMod 会在 ApplyEnchantment 前强制检查 CanEnchant(card) 并直接抛异常。
		// 蛇咬只依赖“打出时能执行附魔 hook”，不要求卡牌具有选敌目标；无目标牌会按规则挑随机敌人上毒。
		// 为避免外部/控制台附魔（例如 Havoc）被过严的默认规则拒绝，这里直接放行。
		return true;
	}

	protected override IEnumerable<DynamicVar> CanonicalVars
	{
		get
		{
			yield return new EnergyVar(2);
			yield return new PowerVar<PoisonPower>(PoisonPerLayer);
		}
	}

	public override void RecalculateValues()
	{
		base.RecalculateValues();
		if (Card == null)
			return;

		if (!Card.EnergyCost.CostsX)
			Card.EnergyCost.SetCustomBaseCost(2);

		MultiEnchantmentCompat.RefreshAllSnakebitePoisonDisplaysOnCard(Card, PoisonPerLayer, this);
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips =>
		new IHoverTip[] { HoverTipFactory.FromPower<PoisonPower>() };

	protected override void OnEnchant()
	{
		if (Card != null)
			CardCmd.ApplyKeyword(Card, CardKeyword.Retain);
	}

	public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (Card?.Owner?.Creature == null)
			return;

		// MultiEnchantment 对「每个额外附魔实例」都会调 OnPlay；非首个实例必须跳过，否则会多次上毒。
		if (!MultiEnchantmentCompat.IsCanonicalSnakebiteForOnPlay(this))
			return;

		_ = MultiEnchantmentCompat.GetActiveTotalAmountOrDefault(this, defaultAmount: 1);
		int layers = MultiEnchantmentCompat.GetTotalSnakebiteLayersOnCard(Card, this);
		decimal poisonAmount = PoisonPerLayer * layers;
		if (poisonAmount <= 0)
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
			await PowerCmd.Apply<PoisonPower>(targets[0], poisonAmount, applier, Card);
		}
		else
		{
			await PowerCmd.Apply<PoisonPower>(targets, poisonAmount, applier, Card);
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

		// 格挡/自身等无选敌目标时，cardPlay.Target 为空且 TargetType 非群体/随机敌；按需求对随机敌人上毒。
		return Card!.TargetType switch
		{
			TargetType.AllEnemies => hittable.ToList(),
			TargetType.RandomEnemy or TargetType.AnyEnemy =>
				SingleTargetFrom(Card.Owner.RunState.Rng.CombatTargets.NextItem(hittable)),
			_ => SingleTargetFrom(Card.Owner.RunState.Rng.CombatTargets.NextItem(hittable)),
		};
	}

	private static List<Creature>? SingleTargetFrom(Creature? c) =>
		c != null ? new List<Creature> { c } : null;
}
