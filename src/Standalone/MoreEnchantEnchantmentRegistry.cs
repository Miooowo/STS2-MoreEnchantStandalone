using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant.Standalone;

/// <summary>在无前缀库时，将本模组的附魔类型并入 <see cref="ModelDb.DebugEnchantments" /> 查询结果。</summary>
internal static class MoreEnchantEnchantmentRegistry
{
	private static readonly List<Type> Types = [];

	internal static void Register<TEnchantment>() where TEnchantment : EnchantmentModel
		=> Types.Add(typeof(TEnchantment));

	internal static IEnumerable<EnchantmentModel> ResolveAppended()
		=> Types.Select(static t => ModelDb.GetById<EnchantmentModel>(ModelDb.GetId(t)));
}
