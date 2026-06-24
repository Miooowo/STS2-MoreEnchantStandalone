using System.Reflection;
using Godot;

namespace MoreEnchant.Standalone.Compat;

/// <summary>星引擎（ReAstralPartyMod）联动检测，避免硬依赖。</summary>
internal static class ReAstralPartyCompat
{
	private static readonly string[] AstralAssemblyNames =
	[
		"ReAstralPartyMod",
		"ReAstralPartyCardCode",
	];

	private static readonly string[] AstralProbeTypeNames =
	[
		"ReAstralPartyMod.ReAstralPartyCardCode.cards.BaseAbilityWrongTarget",
		"ReAstralPartyMod.ReAstralPartyCardCode.Powers.WrongTargetPower",
	];

	internal static bool IsAstralPartyModAvailable()
	{
		// pck 资源存在时，优先视为可用。
		if (ResourceLoader.Exists("res://ReAstralPartyMod/"))
			return true;

		foreach (var typeName in AstralProbeTypeNames)
		{
			if (ResolveType(typeName) != null)
				return true;
		}

		return AppDomain.CurrentDomain.GetAssemblies()
			.Any(static asm =>
			{
				var name = asm.GetName().Name;
				return !string.IsNullOrWhiteSpace(name) &&
				       name.Contains("ReAstralParty", StringComparison.OrdinalIgnoreCase);
			});
	}

	private static Type? ResolveType(string fullTypeName)
	{
		foreach (var assemblyName in AstralAssemblyNames)
		{
			var type = Type.GetType($"{fullTypeName}, {assemblyName}", throwOnError: false);
			if (type != null)
				return type;
		}

		foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
		{
			var asmName = asm.GetName().Name;
			if (string.IsNullOrWhiteSpace(asmName) ||
			    !asmName.Contains("ReAstralParty", StringComparison.OrdinalIgnoreCase))
				continue;
			var type = asm.GetType(fullTypeName);
			if (type != null)
				return type;
		}

		return null;
	}
}
