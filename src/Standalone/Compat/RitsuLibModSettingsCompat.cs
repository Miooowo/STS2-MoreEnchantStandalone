using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MoreEnchant.scripts;

namespace MoreEnchant.Standalone.Compat;

/// <summary>
/// RitsuLib 软依赖：注册 MoreEnchant 可视化设置页（无编译时硬依赖）。
/// </summary>
internal static class RitsuLibModSettingsCompat
{
	private const string RitsuLibAssemblyName = "STS2RitsuLib";
	private const string FrameworkTypeName = "STS2RitsuLib.RitsuLibFramework";
	private const string ModSettingsTextTypeName = "STS2RitsuLib.Settings.ModSettingsText";
	private const string SettingsLocTable = "events";

	private static readonly object Sync = new();
	private static readonly MegaCrit.Sts2.Core.Logging.Logger Log = new("MoreEnchantStandalone", LogType.Generic);
	private static bool _registered;
	private static Type? _modSettingsTextType;
	private static MethodInfo? _modSettingsTextLocStringMethod;
	private static MethodInfo? _callbackBindingFactoryMethod;

	internal static bool TryRegisterSettingsPage()
	{
		lock (Sync)
		{
			if (_registered)
				return true;
		}

		try
		{
			var frameworkType = ResolveType(FrameworkTypeName, RitsuLibAssemblyName);
			_modSettingsTextType = ResolveType(ModSettingsTextTypeName, RitsuLibAssemblyName);
			_modSettingsTextLocStringMethod = _modSettingsTextType?.GetMethod(
				"LocString",
				BindingFlags.Public | BindingFlags.Static,
				null,
				[typeof(string), typeof(string), typeof(string)],
				null);
			if (frameworkType == null || _modSettingsTextType == null || _modSettingsTextLocStringMethod == null)
			{
				Log.Debug("[RitsuLibModSettingsCompat] RitsuLib types are not ready yet.");
				return false;
			}

			var registerMethod = frameworkType.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.FirstOrDefault(static m =>
				{
					if (m.Name != "RegisterModSettings")
						return false;
					var ps = m.GetParameters();
					return ps.Length == 3 && ps[0].ParameterType == typeof(string);
				});
			if (registerMethod == null)
			{
				Log.Warn("[RitsuLibModSettingsCompat] RegisterModSettings method not found.");
				return false;
			}

			var pageBuilderType = registerMethod.GetParameters()[1].ParameterType.GetGenericArguments().FirstOrDefault();
			if (pageBuilderType == null)
			{
				Log.Warn("[RitsuLibModSettingsCompat] Failed to resolve ModSettingsPageBuilder type.");
				return false;
			}

			var configureMethod = typeof(RitsuLibModSettingsCompat).GetMethod(
				nameof(ConfigurePage),
				BindingFlags.NonPublic | BindingFlags.Static);
			if (configureMethod == null)
			{
				Log.Warn("[RitsuLibModSettingsCompat] ConfigurePage method is missing.");
				return false;
			}

			var configureDelegate = CreateTypedDelegate(pageBuilderType, configureMethod);
			registerMethod.Invoke(null, [Entry.ModId, configureDelegate, null]);
			_registered = true;
			Log.Info("[RitsuLibModSettingsCompat] Registered MoreEnchant settings page.");
			return true;
		}
		catch (Exception ex)
		{
			// 不锁死失败状态：RitsuLib 可能在稍后才完成加载，允许后续重试。
			Log.Warn($"[RitsuLibModSettingsCompat] Register failed: {ex.Message}");
			return false;
		}
	}

	private static void ConfigurePage(object pageBuilder)
	{
		var pageType = pageBuilder.GetType();
		var sectionBuilderType = ResolveSectionBuilderType(pageType);
		if (sectionBuilderType == null)
			return;

		var configureSectionMethod = typeof(RitsuLibModSettingsCompat).GetMethod(
			nameof(ConfigureGeneralSection),
			BindingFlags.NonPublic | BindingFlags.Static);
		if (configureSectionMethod == null)
			return;

		var sectionDelegate = CreateTypedDelegate(sectionBuilderType, configureSectionMethod);

		InvokeFluent(pageBuilder, "WithTitle", BuildText("ME_SETTINGS.page.title", "MoreEnchant"));
		InvokeFluent(pageBuilder, "WithModDisplayName", BuildText("ME_SETTINGS.page.modDisplayName", "MoreEnchant"));
		InvokeFluent(pageBuilder, "AddSection", "more_enchant_general", sectionDelegate);

		var configurePoolFilterMethod = typeof(RitsuLibModSettingsCompat).GetMethod(
			nameof(ConfigurePoolFilterSection),
			BindingFlags.NonPublic | BindingFlags.Static);
		if (configurePoolFilterMethod != null)
		{
			var poolFilterDelegate = CreateTypedDelegate(sectionBuilderType, configurePoolFilterMethod);
			InvokeFluent(pageBuilder, "AddSection", "more_enchant_pool_filter", poolFilterDelegate);
		}

		var configureBlacklistMethod = typeof(RitsuLibModSettingsCompat).GetMethod(
			nameof(ConfigureBlacklistSection),
			BindingFlags.NonPublic | BindingFlags.Static);
		if (configureBlacklistMethod != null)
		{
			var blacklistDelegate = CreateTypedDelegate(sectionBuilderType, configureBlacklistMethod);
			InvokeFluent(pageBuilder, "AddSection", "more_enchant_blacklist", blacklistDelegate);
		}
	}

	private static void ConfigureGeneralSection(object sectionBuilder)
	{
		InvokeFluent(sectionBuilder, "WithTitle", BuildText("ME_SETTINGS.section.general.title", "附魔设置"));
		InvokeFluent(sectionBuilder, "WithDescription",
			BuildText("ME_SETTINGS.section.general.description", "使用 RitsuLib 原生控件。联机客机为只读，跟随房主设置。"));
		InvokeFluent(sectionBuilder, "WithEnabledWhen", new Func<bool>(() => !IsMultiplayerClient()));

		AddIntSliderEntry(sectionBuilder, "reward_enchant_chance", "奖励附魔概率",
			() => MoreEnchantSettingsStore.Get().RewardEnchantChancePercent,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.RewardEnchantChancePercent = value;
			},
			"战斗奖励附魔概率（0-100）。");

		AddToggleEntry(sectionBuilder, "starting_deck_enchant_enabled", "初始卡组附魔开关",
			() => MoreEnchantSettingsStore.Get().StartingDeckEnchantEnabled,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.StartingDeckEnchantEnabled = value;
			},
			"控制开局初始牌组是否可随机附魔。");

		AddIntSliderEntry(sectionBuilder, "starting_deck_enchant_chance", "初始卡组附魔概率",
			() => MoreEnchantSettingsStore.Get().StartingDeckEnchantChancePercent,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.StartingDeckEnchantChancePercent = value;
			},
			"初始卡组附魔概率（0-100）。");

		AddToggleEntry(sectionBuilder, "combat_generated_enchant_enabled", "战斗生牌附魔开关",
			() => MoreEnchantSettingsStore.Get().CombatGeneratedEnchantEnabled,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.CombatGeneratedEnchantEnabled = value;
			},
			"控制战斗中生成的新牌是否可随机附魔。");

		AddIntSliderEntry(sectionBuilder, "combat_generated_enchant_chance", "战斗生牌附魔概率",
			() => MoreEnchantSettingsStore.Get().CombatGeneratedEnchantChancePercent,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.CombatGeneratedEnchantChancePercent = value;
			},
			"战斗生牌附魔概率（0-100）。");

		AddToggleEntry(sectionBuilder, "transform_enchant_enabled", "变牌附魔开关",
			() => MoreEnchantSettingsStore.Get().TransformEnchantEnabled,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.TransformEnchantEnabled = value;
			},
			"控制变牌结果是否可随机附魔。");

		AddIntSliderEntry(sectionBuilder, "transform_enchant_chance", "变牌附魔概率",
			() => MoreEnchantSettingsStore.Get().TransformEnchantChancePercent,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.TransformEnchantChancePercent = value;
			},
			"变牌附魔概率（0-100）。");

		AddToggleEntry(sectionBuilder, "shop_enchant_enabled", "商店附魔开关",
			() => MoreEnchantSettingsStore.Get().ShopEnchantEnabled,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.ShopEnchantEnabled = value;
			},
			"控制商店新出现卡牌是否可被随机附魔。");

		AddIntSliderEntry(sectionBuilder, "shop_enchant_chance", "商店附魔概率",
			() => MoreEnchantSettingsStore.Get().ShopEnchantChancePercent,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.ShopEnchantChancePercent = value;
			},
			"商店附魔概率（0-100）。");

		AddToggleEntry(sectionBuilder, "ancient_reward_enchant_enabled", "先古附魔开关",
			() => MoreEnchantSettingsStore.Get().AncientRewardEnchantEnabled,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.AncientRewardEnchantEnabled = value;
			},
			"控制先古奖励附魔功能开关。");

		AddIntSliderEntry(sectionBuilder, "ancient_reward_enchant_chance", "先古附魔概率",
			() => MoreEnchantSettingsStore.Get().AncientRewardEnchantChancePercent,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.AncientRewardEnchantChancePercent = value;
			},
			"先古奖励附魔概率（0-100）。");

		AddToggleEntry(sectionBuilder, "deck_direct_enchant_enabled", "直加牌组附魔开关",
			() => MoreEnchantSettingsStore.Get().DeckDirectEnchantEnabled,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.DeckDirectEnchantEnabled = value;
			},
			"控制直接加入牌组的卡牌是否可随机附魔。");

		AddIntSliderEntry(sectionBuilder, "deck_direct_enchant_chance", "直加牌组附魔概率",
			() => MoreEnchantSettingsStore.Get().DeckDirectEnchantChancePercent,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.DeckDirectEnchantChancePercent = value;
			},
			"直加牌组附魔概率（0-100）。");

		AddToggleEntry(sectionBuilder, "beta_reward_enchantments_enabled", "启用 Beta 附魔池",
			() => MoreEnchantSettingsStore.Get().BetaRewardEnchantmentsEnabled,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.BetaRewardEnchantmentsEnabled = value;
			},
			"启用后奖励池会包含 Beta 附魔。");

		AddToggleEntry(sectionBuilder, "use_chimera_rarity_by_card_rarity", "按卡牌稀有度使用默认权重",
			() => MoreEnchantSettingsStore.Get().UseChimeraRarityByCardRarity,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.UseChimeraRarityByCardRarity = value;
			},
			"启用后按卡牌稀有度自动使用预设权重；关闭后使用下方自定义权重。");

		AddIntSliderEntry(sectionBuilder, "weight_common", "普通权重",
			() => MoreEnchantSettingsStore.Get().WeightCommon,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.WeightCommon = value;
			},
			"自定义模式下的普通档相对权重（0-2000）。", 0, 2000, 1);
		ConfigureEntryEnabledWhen(sectionBuilder, "weight_common", () => !MoreEnchantSettingsStore.Get().UseChimeraRarityByCardRarity);

		AddIntSliderEntry(sectionBuilder, "weight_uncommon", "罕见权重",
			() => MoreEnchantSettingsStore.Get().WeightUncommon,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.WeightUncommon = value;
			},
			"自定义模式下的罕见档相对权重（0-2000）。", 0, 2000, 1);
		ConfigureEntryEnabledWhen(sectionBuilder, "weight_uncommon", () => !MoreEnchantSettingsStore.Get().UseChimeraRarityByCardRarity);

		AddIntSliderEntry(sectionBuilder, "weight_curse", "诅咒权重",
			() => MoreEnchantSettingsStore.Get().WeightCurse,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.WeightCurse = value;
			},
			"自定义模式下的诅咒档相对权重（0-2000）。", 0, 2000, 1);
		ConfigureEntryEnabledWhen(sectionBuilder, "weight_curse", () => !MoreEnchantSettingsStore.Get().UseChimeraRarityByCardRarity);

		AddIntSliderEntry(sectionBuilder, "weight_rare", "稀有权重",
			() => MoreEnchantSettingsStore.Get().WeightRare,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.WeightRare = value;
			},
			"自定义模式下的稀有档相对权重（0-2000）。", 0, 2000, 1);
		ConfigureEntryEnabledWhen(sectionBuilder, "weight_rare", () => !MoreEnchantSettingsStore.Get().UseChimeraRarityByCardRarity);

		AddIntSliderEntry(sectionBuilder, "weight_special", "特殊权重",
			() => MoreEnchantSettingsStore.Get().WeightSpecial,
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.WeightSpecial = value;
			},
			"自定义模式下的特殊档相对权重（0-2000）。", 0, 2000, 1);
		ConfigureEntryEnabledWhen(sectionBuilder, "weight_special", () => !MoreEnchantSettingsStore.Get().UseChimeraRarityByCardRarity);
	}

	private static void ConfigurePoolFilterSection(object sectionBuilder)
	{
		InvokeFluent(sectionBuilder, "WithTitle",
			BuildText("ME_SETTINGS.section.pool_filter.title", "仅允许指定附魔"));
		InvokeFluent(sectionBuilder, "WithDescription",
			BuildText("ME_SETTINGS.section.pool_filter.description",
				"输入附魔 ID / 英文名 / 中文名（逗号或换行分隔）。留空不限制；非空时随机池仅保留匹配项。"));
		InvokeFluent(sectionBuilder, "WithEnabledWhen", new Func<bool>(() => !IsMultiplayerClient()));

		AddMultilineStringEntry(sectionBuilder, "reward_enchant_only_filter", "仅允许这些附魔",
			() => MoreEnchantSettingsStore.Get().RewardEnchantOnlyFilter ?? "",
			value =>
			{
				var settings = MoreEnchantSettingsStore.Get();
				settings.RewardEnchantOnlyFilter = value ?? "";
			},
			"例如：SCORCHING_ENCHANTMENT 或 灼热。匹配失败的名称会被忽略。",
			"ID / name / 中文名…");
	}

	private static void ConfigureBlacklistSection(object sectionBuilder)
	{
		InvokeFluent(sectionBuilder, "WithTitle",
			BuildText("ME_SETTINGS.section.blacklist.title", "附魔黑名单"));
		InvokeFluent(sectionBuilder, "WithDescription",
			BuildText("ME_SETTINGS.section.blacklist.description",
				"关闭开关后，该附魔不会出现在对局随机附魔池中（默认全部开启）。"));
		InvokeFluent(sectionBuilder, "WithEnabledWhen", new Func<bool>(() => !IsMultiplayerClient()));
		try
		{
			InvokeFluent(sectionBuilder, "Collapsible", true);
		}
		catch (MissingMethodException)
		{
			// 旧版 RitsuLib 可能无 Collapsible。
		}

		IEnumerable<MegaCrit.Sts2.Core.Models.EnchantmentModel> candidates;
		try
		{
			candidates = MoreEnchantCardRewardUtil.EnumerateSettingsPoolCandidates()
				.OrderBy(static e => e.Title.GetFormattedText(), StringComparer.CurrentCultureIgnoreCase)
				.ToList();
		}
		catch (Exception ex)
		{
			Log.Debug($"[RitsuLibModSettingsCompat] Blacklist candidates unavailable: {ex.Message}");
			return;
		}

		foreach (var enchantment in candidates)
		{
			var idEntry = enchantment.Id.Entry;
			var toggleId = "blacklist_" + idEntry;
			string label;
			try
			{
				label = enchantment.Title.GetFormattedText();
				if (string.IsNullOrWhiteSpace(label))
					label = idEntry;
			}
			catch
			{
				label = idEntry;
			}

			AddToggleEntryLiteral(sectionBuilder, toggleId, label, idEntry,
				() => !EnchantmentPoolFilter.IsBlacklisted(idEntry, MoreEnchantSettingsStore.Get()),
				value =>
				{
					var settings = MoreEnchantSettingsStore.Get();
					EnchantmentPoolFilter.SetBlacklisted(idEntry, blacklisted: !value, settings);
				});
		}
	}

	private static void ConfigureEntryEnabledWhen(object sectionBuilder, string id, Func<bool> predicate)
	{
		InvokeFluent(sectionBuilder, "WithEntryEnabledWhen", id, predicate);
	}

	private static void AddToggleEntry(
		object sectionBuilder,
		string id,
		string label,
		Func<bool> read,
		Action<bool> write,
		string? description = null)
	{
		var binding = CreateCallbackBinding(typeof(bool), $"settings::{id}", () => read(), value => write((bool)value));
		InvokeFluent(sectionBuilder, "AddToggle", id, BuildText(SettingLabelKey(id), label), binding,
			description == null ? null : BuildText(SettingDescriptionKey(id), description), null);
	}

	/// <summary>黑名单项：标签用运行时附魔标题（Literal），描述为 Id.Entry。</summary>
	private static void AddToggleEntryLiteral(
		object sectionBuilder,
		string id,
		string label,
		string description,
		Func<bool> read,
		Action<bool> write)
	{
		var binding = CreateCallbackBinding(typeof(bool), $"settings::{id}", () => read(), value => write((bool)value));
		InvokeFluent(sectionBuilder, "AddToggle", id, BuildLiteralText(label), binding,
			BuildLiteralText(description), null);
	}

	private static void AddMultilineStringEntry(
		object sectionBuilder,
		string id,
		string label,
		Func<string> read,
		Action<string> write,
		string? description = null,
		string? placeholder = null)
	{
		var binding = CreateCallbackBinding(typeof(string), $"settings::{id}", () => read(), value => write((string)value));
		InvokeFluent(sectionBuilder, "AddMultilineString", id, BuildText(SettingLabelKey(id), label), binding,
			placeholder == null ? null : BuildText($"{SettingLabelKey(id)}.placeholder", placeholder),
			null,
			description == null ? null : BuildText(SettingDescriptionKey(id), description));
	}

	private static void AddIntSliderEntry(
		object sectionBuilder,
		string id,
		string label,
		Func<int> read,
		Action<int> write,
		string? description = null,
		int min = 0,
		int max = 100,
		int step = 1)
	{
		var binding = CreateCallbackBinding(typeof(int), $"settings::{id}", () => read(), value => write((int)value));
		InvokeFluent(sectionBuilder, "AddIntSlider", id, BuildText(SettingLabelKey(id), label), binding, min, max, step, null,
			description == null ? null : BuildText(SettingDescriptionKey(id), description));
	}

	private static object CreateCallbackBinding(Type valueType, string dataKey, Func<object> read, Action<object> write)
	{
		var callbackFactory = ResolveCallbackBindingFactoryMethod();
		if (callbackFactory == null)
			throw new MissingMethodException("STS2RitsuLib.Settings.ModSettingsBindings", "Callback");

		var typedFactory = callbackFactory.MakeGenericMethod(valueType);
		var readDelegate = CreateFuncDelegate(valueType, read);
		var writeDelegate = CreateActionDelegate(valueType, write);
		var scopeType = ResolveType("STS2RitsuLib.Utils.Persistence.SaveScope", RitsuLibAssemblyName)
		                ?? throw new TypeLoadException("SaveScope");
		var globalScope = Enum.Parse(scopeType, "Global");
		return typedFactory.Invoke(null,
			[Entry.ModId, dataKey, readDelegate, writeDelegate, new Action(MoreEnchantSettingsStore.PersistCurrent), globalScope])!;
	}

	private static MethodInfo? ResolveCallbackBindingFactoryMethod()
	{
		if (_callbackBindingFactoryMethod != null)
			return _callbackBindingFactoryMethod;

		var bindingsType = ResolveType("STS2RitsuLib.Settings.ModSettingsBindings", RitsuLibAssemblyName);
		_callbackBindingFactoryMethod = bindingsType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.FirstOrDefault(static m =>
			{
				if (m.Name != "Callback" || !m.IsGenericMethodDefinition)
					return false;
				var ps = m.GetParameters();
				return ps.Length == 6 && ps[0].ParameterType == typeof(string) && ps[1].ParameterType == typeof(string);
			});
		return _callbackBindingFactoryMethod;
	}

	private static Delegate CreateFuncDelegate(Type valueType, Func<object> read)
	{
		var funcType = typeof(Func<>).MakeGenericType(valueType);
		var wrapper = typeof(RitsuLibModSettingsCompat)
			.GetMethod(nameof(CreateFuncDelegateCore), BindingFlags.NonPublic | BindingFlags.Static)!
			.MakeGenericMethod(valueType);
		return (Delegate)wrapper.Invoke(null, [funcType, read])!;
	}

	private static Delegate CreateActionDelegate(Type valueType, Action<object> write)
	{
		var actionType = typeof(Action<>).MakeGenericType(valueType);
		var wrapper = typeof(RitsuLibModSettingsCompat)
			.GetMethod(nameof(CreateActionDelegateCore), BindingFlags.NonPublic | BindingFlags.Static)!
			.MakeGenericMethod(valueType);
		return (Delegate)wrapper.Invoke(null, [actionType, write])!;
	}

	private static Delegate CreateFuncDelegateCore<TValue>(Type funcType, Func<object> read)
	{
		Func<TValue> invoker = () => (TValue)read();
		return Delegate.CreateDelegate(funcType, invoker.Target, invoker.Method);
	}

	private static Delegate CreateActionDelegateCore<TValue>(Type actionType, Action<object> write)
	{
		Action<TValue> invoker = value => write(value!);
		return Delegate.CreateDelegate(actionType, invoker.Target, invoker.Method);
	}

	private static bool IsMultiplayerClient()
	{
		var net = RunManager.Instance?.NetService;
		return net != null && net.IsConnected && net.Type == NetGameType.Client;
	}

	private static object BuildText(string key, string fallback)
	{
		if (_modSettingsTextLocStringMethod == null)
			throw new InvalidOperationException("RitsuLib ModSettingsText.LocString is unavailable.");
		return _modSettingsTextLocStringMethod.Invoke(null, [SettingsLocTable, key, fallback])!;
	}

	private static object BuildLiteralText(string text)
	{
		var literalMethod = _modSettingsTextType?.GetMethod(
			"Literal",
			BindingFlags.Public | BindingFlags.Static,
			null,
			[typeof(string)],
			null);
		if (literalMethod == null)
			return BuildText("ME_SETTINGS.literal", text);
		return literalMethod.Invoke(null, [text])!;
	}

	private static string SettingLabelKey(string id) => $"ME_SETTINGS.{id}.label";

	private static string SettingDescriptionKey(string id) => $"ME_SETTINGS.{id}.description";

	private static Delegate CreateTypedDelegate(Type argType, MethodInfo targetMethod)
	{
		var actionType = typeof(Action<>).MakeGenericType(argType);
		var wrapper = typeof(RitsuLibModSettingsCompat)
			.GetMethod(nameof(CreateTypedDelegateCore), BindingFlags.NonPublic | BindingFlags.Static)!
			.MakeGenericMethod(argType);
		return (Delegate)wrapper.Invoke(null, [actionType, targetMethod])!;
	}

	private static Delegate CreateTypedDelegateCore<TArg>(Type actionType, MethodInfo targetMethod)
	{
		Action<TArg> invoker = arg => targetMethod.Invoke(null, [arg]);
		return Delegate.CreateDelegate(actionType, invoker.Target, invoker.Method);
	}

	private static object InvokeFluent(object target, string methodName, params object?[] args)
	{
		var method = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == args.Length);
		if (method == null)
			throw new MissingMethodException(target.GetType().FullName, methodName);
		return method.Invoke(target, args)!;
	}

	private static Type? ResolveSectionBuilderType(Type pageBuilderType)
	{
		var addSection = pageBuilderType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
			.FirstOrDefault(m => m.Name == "AddSection" && m.GetParameters().Length == 2);
		return addSection?.GetParameters()[1].ParameterType.GetGenericArguments().FirstOrDefault();
	}

	private static Type? ResolveType(string fullTypeName, string assemblyName)
	{
		var assemblyNames = new[] { assemblyName, "STS2-RitsuLib", "STS2RitsuLib" };
		foreach (var candidate in assemblyNames.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			var resolved = Type.GetType($"{fullTypeName}, {candidate}", throwOnError: false)
			               ?? AppDomain.CurrentDomain.GetAssemblies()
				               .FirstOrDefault(a =>
					               string.Equals(a.GetName().Name, candidate, StringComparison.OrdinalIgnoreCase))
				               ?.GetType(fullTypeName);
			if (resolved != null)
				return resolved;
		}

		return null;
	}
}
