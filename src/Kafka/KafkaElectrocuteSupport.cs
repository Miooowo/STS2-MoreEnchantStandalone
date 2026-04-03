using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant.Kafka;

/// <summary>
/// 在已加载程序集中查找 Kafka 模组的触电 Power，并确认其已登记进 <see cref="ModelDb"/>。
/// </summary>
internal static class KafkaElectrocuteSupport
{
	/// <summary>
	/// Kafka 触电 debuff 为 <c>Kafka.KafkaCode.Powers.ShockPower</c>：<see cref="PowerModel.Amount"/> 为每跳伤害，
	/// 内部 <c>ShockData.RemainingTurns</c> 默认 2 回合。
	/// </summary>
	private static readonly string[] PreferredPowerTypeNames =
	[
		"ShockPower",
		"ShockTotalPower",
		"ElectrocutePower",
		"ElectrocutionPower",
		"ElectricShockPower",
	];

	private static int _assemblyCount = -1;
	private static Type? _cachedPowerType;
	private static Type? _modelDbVerifiedFor;

	public static bool TryGetElectrocutePowerType(out Type? powerType)
	{
		var assemblies = AppDomain.CurrentDomain.GetAssemblies();
		if (assemblies.Length != _assemblyCount)
		{
			_assemblyCount = assemblies.Length;
			_cachedPowerType = ResolvePowerType(assemblies);
			if (_cachedPowerType != _modelDbVerifiedFor)
				_modelDbVerifiedFor = null;
		}

		powerType = _cachedPowerType;
		return powerType != null;
	}

	public static bool IsElectrocuteRegisteredInModelDb(Type powerType)
	{
		if (_modelDbVerifiedFor == powerType)
			return true;
		try
		{
			_ = ModelDb.DebugPower(powerType);
			_modelDbVerifiedFor = powerType;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static Type? ResolvePowerType(IReadOnlyList<Assembly> assemblies)
	{
		var kafkaAssemblies = assemblies
			.Where(static a =>
			{
				var n = a.GetName().Name ?? "";
				return n.Contains("Kafka", StringComparison.OrdinalIgnoreCase);
			})
			.ToArray();

		if (kafkaAssemblies.Length == 0)
			return null;

		foreach (var asm in kafkaAssemblies)
		{
			Type[] types;
			try
			{
				types = asm.GetTypes();
			}
			catch (ReflectionTypeLoadException e)
			{
				types = e.Types.Where(static t => t != null).Cast<Type>().ToArray();
			}

			foreach (var name in PreferredPowerTypeNames)
			{
				var t = types.FirstOrDefault(x => x.Name == name);
				if (t != null && IsConcretePowerModel(t))
					return t;
			}

			var fallback = types
				.Where(static t =>
					IsConcretePowerModel(t) &&
					(t.Name.Contains("Electrocute", StringComparison.OrdinalIgnoreCase) ||
					 t.Name.Contains("ElectrShock", StringComparison.OrdinalIgnoreCase) ||
					 (t.Name.Contains("Shock", StringComparison.OrdinalIgnoreCase) &&
					  t.Name.EndsWith("Power", StringComparison.Ordinal))))
				.OrderByDescending(static t => t.Name.Equals("ShockPower", StringComparison.Ordinal))
				.ThenByDescending(static t => t.Name.Equals("ShockTotalPower", StringComparison.Ordinal))
				.ThenBy(static t => t.Name.Length)
				.FirstOrDefault();

			if (fallback != null)
				return fallback;
		}

		return null;
	}

	private static bool IsConcretePowerModel(Type t) =>
		!t.IsAbstract && typeof(PowerModel).IsAssignableFrom(t);
}
