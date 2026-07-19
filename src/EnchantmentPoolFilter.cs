using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant;

/// <summary>
/// 随机附魔池白名单 / 黑名单过滤：白名单按 ID 或中英标题匹配；黑名单按 <c>Id.Entry</c> 剔除。
/// </summary>
internal static class EnchantmentPoolFilter
{
	private static readonly Regex MarkupStrip = new(@"\[[^\]]*\]", RegexOptions.Compiled);
	private static readonly ConcurrentDictionary<string, Dictionary<string, string>> LocTitleCache = new(StringComparer.OrdinalIgnoreCase);
	private static readonly char[] TokenSeparators = [',', '，', ';', '；', '\n', '\r', '\t'];

	internal static bool IsAllowed(EnchantmentModel template, MoreEnchantSettings settings)
	{
		var id = template.Id.Entry;
		if (IsBlacklisted(id, settings))
			return false;

		var tokens = ParseTokens(settings.RewardEnchantOnlyFilter);
		if (tokens.Count == 0)
			return true;

		return MatchesWhitelist(template, tokens);
	}

	internal static bool IsBlacklisted(string idEntry, MoreEnchantSettings settings)
	{
		var list = settings.BlacklistedEnchantmentIds;
		if (list == null || list.Count == 0)
			return false;

		foreach (var banned in list)
		{
			if (string.Equals(banned, idEntry, StringComparison.OrdinalIgnoreCase))
				return true;
		}

		return false;
	}

	internal static void SetBlacklisted(string idEntry, bool blacklisted, MoreEnchantSettings settings)
	{
		settings.BlacklistedEnchantmentIds ??= [];
		var normalized = idEntry.Trim().ToUpperInvariant();
		if (blacklisted)
		{
			if (!settings.BlacklistedEnchantmentIds.Any(id =>
				    string.Equals(id, normalized, StringComparison.OrdinalIgnoreCase)))
				settings.BlacklistedEnchantmentIds.Add(normalized);
		}
		else
		{
			settings.BlacklistedEnchantmentIds.RemoveAll(id =>
				string.Equals(id, normalized, StringComparison.OrdinalIgnoreCase));
		}
	}

	internal static List<string> ParseTokens(string? filterText)
	{
		if (string.IsNullOrWhiteSpace(filterText))
			return [];

		return filterText
			.Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Where(static t => t.Length > 0)
			.ToList();
	}

	private static bool MatchesWhitelist(EnchantmentModel template, List<string> tokens)
	{
		var keys = CollectMatchKeys(template);
		foreach (var token in tokens)
		{
			var needle = NormalizeForMatch(token);
			if (needle.Length == 0)
				continue;
			foreach (var key in keys)
			{
				if (string.Equals(key, needle, StringComparison.OrdinalIgnoreCase))
					return true;
			}
		}

		return false;
	}

	private static List<string> CollectMatchKeys(EnchantmentModel template)
	{
		var keys = new List<string>(8) { template.Id.Entry };

		try
		{
			var title = template.Title.GetFormattedText();
			var plain = NormalizeForMatch(title);
			if (plain.Length > 0)
				keys.Add(plain);
		}
		catch
		{
			// Title 在极早初始化阶段可能不可用。
		}

		AddLocTitles(keys, template.Id.Entry, "eng");
		AddLocTitles(keys, template.Id.Entry, "zhs");
		return keys;
	}

	private static void AddLocTitles(List<string> keys, string idEntry, string lang)
	{
		var map = GetLocTitleMap(lang);
		if (map.TryGetValue($"{idEntry}.title", out var title) ||
		    map.TryGetValue($"{idEntry.ToUpperInvariant()}.title", out title))
		{
			var plain = NormalizeForMatch(title);
			if (plain.Length > 0)
				keys.Add(plain);
		}
	}

	private static Dictionary<string, string> GetLocTitleMap(string lang) =>
		LocTitleCache.GetOrAdd(lang, static language =>
		{
			var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			try
			{
				var path = $"res://MoreEnchantStandalone/localization/{language}/enchantments.json";
				if (!Godot.FileAccess.FileExists(path))
					return result;

				using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
				if (file == null)
					return result;

				var json = file.GetAsText();
				var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
				if (dict == null)
					return result;

				foreach (var (key, value) in dict)
				{
					if (key.EndsWith(".title", StringComparison.OrdinalIgnoreCase))
						result[key] = value;
				}
			}
			catch
			{
				// 忽略本地化加载失败，仍可用 Id / 当前 Title。
			}

			return result;
		});

	private static string NormalizeForMatch(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return "";
		var stripped = MarkupStrip.Replace(text, "");
		return stripped.Trim();
	}
}
