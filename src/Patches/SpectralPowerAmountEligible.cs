using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MoreEnchant.Patches;

/// <summary>
/// 幽灵对「能力层数/数值」乘 1.5 时，仅作用于以该数值直接驱动格挡的能力（与卡面 PowerVar 一致），
/// 不作用于力量、敏捷等纯数值增益。
/// </summary>
internal static class SpectralPowerAmountEligible
{
	internal static bool AllowsScaling(PowerModel power) =>
		power is CrimsonMantlePower
			or RagePower
			or SelfFormingClayPower
			or RampartPower
			or PillarOfCreationPower
			or ParryPower
			or BlockNextTurnPower
			or SpiritOfAshPower
			or SneakyPower
			or SkittishPower
			or AfterimagePower
			or FeelNoPainPower
			or ShroudPower
			or DanseMacabrePower
			or CurlUpPower
			or CoolantPower
			or ChildOfTheStarsPower
			or PlatingPower
			or BeaconOfHopePower;
}
