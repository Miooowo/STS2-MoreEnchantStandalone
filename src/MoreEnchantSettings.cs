using System.Text.Json.Serialization;

namespace MoreEnchant;

/// <summary>附魔奖励设置（Ritsu 版存 ModDataStore；独立版存模组目录 more_enchant_settings.json）。</summary>
public sealed class MoreEnchantSettings
{
	public const string StoreKey = "settings";

	/// <summary>卡牌奖励等：每张选项获得随机附魔的基础概率（0–100，百分数）。</summary>
	[JsonPropertyName("reward_enchant_chance_percent")]
	public int RewardEnchantChancePercent { get; set; } = 10;

	/// <summary>
	/// 为真时按卡牌稀有度使用 Chimera 式四档权重曲线；为假时仅使用下方四组相对权重（无视卡牌稀有度）。
	/// </summary>
	[JsonPropertyName("use_chimera_rarity_by_card_rarity")]
	public bool UseChimeraRarityByCardRarity { get; set; } = true;

	/// <summary>自定义：普通档相对权重（与另外三档一同归一化）。</summary>
	[JsonPropertyName("weight_common")]
	public int WeightCommon { get; set; } = 500;

	[JsonPropertyName("weight_uncommon")]
	public int WeightUncommon { get; set; } = 300;

	[JsonPropertyName("weight_rare")]
	public int WeightRare { get; set; } = 199;

	[JsonPropertyName("weight_special")]
	public int WeightSpecial { get; set; } = 1;
}
