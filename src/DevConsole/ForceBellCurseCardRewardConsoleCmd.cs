using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MoreEnchant;

namespace MoreEnchant.DevConsole;

/// <summary>控制台：<c>forcebellreward</c>，本场战斗中记下请求，使下一次遭遇战卡牌奖励中必有一张带铃铛诅咒附魔。</summary>
public sealed class ForceBellCurseCardRewardConsoleCmd : AbstractConsoleCmd
{
	public override string CmdName => "forcebellreward";

	public override string Args => "";

	public override string Description =>
		"During combat: next encounter card reward includes one card with Curse of the Bell enchantment.";

	public override bool IsNetworked => true;

	public override CmdResult Process(Player? issuingPlayer, string[] args)
	{
		if (issuingPlayer == null)
			return new CmdResult(false, "No issuing player.");
		if (!CombatManager.Instance.IsInProgress)
			return new CmdResult(false, "Use this command while a combat is in progress.");

		MoreEnchantCombatRewardDebug.RequestForceBellCurseOnNextEncounterCardReward();
		return new CmdResult(
			true,
			"Next encounter card reward will include one card with Curse of the Bell (if any option can take the enchant).");
	}
}
