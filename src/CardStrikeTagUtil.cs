using System.Collections.Generic;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant;

/// <summary>
/// 引擎未公开 <c>CardCmd.ApplyTag</c>，通过初始化后的 <see cref="CardModel.Tags"/> 缓存集合写入 <see cref="CardTag.Strike"/>。
/// </summary>
internal static class CardStrikeTagUtil
{
	private static readonly FieldInfo? TagsField = typeof(CardModel).GetField(
		"_tags",
		BindingFlags.Instance | BindingFlags.NonPublic);

	internal static void ApplyStrikeTag(CardModel? card)
	{
		if (card == null || TagsField == null)
			return;

		_ = card.Tags;

		if (TagsField.GetValue(card) is not HashSet<CardTag> set)
			return;

		set.Add(CardTag.Strike);
	}
}
