using MegaCrit.Sts2.Core.ValueProps;

namespace MoreEnchant;

/// <summary>与游戏内 <c>ValuePropExtensions.IsPoweredAttack</c> 等价（该扩展在 sts2 中为 internal，模组侧需本地复制）。</summary>
internal static class ValuePropCombatUtil
{
	internal static bool IsPoweredAttackMove(ValueProp props) =>
		props.HasFlag(ValueProp.Move) && !props.HasFlag(ValueProp.Unpowered);
}
