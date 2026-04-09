using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace MoreEnchant.Patches;

/// <summary>在联机初始握手包末尾附加房主 More Enchant 设置（比特对齐 + 魔数 + JSON），客机反序列化后供卡牌奖励附魔使用。</summary>
internal static class InitialGameInfoMoreEnchantSettingsPatch
{
	private const uint TrailerMagic = 0x4D453156u; // 'ME1V'

	[HarmonyPatch(typeof(InitialGameInfoMessage), nameof(InitialGameInfoMessage.Serialize))]
	[HarmonyPostfix]
	private static void SerializePostfix(PacketWriter writer)
	{
		try
		{
			var settings = MoreEnchantSettingsStore.Get();
			var json = JsonSerializer.Serialize(settings);
			int padBits = (8 - (writer.BitPosition % 8)) % 8;
			if (padBits != 0)
				writer.WriteByte(0, padBits);
			writer.WriteUInt(TrailerMagic);
			writer.WriteString(json);
		}
		catch
		{
			// 附件魔设置失败时不应阻断联机握手
		}
	}

	[HarmonyPatch(typeof(InitialGameInfoMessage), nameof(InitialGameInfoMessage.Deserialize))]
	[HarmonyPostfix]
	private static void DeserializePostfix(PacketReader reader)
	{
		try
		{
			int remainingBits = reader.Buffer.Length * 8 - reader.BitPosition;
			if (remainingBits < 32 + 32)
				return;

			int padBits = (8 - (reader.BitPosition % 8)) % 8;
			if (remainingBits < padBits + 32 + 32)
				return;

			if (padBits != 0)
				reader.ReadByte(padBits);

			uint magic = reader.ReadUInt(32);
			if (magic != TrailerMagic)
				return;

			string json = reader.ReadString();
			if (string.IsNullOrEmpty(json))
				return;

			var parsed = JsonSerializer.Deserialize<MoreEnchantSettings>(json);
			if (parsed != null)
				MoreEnchantMultiplayerSettings.ApplyFromHost(parsed);
		}
		catch
		{
			// 尾缀缺失或损坏：保持仅用本地设置
		}
	}
}
