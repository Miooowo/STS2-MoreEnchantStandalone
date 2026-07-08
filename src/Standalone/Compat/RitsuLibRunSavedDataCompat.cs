using System.Reflection;
using System.Text.Json;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MoreEnchant.scripts;

namespace MoreEnchant.Standalone.Compat;

/// <summary>
/// RitsuLib 软依赖：通过反射接入 RunSavedData，避免对 STS2RitsuLib 的编译时硬依赖。
/// </summary>
internal static class RitsuLibRunSavedDataCompat
{
	private const string RitsuLibAssemblyName = "STS2RitsuLib";
	private const string FrameworkTypeName = "STS2RitsuLib.RitsuLibFramework";
	private const string RunSavedDataOptionsTypeName = "STS2RitsuLib.RunData.RunSavedDataOptions";

	private static readonly object Sync = new();
	private static bool _available;
	private static object? _slot;
	private static MethodInfo? _slotGetMethod;
	private static MethodInfo? _slotTryGetMethod;
	private static MethodInfo? _slotSetMethod;

	internal static bool IsAvailable()
	{
		EnsureInitialized();
		return _available;
	}

	internal static bool TryGetCurrentRunSettings(out MoreEnchantSettings settings)
	{
		settings = null!;
		EnsureInitialized();
		if (!_available || _slot == null)
			return false;

		var runState = ResolveRunState();
		if (runState == null)
			return false;

		try
		{
			if (_slotTryGetMethod != null)
			{
				object?[] args = [runState, null];
				if (_slotTryGetMethod.Invoke(_slot, args) is true && args[1] is MoreEnchantSettings existing)
				{
					settings = Clone(existing);
					return true;
				}
			}

			if (_slotGetMethod?.Invoke(_slot, [runState]) is MoreEnchantSettings created)
			{
				settings = Clone(created);
				return true;
			}
		}
		catch
		{
			// ignore
		}

		return false;
	}

	internal static bool TrySetCurrentRunSettings(MoreEnchantSettings settings)
	{
		ArgumentNullException.ThrowIfNull(settings);
		EnsureInitialized();
		if (!_available || _slotSetMethod == null || _slot == null)
			return false;

		var runState = ResolveRunState();
		if (runState == null || !CanWriteCurrentRunData())
			return false;

		try
		{
			_slotSetMethod.Invoke(_slot, [runState, Clone(settings)]);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool CanWriteCurrentRunData()
	{
		var net = RunManager.Instance?.NetService;
		if (net == null || !net.IsConnected)
			return true;
		return net.Type != NetGameType.Client;
	}

	private static RunState? ResolveRunState()
	{
		var runManager = RunManager.Instance;
		if (runManager == null)
			return null;

		var rmType = runManager.GetType();
		var direct = rmType.GetProperty("RunState", BindingFlags.Public | BindingFlags.Instance)
			?.GetValue(runManager) as RunState;
		if (direct != null)
			return direct;

		var localPlayer = rmType.GetProperty("LocalPlayer", BindingFlags.Public | BindingFlags.Instance)
			?.GetValue(runManager);
		var localRunState = localPlayer?.GetType()
			.GetProperty("RunState", BindingFlags.Public | BindingFlags.Instance)
			?.GetValue(localPlayer) as RunState;
		if (localRunState != null)
			return localRunState;

		var players = rmType.GetProperty("Players", BindingFlags.Public | BindingFlags.Instance)
			?.GetValue(runManager) as System.Collections.IEnumerable;
		if (players == null)
			return null;
		foreach (var player in players)
		{
			var runState = player?.GetType()
				.GetProperty("RunState", BindingFlags.Public | BindingFlags.Instance)
				?.GetValue(player) as RunState;
			if (runState != null)
				return runState;
		}

		return null;
	}

	private static void EnsureInitialized()
	{
		lock (Sync)
		{
			if (_available && _slot != null)
				return;
			try
			{
				var frameworkType = ResolveType(FrameworkTypeName, RitsuLibAssemblyName);
				if (frameworkType == null)
					return;

				var getStore = frameworkType.GetMethod(
					"GetRunSavedDataStore",
					BindingFlags.Public | BindingFlags.Static,
					null,
					[typeof(string)],
					null);
				if (getStore == null)
					return;

				var store = getStore.Invoke(null, [Entry.ModId]);
				if (store == null)
					return;

				var registerMethod = store.GetType()
					.GetMethods(BindingFlags.Public | BindingFlags.Instance)
					.FirstOrDefault(m =>
					{
						if (!m.IsGenericMethodDefinition || m.Name != "Register")
							return false;
						var ps = m.GetParameters();
						return ps.Length == 3 && ps[0].ParameterType == typeof(string);
					});
				if (registerMethod == null)
					return;

				var options = CreateRunSavedDataOptions();
				var defaultFactory = new Func<MoreEnchantSettings>(static () => new());
				var genericRegister = registerMethod.MakeGenericMethod(typeof(MoreEnchantSettings));
				_slot = genericRegister.Invoke(store, [MoreEnchantSettings.StoreKey, defaultFactory, options]);
				if (_slot == null)
					return;

				var slotType = _slot.GetType();
				_slotGetMethod = slotType.GetMethod("Get", BindingFlags.Public | BindingFlags.Instance, null, [typeof(RunState)], null);
				_slotSetMethod = slotType.GetMethod(
					"Set",
					BindingFlags.Public | BindingFlags.Instance,
					null,
					[typeof(RunState), typeof(MoreEnchantSettings)],
					null);
				_slotTryGetMethod = slotType.GetMethod(
					"TryGet",
					BindingFlags.Public | BindingFlags.Instance,
					null,
					[typeof(RunState), typeof(MoreEnchantSettings).MakeByRefType()],
					null);

				_available = _slotGetMethod != null && _slotSetMethod != null;
			}
			catch
			{
				_available = false;
				_slot = null;
			}
		}
	}

	private static object? CreateRunSavedDataOptions()
	{
		var optionsType = ResolveType(RunSavedDataOptionsTypeName, RitsuLibAssemblyName);
		if (optionsType == null)
			return null;

		var options = Activator.CreateInstance(optionsType);
		if (options == null)
			return null;

		optionsType.GetProperty("SchemaVersion", BindingFlags.Public | BindingFlags.Instance)
			?.SetValue(options, 6);
		optionsType.GetProperty("SyncLobbyOnChange", BindingFlags.Public | BindingFlags.Instance)
			?.SetValue(options, true);
		return options;
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

	private static MoreEnchantSettings Clone(MoreEnchantSettings source)
	{
		var json = JsonSerializer.Serialize(source);
		return JsonSerializer.Deserialize<MoreEnchantSettings>(json) ?? new MoreEnchantSettings();
	}
}
