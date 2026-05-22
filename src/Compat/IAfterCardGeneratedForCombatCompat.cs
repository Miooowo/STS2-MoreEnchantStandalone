using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant.Compat;

internal interface IAfterCardGeneratedForCombatCompat
{
	Task AfterCardGeneratedForCombatCompat(CardModel card, bool addedByPlayer);
}
