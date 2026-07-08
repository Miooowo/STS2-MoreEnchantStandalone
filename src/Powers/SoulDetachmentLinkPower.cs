using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MoreEnchant.Powers;

/// <summary>
/// 灵魂链接：灵魂受伤时将实际伤害同步给本体，并确保灵魂持续晕眩。
/// </summary>
public sealed class SoulDetachmentLinkPower : PowerModel
{
	private static bool _transferringDamage;

	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	protected override bool IsVisibleInternal => false;

	public override bool ShouldPlayVfx => false;

	public void SetBodyTarget(Creature body)
	{
		Target = body;
	}

	public override async Task AfterDamageGiven(
		PlayerChoiceContext choiceContext,
		Creature? dealer,
		DamageResult result,
		ValueProp props,
		Creature target,
		CardModel? cardSource)
	{
		if (target != Owner || _transferringDamage || result.UnblockedDamage <= 0)
			return;

		var body = Target;
		if (body == null || body.IsDead)
			return;

		int damage = Math.Max(1, (int)Math.Ceiling(Convert.ToDecimal(result.UnblockedDamage)));
		_transferringDamage = true;
		try
		{
			await CreatureCmd.Damage(
				choiceContext,
				body,
				damage,
				ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.SkipHurtAnim,
				cardSource,
				null);
		}
		finally
		{
			_transferringDamage = false;
		}
	}

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (creature == Target && Owner.IsAlive)
			await CreatureCmd.Kill(Owner, force: true);
	}
}
