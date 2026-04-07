namespace MoreEnchant;

/// <summary>调试：下一场遭遇战卡牌奖励（Combat card reward）生成时，强制其中一张附上铃铛诅咒。</summary>
internal static class MoreEnchantCombatRewardDebug
{
	internal static volatile bool ForceNextEncounterCardRewardBellCurse;

	internal static void RequestForceBellCurseOnNextEncounterCardReward() =>
		ForceNextEncounterCardRewardBellCurse = true;
}
