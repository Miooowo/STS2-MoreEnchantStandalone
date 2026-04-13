using System.Text.Json.Serialization;

namespace MoreEnchant;

/// <summary>附魔奖励设置（Ritsu 版存 ModDataStore；独立版存模组目录 more_enchant_settings.json）。</summary>
public sealed class MoreEnchantSettings
{
	public const string StoreKey = "settings";

	/// <summary>设置 JSON 架构版本；低于 2/3 时由 <see cref="MoreEnchantSettingsStore"/> 迁移默认值。</summary>
	[JsonPropertyName("schema_version")]
	public int SchemaVersion { get; set; } = 3;

	/// <summary>卡牌奖励等：每张选项获得随机附魔的基础概率（0–100，百分数）。</summary>
	[JsonPropertyName("reward_enchant_chance_percent")]
	public int RewardEnchantChancePercent { get; set; } = 10;

	/// <summary>商店单张卡牌是否可随机附魔（概率见 <see cref="ShopEnchantChancePercent"/>）。联机以房主设置为准。</summary>
	[JsonPropertyName("shop_enchant_enabled")]
	public bool ShopEnchantEnabled { get; set; } = true;

	/// <summary>商店卡牌附魔独立概率（0–100）。</summary>
	[JsonPropertyName("shop_enchant_chance_percent")]
	public int ShopEnchantChancePercent { get; set; } = 10;

	/// <summary>先古之民（Ancient 稀有度）卡牌奖励是否可随机附魔；此类卡牌不会出现诅咒档附魔。</summary>
	[JsonPropertyName("ancient_reward_enchant_enabled")]
	public bool AncientRewardEnchantEnabled { get; set; } = true;

	/// <summary>先古之民卡牌奖励附魔概率（0–100）。</summary>
	[JsonPropertyName("ancient_reward_enchant_chance_percent")]
	public int AncientRewardEnchantChancePercent { get; set; } = 10;

	/// <summary>通过 <see cref="MegaCrit.Sts2.Core.Commands.CardPileCmd.AddGeneratedCardsToCombat"/> 加入战斗的牌（玩家侧）是否可随机附魔。联机以房主设置为准。</summary>
	[JsonPropertyName("combat_generated_enchant_enabled")]
	public bool CombatGeneratedEnchantEnabled { get; set; }

	/// <summary>战斗内生成牌附魔概率（0–100），独立于心商店/奖励。</summary>
	[JsonPropertyName("combat_generated_enchant_chance_percent")]
	public int CombatGeneratedEnchantChancePercent { get; set; } = 10;

	/// <summary>变牌（替牌继承原牌附魔后仍无附魔时）是否可再随机附魔。联机以房主为准。</summary>
	[JsonPropertyName("transform_enchant_enabled")]
	public bool TransformEnchantEnabled { get; set; } = true;

	/// <summary>变牌随机附魔概率（0–100），独立于卡牌奖励概率。</summary>
	[JsonPropertyName("transform_enchant_chance_percent")]
	public int TransformEnchantChancePercent { get; set; } = 10;

	/// <summary>
	/// 非奖励工厂路径、直接 <c>CardPileCmd.Add</c> 入牌组时（如巨大扭蛋额外打击防御、涅奥苦痛、卷轴箱等）是否可随机附魔。
	/// </summary>
	[JsonPropertyName("deck_direct_enchant_enabled")]
	public bool DeckDirectEnchantEnabled { get; set; } = true;

	/// <summary>直加牌组随机附魔概率（0–100）；卷轴箱三选一预览与最终入组共用同一套掷骰与权重。</summary>
	[JsonPropertyName("deck_direct_enchant_chance_percent")]
	public int DeckDirectEnchantChancePercent { get; set; } = 10;

	/// <summary>
	/// 为真时按卡牌稀有度使用 Chimera 式五档权重曲线；为假时仅使用下方五组相对权重（无视卡牌稀有度）。
	/// </summary>
	[JsonPropertyName("use_chimera_rarity_by_card_rarity")]
	public bool UseChimeraRarityByCardRarity { get; set; } = true;

	/// <summary>自定义：普通档相对权重（与其余四档一同归一化）。</summary>
	[JsonPropertyName("weight_common")]
	public int WeightCommon { get; set; } = 500;

	[JsonPropertyName("weight_uncommon")]
	public int WeightUncommon { get; set; } = 300;

	/// <summary>自定义：诅咒档相对权重（介于罕见与稀有之间）。</summary>
	[JsonPropertyName("weight_curse")]
	public int WeightCurse { get; set; } = 250;

	[JsonPropertyName("weight_rare")]
	public int WeightRare { get; set; } = 199;

	[JsonPropertyName("weight_special")]
	public int WeightSpecial { get; set; } = 1;
}
