using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// <see cref="CardModel.FromSerializable"/> 执行期间记录的 <see cref="SerializableCard"/>（栈顶为当前正在还原的牌）。
/// 当快照里 <see cref="SerializableCard.CurrentUpgradeLevel"/> 大于该牌模板的默认 <see cref="CardModel.MaxUpgradeLevel"/> 时抬升上限，
/// 避免运行史等多段升级 + 附魔不匹配（或附魔 ID 无法识别灼热）时在还原循环里崩溃。
/// </summary>
internal static class CardModelFromSerializableScope
{
	[ThreadStatic]
	private static List<SerializableCard>? _stack;

	private static List<SerializableCard> Stack => _stack ??= [];

	internal static void Enter(SerializableCard? save)
		=> Stack.Add(save!);

	internal static void Leave()
	{
		if (Stack.Count > 0)
			Stack.RemoveAt(Stack.Count - 1);
	}

	/// <summary>
	/// 读取模板 <see cref="CardModel.MaxUpgradeLevel"/> 时会再次进入本模组的 postfix；用计数器短暂关闭「按存档抬上限」，避免递归把模板误判成无限档。
	/// </summary>
	[ThreadStatic]
	private static int _suppressDeserializeCapLiftInPostfix;

	private static int ReadTemplateMaxUpgradeLevel(ModelId cardId)
	{
		_suppressDeserializeCapLiftInPostfix++;
		try
		{
			return SaveUtil.CardOrDeprecated(cardId).ToMutable().MaxUpgradeLevel;
		}
		finally
		{
			_suppressDeserializeCapLiftInPostfix--;
		}
	}

	internal static bool ActiveSaveNeedsDeserializeMaxCapLift()
	{
		if (_suppressDeserializeCapLiftInPostfix > 0 || Stack.Count == 0)
			return false;

		var save = Stack[^1];
		if (save.Id is not { } cardId)
			return false;

		try
		{
			var templateMax = ReadTemplateMaxUpgradeLevel(cardId);
			return save.CurrentUpgradeLevel > templateMax;
		}
		catch
		{
			return false;
		}
	}

	internal static int PeekTopSaveCurrentUpgradeLevel()
		=> Stack.Count > 0 ? Stack[^1].CurrentUpgradeLevel : 0;
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.FromSerializable))]
internal static class CardModelFromSerializableScopePatch
{
	[HarmonyPrefix]
	[HarmonyPriority(Priority.First)]
	private static void Prefix(SerializableCard save)
		=> CardModelFromSerializableScope.Enter(save);

	[HarmonyFinalizer]
	private static Exception? Finalizer(Exception? __exception)
	{
		CardModelFromSerializableScope.Leave();
		return __exception;
	}
}

/// <summary>灼热：允许额外升级档位；第一次显示为「牌名+」，第二次起为「牌名+等级」（与原版多段升级规则衔接）。</summary>
[HarmonyPatch(typeof(CardModel), "get_MaxUpgradeLevel")]
internal static class CardModelScorchingMaxUpgradePatch
{
	/// <summary>
	/// 勿使用极大常数：原版 <see cref="CardModel.Title"/> 在 <c>MaxUpgradeLevel &gt; 1</c> 时走多段升级标题分支，
	/// 运行史 <see cref="MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen.NDeckHistoryEntry"/> 会按标题宽度排版，过大上限易导致整栏位移异常。
	/// </summary>
	[HarmonyPostfix]
	private static void Postfix(CardModel __instance, ref int __result)
	{
		var need = __result;

		// 至少保留 2 档可升空间；且须严格大于 CurrentUpgradeLevel，否则升到档 2 后 Max 仍为 2 → 无法继续升级。
		if (__instance.Enchantment is ScorchingEnchantment)
			need = Math.Max(need, Math.Max(999, __instance.CurrentUpgradeLevel + 1));

		if (CardModelFromSerializableScope.ActiveSaveNeedsDeserializeMaxCapLift())
			need = Math.Max(need, CardModelFromSerializableScope.PeekTopSaveCurrentUpgradeLevel());

		if (need > __result)
			__result = need;
	}
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
internal static class CardModelScorchingTitlePatch
{
	/// <summary>先于 <see cref="CardModelStrikeTitlePatch"/>，把 +1 规范为单独的 +。</summary>
	[HarmonyPostfix]
	[HarmonyPriority(Priority.First)]
	private static void Postfix(CardModel __instance, ref string __result)
	{
		if (__instance.Enchantment is not ScorchingEnchantment)
			return;
		if (!__instance.IsUpgraded)
			return;

		var baseTitle = __instance.TitleLocString.GetFormattedText();
		if (__instance.CurrentUpgradeLevel <= 1)
			__result = baseTitle + "+";
		else
			__result = $"{baseTitle}+{__instance.CurrentUpgradeLevel}";
	}
}
