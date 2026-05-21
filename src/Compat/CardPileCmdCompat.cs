using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant.Compat;

internal static class CardPileCmdCompat
{
	internal static async Task AddGeneratedCardToCombat(CardModel card, PileType pileType, bool addedByPlayer)
	{
		var methods = typeof(CardPileCmd)
			.GetMethods(BindingFlags.Public | BindingFlags.Static)
			.Where(m => m.Name == nameof(CardPileCmd.AddGeneratedCardToCombat))
			.ToList();
		foreach (var method in methods)
		{
			var parameters = method.GetParameters();
			if (parameters.Length < 2)
				continue;
			if (parameters[0].ParameterType != typeof(CardModel) || parameters[1].ParameterType != typeof(PileType))
				continue;

			var args = new object?[parameters.Length];
			args[0] = card;
			args[1] = pileType;

			var supported = true;
			for (var i = 2; i < parameters.Length; i++)
			{
				var type = parameters[i].ParameterType;
				if (type == typeof(bool))
				{
					args[i] = addedByPlayer;
				}
				else if (type == typeof(CardPilePosition))
				{
					args[i] = CardPilePosition.Bottom;
				}
				else if (type == typeof(Player))
				{
					args[i] = card.Owner;
				}
				else
				{
					supported = false;
					break;
				}
			}

			if (!supported)
				continue;

			await ((Task)method.Invoke(null, args)!).ConfigureAwait(false);
			return;
		}

		throw new MissingMethodException("CardPileCmd.AddGeneratedCardToCombat signature not found.");
	}
}
