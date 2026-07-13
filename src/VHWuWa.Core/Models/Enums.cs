namespace VHWuWa.Core.Models;

/// <summary>Loại gói phát hành.</summary>
public enum PackageType
{
    Translation,
    Mod,
    Font
}

/// <summary>Thao tác áp dụng cho từng file trong gói.</summary>
public enum FileOperation
{
    /// <summary>Ghi đè file đã tồn tại (có backup file cũ).</summary>
    Replace,
    /// <summary>Chỉ copy file mới (không đè file game gốc).</summary>
    Copy
}
