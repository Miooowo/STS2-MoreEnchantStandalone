using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Enchantments.Mocks;
using MoreEnchant.Standalone;

namespace MoreEnchant.EnchantmentCompendium;

internal static class EnchantmentBrowserOverlay
{
	internal static void Show(NCompendiumSubmenu host)
	{
		if (host.GetNodeOrNull(EnchantmentCompendiumConstants.OverlayNodeName) != null)
			return;

		var list = CollectEnchantments();
		var overlay = new EnchantmentBrowserRoot(list);
		overlay.Name = EnchantmentCompendiumConstants.OverlayNodeName;
		host.AddChild(overlay);
	}

	private static List<EnchantmentModel> CollectEnchantments()
	{
		var seen = new HashSet<ModelId>();
		var result = new List<EnchantmentModel>();

		void TryAdd(EnchantmentModel e)
		{
			if (!ShouldInclude(e))
				return;
			if (seen.Add(e.Id))
				result.Add(e);
		}

		foreach (var e in ModelDb.DebugEnchantments)
			TryAdd(e);
		foreach (var e in MoreEnchantEnchantmentRegistry.ResolveAppended())
			TryAdd(e);

		result.Sort(static (a, b) =>
			string.CompareOrdinal(a.Title.GetFormattedText(), b.Title.GetFormattedText()));
		return result;
	}

	private static bool ShouldInclude(EnchantmentModel e)
	{
		var t = e.GetType();
		if (t == typeof(DeprecatedEnchantment))
			return false;
		if (t == typeof(MockFreeEnchantment) || t.Namespace == typeof(MockFreeEnchantment).Namespace)
			return false;
		return true;
	}
}
