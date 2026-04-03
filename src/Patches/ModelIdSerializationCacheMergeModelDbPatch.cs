using System.Buffers.Binary;
using System.Collections;
using System.IO.Hashing;
using System.Reflection;
using System.Text;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Timeline;

namespace MoreEnchant.Patches;

/// <summary>
/// 原版 <see cref="ModelIdSerializationCache.Init" /> 只扫描部分模型子类型；模组附魔等没有 net ID，
/// 写回放/存档序列化时会抛 <c>could not be mapped to any net ID</c>。
/// 在 Init 之后并入 <see cref="ModelDb"/>；对部分只在运行期才出现的 entry（例如仅带 ID 的附魔引用），
/// 再在 <see cref="ModelIdSerializationCache.GetNetIdForEntry" /> 上惰性补登记。
/// </summary>
[HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))]
internal static class ModelIdSerializationCacheMergeModelDbPatch
{
	private static void Postfix() => ModelIdSerializationCacheMergeHelper.MergeAllFromModelDb();
}

[HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.GetNetIdForEntry))]
internal static class ModelIdSerializationCacheLazyEntryPatch
{
	private static bool Prefix(string entry, ref int __result)
	{
		if (string.IsNullOrEmpty(entry))
			return true;

		var entMap =
			ModelIdSerializationCacheMergeHelper.GetStaticField<Dictionary<string, int>>(typeof(ModelIdSerializationCache),
				"_entryNameToNetIdMap");
		var entList = ModelIdSerializationCacheMergeHelper.GetStaticField<List<string>>(typeof(ModelIdSerializationCache),
			"_netIdToEntryNameMap");
		if (entMap == null || entList == null)
			return true;

		if (entMap.TryGetValue(entry, out __result))
			return false;

		ModelIdSerializationCacheMergeHelper.EnsureEntry(entry, entMap, entList);
		ModelIdSerializationCacheMergeHelper.RefreshBitSizesAndHashAfterMapChange();
		__result = entMap[entry];
		return false;
	}
}

internal static class ModelIdSerializationCacheMergeHelper
{
	internal static void MergeAllFromModelDb()
	{
		var contentById = GetModelDbContentById();
		if (contentById == null || contentById.Count == 0)
			return;

		var catMap =
			GetStaticField<Dictionary<string, int>>(typeof(ModelIdSerializationCache), "_categoryNameToNetIdMap");
		var catList = GetStaticField<List<string>>(typeof(ModelIdSerializationCache), "_netIdToCategoryNameMap");
		var entMap =
			GetStaticField<Dictionary<string, int>>(typeof(ModelIdSerializationCache), "_entryNameToNetIdMap");
		var entList = GetStaticField<List<string>>(typeof(ModelIdSerializationCache), "_netIdToEntryNameMap");

		if (catMap == null || catList == null || entMap == null || entList == null)
			return;

		foreach (DictionaryEntry entry in contentById)
		{
			if (entry.Key is not ModelId id)
				continue;

			EnsureCategory(id.Category, catMap, catList);
			EnsureEntry(id.Entry, entMap, entList);
		}

		RefreshBitSizesAndHashAfterMapChange();
	}

	internal static void RefreshBitSizesAndHashAfterMapChange()
	{
		var contentById = GetModelDbContentById();
		if (contentById == null)
			return;

		var catList = GetStaticField<List<string>>(typeof(ModelIdSerializationCache), "_netIdToCategoryNameMap");
		var entList = GetStaticField<List<string>>(typeof(ModelIdSerializationCache), "_netIdToEntryNameMap");
		var epochList = GetStaticField<List<string>>(typeof(ModelIdSerializationCache), "_netIdToEpochNameMap");
		if (catList == null || entList == null)
			return;

		var maxCategory = catList.Count;
		var maxEntry = entList.Count;
		var maxEpoch = epochList?.Count ?? 0;

		SetStaticProperty(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.CategoryIdBitSize),
			Mathf.CeilToInt(Math.Log2(Math.Max(1, maxCategory))));
		SetStaticProperty(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.EntryIdBitSize),
			Mathf.CeilToInt(Math.Log2(Math.Max(1, maxEntry))));
		SetStaticProperty(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.EpochIdBitSize),
			Mathf.CeilToInt(Math.Log2(Math.Max(1, maxEpoch))));

		var newHash = ComputeHashLikeVanilla(contentById, maxCategory, maxEntry, maxEpoch);
		SetStaticProperty(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Hash), newHash);
	}

	internal static void EnsureCategory(string category, Dictionary<string, int> map, List<string> list)
	{
		if (map.ContainsKey(category))
			return;

		map[category] = list.Count;
		list.Add(category);
	}

	internal static void EnsureEntry(string entry, Dictionary<string, int> map, List<string> list)
	{
		if (map.ContainsKey(entry))
			return;

		map[entry] = list.Count;
		list.Add(entry);
	}

	internal static T? GetStaticField<T>(Type declaringType, string name)
		where T : class
	{
		return AccessTools.DeclaredField(declaringType, name)?.GetValue(null) as T;
	}

	private static IDictionary? GetModelDbContentById()
	{
		var field = AccessTools.DeclaredField(typeof(ModelDb), "_contentById");
		return field?.GetValue(null) as IDictionary;
	}

	private static uint ComputeHashLikeVanilla(IDictionary contentById, int maxCategory, int maxEntry, int maxEpoch)
	{
		var buffer = new byte[512];
		var xxHash = new XxHash32();

		var types = new HashSet<Type>();
		foreach (var t in ModelDb.AllAbstractModelSubtypes)
			types.Add(t);

		foreach (DictionaryEntry entry in contentById)
			if (entry.Value is AbstractModel model)
				types.Add(model.GetType());

		var sorted = types.ToList();
		sorted.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));

		foreach (var id in sorted.Select(ModelDb.GetId))
		{
			AppendUtf8(xxHash, id.Category, buffer);
			AppendUtf8(xxHash, id.Entry, buffer);
		}

		foreach (var epochId in EpochModel.AllEpochIds)
			AppendUtf8(xxHash, epochId, buffer);

		BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(), maxCategory);
		xxHash.Append(buffer.AsSpan(0, 4));
		BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(), maxEntry);
		xxHash.Append(buffer.AsSpan(0, 4));
		BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(), maxEpoch);
		xxHash.Append(buffer.AsSpan(0, 4));

		return xxHash.GetCurrentHashAsUInt32();
	}

	private static void AppendUtf8(XxHash32 xxHash, string text, byte[] buffer)
	{
		var bytes = Encoding.UTF8.GetBytes(text, 0, text.Length, buffer, 0);
		xxHash.Append(buffer.AsSpan(0, bytes));
	}

	private static void SetStaticProperty(Type declaringType, string name, object value)
	{
		var prop = declaringType.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
		prop?.GetSetMethod(true)?.Invoke(null, [value]);
	}
}
