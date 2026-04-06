using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Standalone.Compat;

/// <summary>
/// 对 MultiEnchantmentMod 的可选兼容层：不在编译期引用其程序集。
/// 若玩家安装了 MultiEnchantmentMod，则启用部分附魔的叠层语义与 snapshot 读取。
/// </summary>
public static class MultiEnchantmentCompat
{
	private const string AssemblyName = "MultiEnchantmentMod";
	private const string ApiTypeName = "MultiEnchantmentMod.MultiEnchantmentStackApi";

	/// <summary>与 <see cref="SnakebiteEnchantment"/> 的每层基础毒量一致（避免 compat 反向引用该 private const）。</summary>
	private const int SnakebitePoisonPerLayer = 7;

	private static readonly object SnakebiteRegisterLock = new();
	private static bool _snakebiteStackProvidersRegistered;

	public static bool IsAvailable()
	{
		return FindApiType() != null;
	}

	public static void TryEnableForSnakebite(Type snakebiteEnchantmentType)
	{
		lock (SnakebiteRegisterLock)
		{
			if (_snakebiteStackProvidersRegistered)
				return;

			try
			{
				var apiType = FindApiType();
				if (apiType == null)
					return;

				// 通过反射动态实现并注册：
				// - IEnchantmentStackDefinitionProvider<SnakebiteEnchantment> => MergeAmount + Shared
				// - IEnchantmentExecutionPolicyProvider<SnakebiteEnchantment> => OnPlay: FirstActiveInstanceOnly
				object? definitionProvider = BuildSnakebiteDefinitionProvider(snakebiteEnchantmentType);
				if (definitionProvider != null)
					InvokeGeneric(apiType, "RegisterDefinitionProvider", snakebiteEnchantmentType, definitionProvider);

				object? policyProvider = BuildSnakebiteExecutionPolicyProvider(snakebiteEnchantmentType);
				if (policyProvider != null)
					InvokeGeneric(apiType, "RegisterExecutionPolicyProvider", snakebiteEnchantmentType, policyProvider);

				// MergeAmount 下由合并刷新回调统一走蛇咬自己的 RecalculateValues（同步文案/hover）。
				object? mergedProvider = BuildSnakebiteMergedStateProvider(snakebiteEnchantmentType);
				if (mergedProvider != null)
					InvokeGeneric(apiType, "RegisterMergedStateProvider", snakebiteEnchantmentType, mergedProvider);

				// 叠层时合并 UI：单一视觉切片（一枚角标）+ 用 snapshot 总层数重写文案，避免两条紫色 extra 与附魔 tab 仍显示 7。
				object? presentationProvider = BuildSnakebitePresentationProvider(snakebiteEnchantmentType);
				if (presentationProvider != null)
					InvokeGeneric(apiType, "RegisterPresentationProvider", snakebiteEnchantmentType, presentationProvider);

				// 若定义未真正生效（例如加载顺序/代理失败），不要标记完成，以便下次再试。
				if (VerifySnakebiteUsesMergeAmountBehavior(snakebiteEnchantmentType))
					_snakebiteStackProvidersRegistered = true;
			}
			catch
			{
				// 兼容层不应阻止模组正常加载：忽略所有异常。
			}
		}
	}

	/// <summary>
	/// MultiEnchantment 的 <c>RunAdditionalEnchantmentsOnPlay</c> 会对每张“额外附魔实例”各调一次 <c>OnPlay</c>。
	/// 若合并未生效导致同卡存在多个蛇咬实例，这里只让顺序上的第一个实例结算，其余跳过，避免多次上毒。
	/// </summary>
	public static bool IsCanonicalSnakebiteForOnPlay(SnakebiteEnchantment self)
	{
		if (self.Card == null)
			return true;

		var list = ListSnakebiteInstancesOnCard(self.Card);
		if (list.Count == 0)
			return true;

		return ReferenceEquals(list[0], self);
	}

	/// <summary>
	/// 总层数：多实例时累加各实例 <see cref="EnchantmentModel.Amount"/>；合并单实例时与 snapshot 一致。
	/// 取 <c>max(ΣAmount, snapshot.TotalAmount)</c> 以避免「Amount 尚未写回但 snapshot 已更新」或反射枚举失败只读到 1。
	/// </summary>
	/// <param name="context">
	/// 打出/重算时的当前蛇咬实例；当 MEM 列表枚举失败时，用于回退 <see cref="TryGetSnapshotTotalAmount"/> 与 <see cref="EnchantmentModel.Amount"/>。
	/// </param>
	public static int GetTotalSnakebiteLayersOnCard(CardModel card, SnakebiteEnchantment? context = null)
	{
		var list = ListSnakebiteInstancesOnCard(card);
		if (list.Count == 0)
		{
			if (context != null)
			{
				int fs = TryGetSnapshotTotalAmount(context);
				int am = Math.Max(1, context.Amount);
				return Math.Max(1, Math.Max(fs, am));
			}

			return 1;
		}

		int sumAmount = 0;
		foreach (var sb in list)
			sumAmount += Math.Max(1, sb.Amount);

		int snap = TryGetSnapshotTotalAmount(list[0]);
		int fromContext = 0;
		if (context != null && !list.Exists(x => ReferenceEquals(x, context)))
			fromContext = Math.Max(TryGetSnapshotTotalAmount(context), Math.Max(1, context.Amount));

		return Math.Max(1, Math.Max(Math.Max(sumAmount, snap), fromContext));
	}

	/// <summary>
	/// 将叠加后的毒量写回卡上每一处蛇咬实例的附魔动态变量，并刷新卡牌动态变量（description / extraCardText / hover）。
	/// </summary>
	public static void RefreshAllSnakebitePoisonDisplaysOnCard(CardModel card, decimal poisonPerLayer, SnakebiteEnchantment? context = null)
	{
		decimal total = poisonPerLayer * GetTotalSnakebiteLayersOnCard(card, context);
		foreach (var sb in ListSnakebiteInstancesOnCard(card))
			TryApplyPoisonPowerDisplay(sb, total);

		card.DynamicVars.RecalculateForUpgradeOrEnchant();
	}

	/// <summary>
	/// 同步蛇咬在卡面/hover 上显示的毒量。STS2 的 <c>{PoisonPower:diff()}</c> 往往走“附魔动态值”通道，
	/// 仅改 <see cref="MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar.BaseValue"/> 不够，需要配合
	/// <c>MultiEnchantmentSupport.SetEnchantedValue</c>。
	/// </summary>
	public static void TryApplyPoisonPowerDisplay(SnakebiteEnchantment enchantment, decimal totalPoison)
	{
		TrySetPoisonDynamicVarBaseValue(enchantment, totalPoison);
		TrySetPoisonEnchantedValue(enchantment, totalPoison);
	}

	private static List<SnakebiteEnchantment> ListSnakebiteInstancesOnCard(CardModel card)
	{
		var result = new List<SnakebiteEnchantment>();

		void AddUnique(SnakebiteEnchantment sb)
		{
			foreach (var x in result)
			{
				if (ReferenceEquals(x, sb))
					return;
			}

			result.Add(sb);
		}

		// 与 MultiEnchantment 打出顺序一致：主附魔（若有）先于额外附魔。
		if (card.Enchantment is SnakebiteEnchantment primary)
			AddUnique(primary);

		var supportType = Type.GetType("MultiEnchantmentMod.MultiEnchantmentSupport, MultiEnchantmentMod", throwOnError: false);
		foreach (var e in InvokeMemStaticEnumerable(supportType, "GetAdditionalEnchantments", card))
		{
			if (IsSnakebiteEnchantment(e))
				AddUnique((SnakebiteEnchantment)e!);
		}

		if (result.Count > 0)
			return result;

		foreach (var e in InvokeMemStaticEnumerable(supportType, "GetEnchantments", card))
		{
			if (IsSnakebiteEnchantment(e))
				AddUnique((SnakebiteEnchantment)e!);
		}

		if (card.Enchantment is SnakebiteEnchantment p2)
			AddUnique(p2);

		return result;
	}

	private static bool IsSnakebiteEnchantment(object? e) =>
		e != null && typeof(SnakebiteEnchantment).IsAssignableFrom(e.GetType());

	private static int ReadSnapshotLayerTotal(object? snapshot)
	{
		if (snapshot == null)
			return 1;

		try
		{
			foreach (var propName in new[] { "ActiveTotalAmount", "TotalAmount" })
			{
				var prop = snapshot.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
				if (prop?.PropertyType == typeof(int))
				{
					int v = (int)prop.GetValue(snapshot)!;
					if (v > 0)
						return v;
				}
			}
		}
		catch
		{
			// ignore
		}

		return 1;
	}

	/// <summary>
	/// MEM 的 Presentation 钩子可能传入 snapshot，也可能直接传入附魔实例；前者用 snapshot 字段，后者必须用卡面总层数逻辑。
	/// </summary>
	private static int ResolveSnakebiteLayersForMemPresentation(object? arg0)
	{
		if (arg0 is SnakebiteEnchantment sb)
		{
			if (sb.Card != null)
				return GetTotalSnakebiteLayersOnCard(sb.Card, sb);

			return Math.Max(1, TryGetSnapshotTotalAmount(sb));
		}

		return Math.Max(1, ReadSnapshotLayerTotal(arg0));
	}

	private static bool TryFindTryFormatExtraCardTextArgs(
		MethodInfo method,
		object?[]? args,
		out object? layerSubject,
		out string defaultText,
		out int formattedOutIndex)
	{
		layerSubject = null;
		defaultText = "";
		formattedOutIndex = -1;

		if (args == null || args.Length == 0)
			return false;

		var ps = method.GetParameters();
		for (int i = 0; i < ps.Length && i < args.Length; i++)
		{
			if (args[i] is SnakebiteEnchantment sb)
				layerSubject = sb;

			var p = ps[i];
			var elem = p.ParameterType.IsByRef ? p.ParameterType.GetElementType() : p.ParameterType;
			if (elem == typeof(string))
			{
				if (p.IsOut)
					formattedOutIndex = i;
				else if (args[i] is string s && string.IsNullOrEmpty(defaultText))
					defaultText = s;
			}
		}

		if (layerSubject == null)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (args[i] != null && typeof(SnakebiteEnchantment).IsAssignableFrom(args[i]!.GetType()))
				{
					layerSubject = args[i];
					break;
				}
			}
		}

		// 仅有 snapshot、无附魔实例时仍应用 ReadSnapshotLayerTotal。
		if (layerSubject == null && args.Length > 0)
			layerSubject = args[0];

		return !string.IsNullOrEmpty(defaultText) && formattedOutIndex >= 0;
	}

	/// <summary>
	/// 将已格式化的附魔/描述字符串里的「毒层数」替换为叠加后的总量（中文 / 英文 / 常见 BBCode）。
	/// </summary>
	private static string FormatSnakebiteStackedPoisonText(string defaultText, int poisonTotal)
	{
		if (string.IsNullOrEmpty(defaultText))
			return defaultText;

		var t = defaultText;

		// 中文：数字 + 层 + （同段内）毒，例如「施加7层中毒」「打出时对目标施加7层中毒」。
		t = Regex.Replace(t, @"\d+(?=层[^\n。]*毒)", poisonTotal.ToString());

		// 英文：Apply N … Poison（保留 captured 前缀大小写等）。
		t = Regex.Replace(t, @"(?i)(Apply )(\d+)(?=[^\n]*Poison)", m => $"{m.Groups[1].Value}{poisonTotal}");

		// [blue]7[/blue] 层……毒
		t = Regex.Replace(t, @"\[blue\]\d+\[/blue\](?=层[^\n]*毒)", $"[blue]{poisonTotal}[/blue]");

		return t;
	}

	private static IEnumerable<object?> InvokeMemStaticEnumerable(Type? supportType, string methodName, CardModel card)
	{
		if (supportType == null)
			yield break;

		foreach (var method in supportType.GetMethods(BindingFlags.Public | BindingFlags.Static)
			         .Where(m => m.Name == methodName && m.GetParameters().Length == 1))
		{
			var p0 = method.GetParameters()[0].ParameterType;
			if (!p0.IsInstanceOfType(card))
				continue;

			object? ret;
			try
			{
				ret = method.Invoke(null, new object[] { card });
			}
			catch
			{
				continue;
			}

			if (ret is System.Collections.IEnumerable ens)
			{
				foreach (object? item in ens)
					yield return item;

				yield break;
			}
		}
	}

	/// <summary>读取多重附魔 snapshot 的总层数；失败返回 0。</summary>
	private static int TryGetSnapshotTotalAmount(SnakebiteEnchantment enchantment)
	{
		try
		{
			var apiType = FindApiType();
			if (apiType == null)
				return 0;

			TryEnableForSnakebite(enchantment.GetType());

			var getSnapshot = apiType.GetMethod("GetSnapshot", BindingFlags.Public | BindingFlags.Static);
			if (getSnapshot == null)
				return 0;

			object? snapshot = getSnapshot.Invoke(null, new object[] { enchantment });
			if (snapshot == null)
				return 0;

			foreach (var propName in new[] { "ActiveTotalAmount", "TotalAmount" })
			{
				var prop = snapshot.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
				if (prop?.PropertyType == typeof(int))
				{
					int v = (int)prop.GetValue(snapshot)!;
					if (v > 0)
						return v;
				}
			}
		}
		catch
		{
			// ignore
		}

		return 0;
	}

	private static bool VerifySnakebiteUsesMergeAmountBehavior(Type snakebiteEnchantmentType)
	{
		try
		{
			var stackSupport =
				Type.GetType("MultiEnchantmentMod.MultiEnchantmentStackSupport, MultiEnchantmentMod", throwOnError: false);
			var getBehavior = stackSupport?.GetMethod("GetBehavior", BindingFlags.Public | BindingFlags.Static);
			object? behavior = getBehavior?.Invoke(null, new object[] { snakebiteEnchantmentType });
			return string.Equals(behavior?.ToString(), "MergeAmount", StringComparison.Ordinal);
		}
		catch
		{
			return false;
		}
	}

	private static void TrySetPoisonEnchantedValue(SnakebiteEnchantment enchantment, decimal value)
	{
		try
		{
			var poisonVar = TryGetPoisonDynamicVar(enchantment);
			if (poisonVar == null)
				return;

			var supportType = Type.GetType("MultiEnchantmentMod.MultiEnchantmentSupport, MultiEnchantmentMod", throwOnError: false);
			MethodInfo? setEnchanted = supportType?.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.FirstOrDefault(m =>
					m.Name == "SetEnchantedValue" &&
					m.GetParameters().Length == 2 &&
					m.GetParameters()[1].ParameterType == typeof(decimal));
			setEnchanted?.Invoke(null, new object[] { poisonVar, value });
		}
		catch
		{
			// ignore
		}
	}

	private static object? TryGetPoisonDynamicVar(SnakebiteEnchantment enchantment)
	{
		try
		{
			var dynVarsProp = enchantment.GetType().GetProperty("DynamicVars", BindingFlags.Public | BindingFlags.Instance);
			object? dynVars = dynVarsProp?.GetValue(enchantment);
			if (dynVars == null)
				return null;

			foreach (var key in new[] { "PoisonPower", "Poison" })
			{
				if (dynVars is System.Collections.IDictionary dict && dict.Contains(key))
				{
					var v = dict[key];
					if (v != null)
						return v;
				}

				var tryGet = dynVars.GetType().GetMethod(
					"TryGetValue",
					BindingFlags.Public | BindingFlags.Instance,
					binder: null,
					types: new[] { typeof(string), typeof(DynamicVar).MakeByRefType() },
					modifiers: null);
				if (tryGet != null)
				{
					var args = new object?[] { key, null };
					if ((bool)tryGet.Invoke(dynVars, args)! && args[1] is DynamicVar)
						return args[1];
				}
			}

			var poisonProp = dynVars.GetType().GetProperty("Poison", BindingFlags.Public | BindingFlags.Instance)
			               ?? dynVars.GetType().GetProperty("PoisonPower", BindingFlags.Public | BindingFlags.Instance);
			return poisonProp?.GetValue(dynVars);
		}
		catch
		{
			return null;
		}
	}

	public static int GetActiveTotalAmountOrDefault(object enchantmentInstance, int defaultAmount)
	{
		try
		{
			var apiType = FindApiType();
			if (apiType == null)
				return defaultAmount;

			// 可能在我们的 Entry.Init 调用时 MultiEnchantmentMod 尚未加载；在首次读取 snapshot 时再尝试启用一次。
			TryEnableForSnakebite(enchantmentInstance.GetType());

			var getSnapshot = apiType.GetMethod("GetSnapshot", BindingFlags.Public | BindingFlags.Static);
			if (getSnapshot == null)
				return defaultAmount;

			object? snapshot = getSnapshot.Invoke(null, new[] { enchantmentInstance });
			if (snapshot == null)
				return defaultAmount;

			var prop = snapshot.GetType().GetProperty("ActiveTotalAmount", BindingFlags.Public | BindingFlags.Instance);
			if (prop?.PropertyType == typeof(int))
			{
				int amount = (int)prop.GetValue(snapshot)!;
				return amount > 0 ? amount : defaultAmount;
			}

			// 兜底：没有 ActiveTotalAmount 时使用 TotalAmount
			prop = snapshot.GetType().GetProperty("TotalAmount", BindingFlags.Public | BindingFlags.Instance);
			if (prop?.PropertyType == typeof(int))
			{
				int amount = (int)prop.GetValue(snapshot)!;
				return amount > 0 ? amount : defaultAmount;
			}
		}
		catch
		{
			// ignore
		}

		return defaultAmount;
	}

	private static Type? FindApiType()
	{
		// 先从已加载程序集找（更快），找不到再尝试 Type.GetType 触发加载。
		var loaded = AppDomain.CurrentDomain
			.GetAssemblies()
			.FirstOrDefault(a => string.Equals(a.GetName().Name, AssemblyName, StringComparison.OrdinalIgnoreCase));

		return loaded?.GetType(ApiTypeName, throwOnError: false)
		       ?? Type.GetType($"{ApiTypeName}, {AssemblyName}", throwOnError: false);
	}

	private static void InvokeGeneric(Type apiType, string methodName, Type enchantmentType, object provider)
	{
		var method = apiType.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.FirstOrDefault(m => m.Name == methodName && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 1);
		if (method == null)
			return;

		var closed = method.MakeGenericMethod(enchantmentType);
		closed.Invoke(null, new[] { provider });
	}

	private static object? BuildSnakebiteDefinitionProvider(Type snakebiteType)
	{
		Type? ifaceOpen = Type.GetType("MultiEnchantmentMod.IEnchantmentStackDefinitionProvider`1, MultiEnchantmentMod", throwOnError: false);
		if (ifaceOpen == null)
			return null;

		Type iface = ifaceOpen.MakeGenericType(snakebiteType);

		Type? defType = Type.GetType("MultiEnchantmentMod.EnchantmentStackDefinition, MultiEnchantmentMod", throwOnError: false);
		Type? behaviorEnum = Type.GetType("MultiEnchantmentMod.EnchantmentStackBehavior, MultiEnchantmentMod", throwOnError: false);
		Type? statusEnum = Type.GetType("MultiEnchantmentMod.EnchantmentStatusAggregation, MultiEnchantmentMod", throwOnError: false);
		if (defType == null || behaviorEnum == null || statusEnum == null)
			return null;

		object mergeAmount = Enum.Parse(behaviorEnum, "MergeAmount");
		object shared = Enum.Parse(statusEnum, "Shared");
		object definition = Activator.CreateInstance(defType, mergeAmount, shared)!;

		return RuntimeProviderFactory.CreateProvider(
			iface,
			priority: 100,
			methodName: "GetDefinition",
			methodReturn: definition);
	}

	private static object? BuildSnakebitePresentationProvider(Type snakebiteType)
	{
		Type? ifaceOpen =
			Type.GetType("MultiEnchantmentMod.IEnchantmentPresentationProvider`1, MultiEnchantmentMod", throwOnError: false);
		if (ifaceOpen == null)
			return null;

		Type iface = ifaceOpen.MakeGenericType(snakebiteType);

		// MEM 不同版本可能调用不同方法名；需全部挂钩，否则角标/附魔 Tab 说明不会合并层数。
		object? ResolveVisualSliceAmountsCore(MethodInfo? _, object?[]? args)
		{
			if (args == null || args.Length == 0)
				return null;

			object? subject = args.FirstOrDefault(a => a is SnakebiteEnchantment) ?? args[0];
			if (subject == null)
				return null;

			int total = ResolveSnakebiteLayersForMemPresentation(subject);
			if (total <= 1)
				return null;

			return new[] { total };
		}

		object? TryFormatSnakebitePresentationCore(MethodInfo method, object?[]? args)
		{
			if (!TryFindTryFormatExtraCardTextArgs(method, args, out var subject, out var defaultText, out var outIdx))
				return false;

			int layers = ResolveSnakebiteLayersForMemPresentation(subject);
			int poisonTotal = SnakebitePoisonPerLayer * Math.Max(1, layers);
			string formatted = FormatSnakebiteStackedPoisonText(defaultText, poisonTotal);
			args![outIdx] = formatted;
			return true;
		}

		return RuntimeProviderFactory.CreateProviderWithHandlers(
			iface,
			priority: 100,
			handlers: new (string MethodName, Func<MethodInfo, object?[]?, object?> Handler)[]
			{
				("GetVisualSliceAmounts", ResolveVisualSliceAmountsCore),
				("ResolveVisualSliceAmounts", ResolveVisualSliceAmountsCore),
				("TryFormatExtraCardText", TryFormatSnakebitePresentationCore),
				("TryGetFormattedExtraCardTextForDescription", TryFormatSnakebitePresentationCore),
			});
	}

	private static object? BuildSnakebiteExecutionPolicyProvider(Type snakebiteType)
	{
		Type? ifaceOpen = Type.GetType("MultiEnchantmentMod.IEnchantmentExecutionPolicyProvider`1, MultiEnchantmentMod", throwOnError: false);
		if (ifaceOpen == null)
			return null;

		Type iface = ifaceOpen.MakeGenericType(snakebiteType);

		Type? policyType = Type.GetType("MultiEnchantmentMod.EnchantmentExecutionPolicy, MultiEnchantmentMod", throwOnError: false);
		Type? hookModeEnum = Type.GetType("MultiEnchantmentMod.HookExecutionMode, MultiEnchantmentMod", throwOnError: false);
		if (policyType == null || hookModeEnum == null)
			return null;

		object @default = Enum.Parse(hookModeEnum, "Default");
		object firstOnly = Enum.Parse(hookModeEnum, "FirstActiveInstanceOnly");

		// ctor: (DefaultMode, OnEnchant, OnPlay, AfterCardPlayed, AfterCardDrawn, AfterPlayerTurnStart, BeforeFlush)
		object policy = Activator.CreateInstance(
			policyType,
			@default, // DefaultMode
			@default, // OnEnchant
			firstOnly, // OnPlay
			@default, // AfterCardPlayed
			@default, // AfterCardDrawn
			@default, // AfterPlayerTurnStart
			@default // BeforeFlush
		)!;

		return RuntimeProviderFactory.CreateProvider(
			iface,
			priority: 100,
			methodName: "GetExecutionPolicy",
			methodReturn: policy);
	}

	private static object? BuildSnakebiteMergedStateProvider(Type snakebiteType)
	{
		Type? ifaceOpen = Type.GetType("MultiEnchantmentMod.IEnchantmentMergedStateProvider`1, MultiEnchantmentMod", throwOnError: false);
		if (ifaceOpen == null)
			return null;

		Type iface = ifaceOpen.MakeGenericType(snakebiteType);

		// ApplyMergedAmountDelta(T enchantment, int addedAmount) => no-op
		// RefreshMergedState(T enchantment) => 走蛇咬自己的 RecalculateValues（统一层数/hover/附魔动态值）
		return RuntimeProviderFactory.CreateProviderWithHandlers(
			iface,
			priority: 100,
			handlers: new (string MethodName, Func<MethodInfo, object?[]?, object?> Handler)[]
			{
				("ApplyMergedAmountDelta", static (_, __) => null),
				("RefreshMergedState", static (_, args) =>
				{
					if (args == null || args.Length == 0 || args[0] is not SnakebiteEnchantment sb)
						return null;

					sb.RecalculateValues();
					return null;
				}),
			});
	}

	private static void TrySetPoisonDynamicVarBaseValue(object enchantment, decimal newBaseValue)
	{
		try
		{
			if (enchantment is not SnakebiteEnchantment sb)
				return;

			object? poisonVar = TryGetPoisonDynamicVar(sb);
			if (poisonVar == null)
				return;

			// 1) 直接写 BaseValue（若有 setter）
			var baseValueProp = poisonVar.GetType().GetProperty("BaseValue", BindingFlags.Public | BindingFlags.Instance);
			if (baseValueProp?.CanWrite == true && baseValueProp.PropertyType == typeof(decimal))
			{
				baseValueProp.SetValue(poisonVar, newBaseValue);
				return;
			}

			// 2) 常见方法：SetCustomBaseValue(decimal)
			var setMethod = poisonVar.GetType().GetMethod("SetCustomBaseValue", BindingFlags.Public | BindingFlags.Instance);
			if (setMethod != null)
			{
				var ps = setMethod.GetParameters();
				if (ps.Length == 1 && ps[0].ParameterType == typeof(decimal))
				{
					setMethod.Invoke(poisonVar, new object[] { newBaseValue });
				}
			}
		}
		catch
		{
			// ignore
		}
	}

	private static class RuntimeProviderFactory
	{
		public static object? CreateProvider(Type iface, int priority, string methodName, object methodReturn)
		{
			try
			{
				// 使用 DispatchProxy 动态实现接口（无需额外程序集引用/emit）。
				Type proxyOpen = typeof(SimpleDispatchProxy<>);
				Type proxyType = proxyOpen.MakeGenericType(iface);
				object proxy = (object)Activator.CreateInstance(proxyType)!;

				var init = proxyType.GetMethod("Init", BindingFlags.Public | BindingFlags.Instance);
				init?.Invoke(proxy, new object[] { priority, methodName, methodReturn });
				return proxy;
			}
			catch
			{
				return null;
			}
		}

		public static object? CreateProviderWithHandlers(
			Type iface,
			int priority,
			(string MethodName, Func<MethodInfo, object?[]?, object?> Handler)[] handlers)
		{
			try
			{
				Type proxyOpen = typeof(HandlerDispatchProxy<>);
				Type proxyType = proxyOpen.MakeGenericType(iface);
				object proxy = (object)Activator.CreateInstance(proxyType)!;

				var init = proxyType.GetMethod("Init", BindingFlags.Public | BindingFlags.Instance);
				init?.Invoke(proxy, new object[] { priority, handlers });
				return proxy;
			}
			catch
			{
				return null;
			}
		}

		private sealed class SimpleDispatchProxy<T> : DispatchProxy where T : class
		{
			private int _priority;
			private string _methodName = "";
			private object? _methodReturn;

			public void Init(int priority, string methodName, object methodReturn)
			{
				_priority = priority;
				_methodName = methodName;
				_methodReturn = methodReturn;
			}

			protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
			{
				if (targetMethod == null)
					return null;

				if (targetMethod.Name == "get_Priority")
					return _priority;

				if (targetMethod.Name == _methodName)
					return _methodReturn;

				// 未预期方法：返回默认值，避免抛异常影响加载。
				if (targetMethod.ReturnType == typeof(void))
					return null;

				return targetMethod.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
			}
		}

		private sealed class HandlerDispatchProxy<T> : DispatchProxy where T : class
		{
			private int _priority;
			private (string MethodName, Func<MethodInfo, object?[]?, object?> Handler)[] _handlers =
				Array.Empty<(string, Func<MethodInfo, object?[]?, object?>)>();

			public void Init(int priority, (string MethodName, Func<MethodInfo, object?[]?, object?> Handler)[] handlers)
			{
				_priority = priority;
				_handlers = handlers ?? Array.Empty<(string, Func<MethodInfo, object?[]?, object?>)>();
			}

			protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
			{
				if (targetMethod == null)
					return null;

				if (targetMethod.Name == "get_Priority")
					return _priority;

				foreach (var (methodName, handler) in _handlers)
				{
					if (targetMethod.Name == methodName)
						return handler(targetMethod, args);
				}

				if (targetMethod.ReturnType == typeof(void))
					return null;

				return targetMethod.ReturnType.IsValueType ? Activator.CreateInstance(targetMethod.ReturnType) : null;
			}
		}
	}
}

