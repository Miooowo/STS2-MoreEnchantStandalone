using MegaCrit.Sts2.Core.Models;

namespace MoreEnchant.Standalone;

/// <summary>与 RitsuLib 中同名基类等价：附魔模型 + 可选图标覆盖。</summary>
public sealed record EnchantmentAssetProfile(string? IconPath = null)
{
	public static EnchantmentAssetProfile Empty { get; } = new();
}

public interface IModEnchantmentAssetOverrides
{
	EnchantmentAssetProfile AssetProfile => EnchantmentAssetProfile.Empty;
	string? CustomIconPath => AssetProfile.IconPath;
}

public abstract class ModEnchantmentTemplate : EnchantmentModel, IModEnchantmentAssetOverrides
{
	public virtual EnchantmentAssetProfile AssetProfile => EnchantmentAssetProfile.Empty;

	public virtual string? CustomIconPath => AssetProfile.IconPath;
}
