using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MoreEnchant.Enchantments;

namespace MoreEnchant.Patches;

/// <summary>
/// 冰梆等遗物复制牌时往往直接 <see cref="CardPileCmd.Add"/> 进牌组，不经过 <see cref="RewardSynchronizer.SyncLocalObtainedCard"/>。
/// 各版本 <c>Add</c> 参数顺序/个数可能不同，故扫描「含 <see cref="CardModel"/> + <see cref="PileType"/> 且返回 <see cref="Task"/>」的静态重载。
/// 必须用 <see cref="Task{TResult}"/> 包装以保留原返回值；若只返回非泛型 <see cref="Task"/>，调用方 <c>await</c> 会得到错误结果并在 <c>CardReward.OnSelect</c> 等处 NRE。
/// </summary>
[HarmonyPatch]
internal static class CardPileCmdBellCurseDeckAddPatch
{
	private static readonly MethodBase[] Targets = BuildTargets();

	private static readonly MethodInfo AfterDeckAddGenericOpen = AccessTools.DeclaredMethod(
		typeof(CardPileCmdBellCurseDeckAddPatch),
		nameof(AfterDeckAddGeneric))!;

	private static MethodBase[] BuildTargets()
	{
		var list = new List<MethodBase>();
		foreach (var m in AccessTools.GetDeclaredMethods(typeof(CardPileCmd)))
		{
			if (m.Name != nameof(CardPileCmd.Add) || !m.IsStatic)
				continue;
			var ps = m.GetParameters();
			var hasCard = ps.Any(p => typeof(CardModel).IsAssignableFrom(p.ParameterType));
			var hasPile = ps.Any(p => p.ParameterType == typeof(PileType));
			if (!hasCard || !hasPile)
				continue;
			if (!typeof(Task).IsAssignableFrom(m.ReturnType))
				continue;
			list.Add(m);
		}

		return list.ToArray();
	}

	private static bool Prepare() => Targets.Length > 0;

	private static IEnumerable<MethodBase> TargetMethods() => Targets;

	[HarmonyPostfix]
	private static void Postfix(object[] __args, ref object __result)
	{
		if (__args == null || __result == null)
			return;

		CardModel? card = null;
		PileType pile = default;
		var seenPile = false;
		foreach (var o in __args)
		{
			switch (o)
			{
				case CardModel c:
					card = c;
					break;
				case PileType p:
					pile = p;
					seenPile = true;
					break;
			}
		}

		if (!seenPile || pile != PileType.Deck || card == null)
			return;

		var rt = __result.GetType();
		if (rt == typeof(Task))
		{
			__result = AfterDeckAddPlain((Task)__result, card);
			return;
		}

		if (rt.IsGenericType && rt.GetGenericTypeDefinition() == typeof(Task<>))
		{
			var tArg = rt.GetGenericArguments()[0];
			var closed = AfterDeckAddGenericOpen.MakeGenericMethod(tArg);
			__result = closed.Invoke(null, [__result, card])!;
		}
	}

	private static async Task AfterDeckAddPlain(Task inner, CardModel card)
	{
		await inner.ConfigureAwait(false);
		MaybeGrantBellAfterAwait(card);
	}

	private static async Task<T> AfterDeckAddGeneric<T>(Task<T> inner, CardModel card)
	{
		var r = await inner.ConfigureAwait(false);
		MaybeGrantBellAfterAwait(card);
		return r;
	}

	private static void MaybeGrantBellAfterAwait(CardModel card)
	{
		if (card.Owner is not Player p)
			return;
		if (card.Enchantment is not BellCurseEnchantment bell || !bell.TryTakeRewardRelicGrantOnce())
			return;
		_ = TaskHelper.RunSafely(BellCurseReward.GrantCore(p));
	}
}
