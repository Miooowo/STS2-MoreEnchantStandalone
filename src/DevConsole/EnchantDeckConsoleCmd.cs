using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Exceptions;
using MegaCrit.Sts2.Core.Runs;

namespace MoreEnchant.DevConsole;

/// <summary>调试控制台：<c>enchantdeck</c>，对牌库（Deck）中按下标的牌施加附魔。</summary>
public sealed class EnchantDeckConsoleCmd : AbstractConsoleCmd
{
	public override string CmdName => "enchantdeck";

	public override string Args => "<enchant-id:string> [amount:int] [deck-index:int]";

	public override string Description =>
		"Enchants a card in your Deck by index (0-based). Requires a run in progress. Does not require combat.";

	public override bool IsNetworked => true;

	public override CmdResult Process(Player? issuingPlayer, string[] args)
	{
		if (args.Length == 0)
			return new CmdResult(false, "Must specify an enchantment ID!");
		if (issuingPlayer == null)
			return new CmdResult(false, "No issuing player.");
		if (!RunManager.Instance.IsInProgress)
			return new CmdResult(false, "A run is currently not in progress!");

		var modelId = new ModelId(ModelId.SlugifyCategory<EnchantmentModel>(), args[0].ToUpperInvariant());
		EnchantmentModel enchantmentModel;
		try
		{
			enchantmentModel = ModelDb.GetById<EnchantmentModel>(modelId).ToMutable();
		}
		catch (ModelNotFoundException)
		{
			return new CmdResult(false, "Enchantment '" + modelId.Entry + "' not found");
		}

		var amount = 1;
		if (args.Length > 1 && !int.TryParse(args[1], out amount))
			return new CmdResult(false, "Arg 2 must be the enchantment amount (int), got '" + args[1] + "'.");

		var deckIndex = 0;
		if (args.Length > 2 && !int.TryParse(args[2], out deckIndex))
			return new CmdResult(false, "Arg 3 must be the deck index (int), got '" + args[2] + "'.");

		var pile = PileType.Deck.GetPile(issuingPlayer);
		var cards = pile.Cards;
		var count = cards.Count;
		if (deckIndex < 0 || deckIndex >= count)
			return new CmdResult(false, $"Invalid deck index {deckIndex}. Valid range: 0-{count - 1} (deck size {count}).");

		var cardModel = cards[deckIndex];
		if (!enchantmentModel.CanEnchant(cardModel))
			return new CmdResult(
				false,
				$"Cannot enchant {cardModel.Id.Entry} with {enchantmentModel.Id.Entry} (type/rules conflict). Example: curses or wrong card type.");

		try
		{
			CardCmd.Enchant(enchantmentModel, cardModel, amount);
		}
		catch (InvalidOperationException ex)
		{
			return new CmdResult(false, ex.Message);
		}

		return new CmdResult(
			true,
			$"Enchanted deck card [{deckIndex}] {cardModel.Title} with {amount} {enchantmentModel.Title.GetFormattedText()}");
	}

	public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
	{
		if (args.Length <= 1)
		{
			var candidates = ModelDb.DebugEnchantments.Select(e => e.Id.Entry).ToList();
			return CompleteArgument(candidates, [], args.FirstOrDefault() ?? "");
		}

		return new CompletionResult
		{
			Type = CompletionType.Argument,
			ArgumentContext = CmdName,
		};
	}
}
