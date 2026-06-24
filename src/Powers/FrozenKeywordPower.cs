using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant.Powers;

/// <summary>仅用于「冻结」关键词悬停说明展示，不参与数值结算。</summary>
public sealed class FrozenKeywordPower : PowerModel
{
	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Single;
}
