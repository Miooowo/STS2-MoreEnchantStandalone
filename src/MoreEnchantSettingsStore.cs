using System.Reflection;
using System.Text.Json;

namespace MoreEnchant;

/// <summary>独立版：从模组目录下的 JSON 读取设置（无游戏内设置页时可直接编辑文件）。</summary>
internal static class MoreEnchantSettingsStore
{
	private static readonly object Sync = new();
	private static string? _cachedPath;
	private static MoreEnchantSettings? _cached;

	internal static MoreEnchantSettings Get()
	{
		lock (Sync)
		{
			var path = ResolvePath();
			if (_cached != null && _cachedPath == path)
				return _cached;

			_cachedPath = path;
			if (File.Exists(path))
			{
				try
				{
					var json = File.ReadAllText(path);
					_cached = JsonSerializer.Deserialize<MoreEnchantSettings>(json) ?? new MoreEnchantSettings();
				}
				catch
				{
					_cached = new MoreEnchantSettings();
				}

				return _cached;
			}

			_cached = new MoreEnchantSettings();
			var dir = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(dir))
				Directory.CreateDirectory(dir);
			TryWriteDefaults(path, _cached);
			return _cached;
		}
	}

	/// <summary>将当前缓存（与 <see cref="Get"/> 为同一实例）写回磁盘。</summary>
	internal static void PersistCurrent()
	{
		lock (Sync)
		{
			var settings = _cached ?? new MoreEnchantSettings();
			_cached = settings;
			_cachedPath = ResolvePath();
			var path = _cachedPath;
			var dir = Path.GetDirectoryName(path);
			if (!string.IsNullOrEmpty(dir))
				Directory.CreateDirectory(dir);

			try
			{
				File.WriteAllText(
					path,
					JsonSerializer.Serialize(
						settings,
						new JsonSerializerOptions { WriteIndented = true }));
			}
			catch
			{
				// ignore
			}
		}
	}

	private static void TryWriteDefaults(string path, MoreEnchantSettings settings)
	{
		try
		{
			File.WriteAllText(
				path,
				JsonSerializer.Serialize(
					settings,
					new JsonSerializerOptions { WriteIndented = true }));
		}
		catch
		{
			// ignore
		}
	}

	private static string ResolvePath()
	{
		// 勿放在 mods\... 根目录：游戏会递归把 *.json 当 mod manifest 扫描，缺 id 会刷 ERROR。
		var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		return Path.Combine(roaming, "SlayTheSpire2", "MoreEnchantStandalone", "more_enchant_settings.json");
	}
}
