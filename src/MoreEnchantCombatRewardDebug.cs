namespace MoreEnchant;

/// <summary>调试：下一场遭遇战卡牌奖励（Combat card reward）生成时的强制附魔请求。</summary>
internal static class MoreEnchantCombatRewardDebug
{
	internal static volatile bool ForceNextEncounterCardRewardBellCurse;

	internal static volatile bool ForceNextEncounterCardRewardRandomCurse;

	internal static void RequestForceBellCurseOnNextEncounterCardReward() =>
		ForceNextEncounterCardRewardBellCurse = true;

	internal static void RequestForceRandomCurseOnNextEncounterCardReward() =>
		ForceNextEncounterCardRewardRandomCurse = true;
}
