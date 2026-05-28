using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization;
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
			string.CompareOrdinal(GetSafeTitle(a), GetSafeTitle(b)));
		return result;
	}

	private static bool ShouldInclude(EnchantmentModel e)
	{
		var t = e.GetType();
		if (t == typeof(DeprecatedEnchantment))
			return false;
		if (t == typeof(MockFreeEnchantment) || t.Namespace == typeof(MockFreeEnchantment).Namespace)
			return false;
		// 防御性过滤：缺失本地化键的附魔不进入图鉴，避免排序阶段抛 LocException 导致界面崩溃。
		if (!HasLocalizedTitle(e))
			return false;
		return true;
	}

	private static bool HasLocalizedTitle(EnchantmentModel e)
	{
		try
		{
			_ = e.Title.GetFormattedText();
			return true;
		}
		catch (LocException)
		{
			return false;
		}
	}

	private static string GetSafeTitle(EnchantmentModel e)
	{
		try
		{
			return e.Title.GetFormattedText();
		}
		catch (LocException)
		{
			// 兜底：排序时若本地化丢失，回退到 ModelId，避免 InvalidOperationException 包裹排序异常。
			return e.Id.ToString();
		}
	}
}
