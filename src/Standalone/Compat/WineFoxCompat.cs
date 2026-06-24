using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant.Standalone.Compat;

/// <summary>酒狐（STS2_WineFox）联动检测与关键词反射访问（避免硬依赖）。</summary>
internal static class WineFoxCompat
{
	private const string WineFoxAssemblyName = "STS2_WineFox";
	private const string WineFoxKeywordsTypeName = "STS2_WineFox.Cards.WineFoxKeywords";
	private const string WineFoxRootResource = "res://STS2_WineFox/";
	private const string CraftCmdTypeName = "STS2_WineFox.Commands.CraftCmd";

	private static Assembly? TryGetWineFoxAssembly()
	{
		return AppDomain.CurrentDomain.GetAssemblies()
			.FirstOrDefault(static a =>
				string.Equals(a.GetName().Name, WineFoxAssemblyName, StringComparison.OrdinalIgnoreCase));
	}

	private static Type? ResolveType(string fullTypeName)
	{
		var asm = TryGetWineFoxAssembly();
		return asm?.GetType(fullTypeName) ?? Type.GetType($"{fullTypeName}, {WineFoxAssemblyName}");
	}

	internal static bool IsWineFoxModAvailable()
	{
		if (ResourceLoader.Exists(WineFoxRootResource))
			return true;

		return ResolveType(WineFoxKeywordsTypeName) != null
		       || AppDomain.CurrentDomain.GetAssemblies()
			       .Any(static asm =>
			       {
				       var name = asm.GetName().Name;
				       return !string.IsNullOrWhiteSpace(name)
				              && name.Contains("WineFox", StringComparison.OrdinalIgnoreCase);
			       });
	}

	/// <summary>获取酒狐自定义关键词「合成」（<c>WineFoxKeywords.CraftKeyword</c>）。</summary>
	internal static bool TryGetCraftKeyword(out CardKeyword craftKeyword)
	{
		craftKeyword = default;

		var keywordsType = ResolveType(WineFoxKeywordsTypeName);
		if (keywordsType == null)
			return false;

		var field = keywordsType.GetField("CraftKeyword", BindingFlags.Public | BindingFlags.Static);
		if (field?.GetValue(null) is CardKeyword directKeyword)
		{
			craftKeyword = directKeyword;
			return true;
		}

		// 兼容新版 WineFoxKeywords：先取 string 关键词 ID，再通过 RitsuLib 扩展转 CardKeyword。
		var legacyField = keywordsType.GetField("Craft", BindingFlags.Public | BindingFlags.Static);
		if (legacyField?.GetValue(null) is string keywordId
		    && TryResolveModKeyword(keywordId, out var resolved))
		{
			craftKeyword = resolved;
			return true;
		}

		return false;
	}

	internal static bool TryGetCraftIntoHandMethod(out MethodInfo method)
	{
		method = null!;

		var craftCmdType = ResolveType(CraftCmdTypeName);
		if (craftCmdType == null)
			return false;

		method = craftCmdType
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.FirstOrDefault(m =>
			{
				if (m.Name != "CraftIntoHand")
					return false;
				var p = m.GetParameters();
				return p.Length >= 2 && p[1].ParameterType == typeof(CardModel);
			})!;
		return method != null;
	}

	private static bool TryResolveModKeyword(string keywordId, out CardKeyword keyword)
	{
		keyword = default;
		if (string.IsNullOrWhiteSpace(keywordId))
			return false;

		foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
		{
			var asmName = asm.GetName().Name;
			if (string.IsNullOrWhiteSpace(asmName) ||
			    !asmName.Contains("RitsuLib", StringComparison.OrdinalIgnoreCase))
				continue;

			foreach (var type in asm.GetTypes())
			{
				var method = type.GetMethod(
					"GetModCardKeyword",
					BindingFlags.Public | BindingFlags.Static,
					null,
					[typeof(string)],
					null);
				if (method == null || method.ReturnType != typeof(CardKeyword))
					continue;

				if (method.Invoke(null, [keywordId]) is CardKeyword resolved)
				{
					keyword = resolved;
					return true;
				}
			}
		}

		return false;
	}
}
