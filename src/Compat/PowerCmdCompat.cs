using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant.Compat;

internal static class PowerCmdCompat
{
	private static readonly BlockingPlayerChoiceContext FallbackChoiceContext = new();

	internal static async Task<T?> Apply<T>(
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		PlayerChoiceContext? choiceContext = null,
		bool silent = false) where T : PowerModel
	{
		var list = await Apply<T>(new[] { target }, amount, applier, cardSource, choiceContext, silent);
		return list.FirstOrDefault();
	}

	internal static async Task<IReadOnlyList<T>> Apply<T>(
		IEnumerable<Creature> targets,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		PlayerChoiceContext? choiceContext = null,
		bool silent = false) where T : PowerModel
	{
		var methods = typeof(PowerCmd)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Where(m => m.Name == nameof(PowerCmd.Apply) && m.IsGenericMethodDefinition)
			.ToList();

		var genericApply = methods.FirstOrDefault(m =>
			{
				var p = m.GetParameters();
				return p.Length == 6 &&
				       p[0].ParameterType == typeof(PlayerChoiceContext) &&
				       typeof(IEnumerable<Creature>).IsAssignableFrom(p[1].ParameterType);
			});
		if (genericApply != null)
		{
			var args = new object?[] { choiceContext ?? FallbackChoiceContext, targets, amount, applier, cardSource, silent };
			var taskObj = genericApply.MakeGenericMethod(typeof(T)).Invoke(null, args);
			var task = (Task)taskObj!;
			await task.ConfigureAwait(false);
			return (IReadOnlyList<T>)task.GetType().GetProperty("Result")!.GetValue(task)!;
		}

		var legacyGenericApply = methods.FirstOrDefault(m =>
		{
			var p = m.GetParameters();
			return p.Length == 5 && typeof(IEnumerable<Creature>).IsAssignableFrom(p[0].ParameterType);
		});
		if (legacyGenericApply == null)
			throw new MissingMethodException("PowerCmd.Apply<T> signature not found.");

		{
			var args = new object?[] { targets, amount, applier, cardSource, silent };
			var taskObj = legacyGenericApply.MakeGenericMethod(typeof(T)).Invoke(null, args);
			var task = (Task)taskObj!;
			await task.ConfigureAwait(false);
			return (IReadOnlyList<T>)task.GetType().GetProperty("Result")!.GetValue(task)!;
		}
	}

	internal static async Task Apply(
		PowerModel power,
		Creature target,
		decimal amount,
		Creature? applier,
		CardModel? cardSource,
		PlayerChoiceContext? choiceContext = null,
		bool silent = false)
	{
		var methods = typeof(PowerCmd)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Where(m => m.Name == nameof(PowerCmd.Apply) && !m.IsGenericMethodDefinition)
			.ToList();

		var apply = methods.FirstOrDefault(m =>
			{
				var p = m.GetParameters();
				return p.Length == 7 &&
				       p[0].ParameterType == typeof(PlayerChoiceContext) &&
				       p[1].ParameterType == typeof(PowerModel);
			});
		if (apply != null)
		{
			var task = (Task)apply.Invoke(null,
				[choiceContext ?? FallbackChoiceContext, power, target, amount, applier, cardSource, silent])!;
			await task.ConfigureAwait(false);
			return;
		}

		var legacyApply = methods.FirstOrDefault(m =>
		{
			var p = m.GetParameters();
			return p.Length == 6 && p[0].ParameterType == typeof(PowerModel);
		});
		if (legacyApply == null)
			throw new MissingMethodException("PowerCmd.Apply(PowerModel, ...) signature not found.");

		{
			var task = (Task)legacyApply.Invoke(null, new object?[] { power, target, amount, applier, cardSource, silent })!;
			await task.ConfigureAwait(false);
		}
	}
}
