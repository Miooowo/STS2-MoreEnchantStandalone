using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MoreEnchant.Enchantments;
using MoreEnchant.Standalone;
using MoreEnchant.Standalone.Compat;

namespace MoreEnchant.scripts;

[ModInitializer("Init")]
public static class Entry
{
	public const string ModId = "MoreEnchantStandalone";

	public static void Init()
	{
		MoreEnchantEnchantmentRegistry.Register<KeenEdgeEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<LikeDaggerEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ChargeUpEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<InitiativeEnergyEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<SurvivorDiscardEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<StreamlineEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ShockChannelEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<FrostChannelEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<SpectralEtherealEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ShieldPlatingEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<SwordArtEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ForgeSwordEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ReturnToHandEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<SnakebiteEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<KafkaMicroCurrentEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ChimeraStrikeEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ChimeraGemEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ChimeraInnateEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ChimeraCompactEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ChimeraBulkyEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ChimeraGiganticEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ChimeraSolidifyEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ChimeraHeavyBladeEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ChimeraEnlightenmentEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ChimeraEntrenchEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ScorchingEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<RescueEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ClumsyCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ObsessionCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<SporeCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<MediocreCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<BellCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<BadLuckCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<InfectionCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<FinaleCurtainEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ShameCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<DoubtCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<FollyCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<DebtCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<GreedCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<PoorSleepCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<DecayCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<RegretCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<AnguishCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<GuiltCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<InjuryCurseEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<CalamityCurseEnchantment>();

		// 可选拓展：若安装了 MultiEnchantmentMod，则启用蛇咬等附魔的 MergeAmount 叠层语义。
		MultiEnchantmentCompat.TryEnableForSnakebite(typeof(SnakebiteEnchantment));

		var harmony = new Harmony(ModId);
		harmony.PatchAll(Assembly.GetExecutingAssembly());

		EnsureGodotScriptsRegistered(Assembly.GetExecutingAssembly());
		_ = MoreEnchantSettingsStore.Get();
	}

	private static void EnsureGodotScriptsRegistered(Assembly assembly)
	{
		try
		{
			var bridgeType = typeof(GodotObject).Assembly.GetType("Godot.Bridge.ScriptManagerBridge");
			var lookupMethod = bridgeType?.GetMethod(
				"LookupScriptsInAssembly",
				BindingFlags.Public | BindingFlags.Static,
				null,
				[typeof(Assembly)],
				null);
			lookupMethod?.Invoke(null, [assembly]);
		}
		catch
		{
			// ignore
		}
	}
}
