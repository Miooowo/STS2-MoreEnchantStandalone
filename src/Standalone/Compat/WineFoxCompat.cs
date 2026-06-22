using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace MoreEnchant.Standalone.Compat;

/// <summary>酒狐（STS2_WineFox）联动检测与关键词反射访问（避免硬依赖）。</summary>
internal static class WineFoxCompat
{
	private const string WineFoxAssemblyName = "STS2_WineFox";
	private const string WineFoxKeywordsTypeName = "STS2_WineFox.Cards.WineFoxKeywords";
	private const string WineFoxRootResource = "res://STS2_WineFox/";

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
		if (field?.GetValue(null) is not CardKeyword keyword)
			return false;

		craftKeyword = keyword;
		return true;
	}
}
