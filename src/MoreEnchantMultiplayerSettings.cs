using System.Text.Json;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace MoreEnchant;

/// <summary>联机时由房主通过 <c>InitialGameInfoMessage</c> 尾缀广播的模组设置快照；客机玩法与宿主一致。</summary>
internal static class MoreEnchantMultiplayerSettings
{
	private static readonly object Sync = new();
	private static MoreEnchantSettings? _hostReplica;

	internal static void ApplyFromHost(MoreEnchantSettings incoming)
	{
		var json = JsonSerializer.Serialize(incoming);
		lock (Sync)
		{
			_hostReplica = JsonSerializer.Deserialize<MoreEnchantSettings>(json) ?? new MoreEnchantSettings();
			if (_hostReplica.WeightCurse <= 0)
				_hostReplica.WeightCurse = 250;
		}
	}

	/// <summary>联机客机：已收到房主快照则用快照；否则本地 <see cref="MoreEnchantSettingsStore"/>。</summary>
	internal static MoreEnchantSettings GetEffectiveSettings()
	{
		MoreEnchantSettings? replica;
		lock (Sync)
			replica = _hostReplica;

		try
		{
			var net = RunManager.Instance?.NetService;
			if (net != null
			    && net.Type == NetGameType.Client
			    && net.IsConnected
			    && replica != null)
				return replica;
		}
		catch
		{
			// RunManager / NetService 在极端时点不可用则回落本地
		}

		return MoreEnchantSettingsStore.Get();
	}
}
