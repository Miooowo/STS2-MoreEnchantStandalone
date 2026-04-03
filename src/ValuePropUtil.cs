using MegaCrit.Sts2.Core.ValueProps;

namespace MoreEnchant;

internal static class ValuePropUtil
{
	internal static bool IsPoweredAttack(ValueProp props) =>
		props.HasFlag(ValueProp.Move) && !props.HasFlag(ValueProp.Unpowered);

	internal static bool IsPoweredCardOrMonsterMoveBlock(ValueProp props) =>
		props.HasFlag(ValueProp.Move) && !props.HasFlag(ValueProp.Unpowered);
}
