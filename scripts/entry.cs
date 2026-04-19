using System.Reflection;
using Godot;
using HarmonyLib;
using Steamworks;
using MegaCrit.Sts2.Core.Modding;
using MoreEnchant.Enchantments;
using MoreEnchant.Enchantments.Beta;
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
		MoreEnchantEnchantmentRegistry.Register<MagicCorruptionEnchantment>();
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
		MoreEnchantEnchantmentRegistry.Register<SteadfastExhaustEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<RuinAutoPlayEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<QuickDrawEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<BloodlettingEnergyEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ArmingUpgradeEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<TerrorVulnerableEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ShiverVulnerableEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<NeutralWeakEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<BackpackDrawDiscardEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<StarlightStarsEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<EscortSummonEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ExtraHitEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ShredDebrisEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<SlipperyFirstPlayEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<GainBufferPowerEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ReplicaExhaustCopyEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<MeltDoubleVulnerableEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<PlunderDrawEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<DuelStrengthEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<DemonShieldShareBlockEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ReaperDoomOnDamageEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<EagerPerAttackEnergyEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<PounceNextSkillFreeEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<PincerFlankingMarkEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<SlyKeywordEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<SurgeEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<FeedEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ColossusEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<HellraiserEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<CorrosiveWaveEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<CalamityWaveDoomEnchantment>();
		MoreEnchantEnchantmentRegistry.Register<ForgeWaveEnchantment>();

		// 可选拓展：若安装了 MultiEnchantmentMod，则启用蛇咬等附魔的 MergeAmount 叠层语义。
		MultiEnchantmentCompat.TryEnableForSnakebite(typeof(SnakebiteEnchantment));

		var harmony = new Harmony(ModId);
		harmony.PatchAll(Assembly.GetExecutingAssembly());

		EnsureGodotScriptsRegistered(Assembly.GetExecutingAssembly());
		_ = MoreEnchantSettingsStore.Get();

		// 与游戏本体一致：Slay the Spire 2 Steam AppID（MegaCrit.Sts2.Core.Platform.Steam.SteamInitializer.steamAppId）
		const uint Sts2SteamAppId = 2868840u;
		if (!SteamApps.BIsSubscribedApp(new AppId_t(Sts2SteamAppId)))
		{
			const string msg = "MoreEnchantStandalone 需要在 Steam 正版《杀戮尖塔2》中运行。";
			GD.PushError(msg);
			throw new InvalidOperationException(msg);
		}
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
