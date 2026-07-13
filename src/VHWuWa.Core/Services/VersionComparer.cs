namespace VHWuWa.Core.Services;

/// <summary>So sánh phiên bản dạng "x.y.z" (bỏ qua tiền tố 'v'). An toàn với chuỗi lỗi.</summary>
public static class VersionComparer
{
    public static int Compare(string a, string b)
    {
        var va = Parse(a);
        var vb = Parse(b);
        for (int i = 0; i < Math.Max(va.Length, vb.Length); i++)
        {
            int x = i < va.Length ? va[i] : 0;
            int y = i < vb.Length ? vb[i] : 0;
            if (x != y) return x.CompareTo(y);
        }
        return 0;
    }

    /// <summary>True nếu <paramref name="candidate"/> mới hơn <paramref name="current"/>.</summary>
    public static bool IsNewer(string candidate, string current) => Compare(candidate, current) > 0;

    private static int[] Parse(string v)
    {
        if (string.IsNullOrWhiteSpace(v)) return new[] { 0 };
        v = v.Trim().TrimStart('v', 'V');
        // cắt phần pre-release/build sau '-' hoặc '+'
        int cut = v.IndexOfAny(new[] { '-', '+' });
        if (cut >= 0) v = v[..cut];
        var parts = v.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var nums = new List<int>(parts.Length);
        foreach (var p in parts)
            nums.Add(int.TryParse(p, out var n) ? n : 0);
        return nums.Count == 0 ? new[] { 0 } : nums.ToArray();
    }
}
