using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant.Standalone;

/// <summary>在无前缀库时，将本模组的附魔类型并入 <see cref="ModelDb.DebugEnchantments" /> 查询结果。</summary>
internal static class MoreEnchantEnchantmentRegistry
{
	private static readonly HashSet<Type> Types = [];

	internal static void Register<TEnchantment>() where TEnchantment : EnchantmentModel
	{
		if (!TryRegister(typeof(TEnchantment), out var reason))
			throw new InvalidOperationException(reason);
	}

	internal static bool TryRegister(Type enchantmentType, out string? reason)
	{
		reason = null;
		if (enchantmentType == null)
		{
			reason = "Enchantment type cannot be null.";
			return false;
		}

		if (!typeof(EnchantmentModel).IsAssignableFrom(enchantmentType))
		{
			reason = $"Type '{enchantmentType.FullName}' does not inherit from EnchantmentModel.";
			return false;
		}

		if (!Types.Add(enchantmentType))
		{
			reason = $"Type '{enchantmentType.FullName}' is already registered.";
			return false;
		}

		return true;
	}

	internal static IEnumerable<EnchantmentModel> ResolveAppended()
	{
		foreach (var type in Types)
		{
			if (!ModelDb.Contains(type))
				continue;
			yield return ModelDb.GetById<EnchantmentModel>(ModelDb.GetId(type));
		}
	}
}
