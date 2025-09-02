using System;
using System.Collections.Generic;
using System.Linq;

public record ProgressPoint(double Percentage, DateTime Time);

public record EstimationResult(
    bool Success,
    TimeSpan? Remaining,
    DateTime? Eta,
    double RatePerSecond,    // percentage per second (slope)
    string? ErrorMessage
);

public static class ProgressEstimator
{
    /// <summary>
    /// 用線性回歸根據進度樣本估算剩餘時間與 ETA。
    /// 回傳 Success = false 時表示無法估算（例如樣本太少或速率<=0）。
    /// </summary>
    public static EstimationResult EstimateRemaining(IEnumerable<ProgressPoint> samples)
    {
        if (samples == null) return new EstimationResult(false, null, null, 0, "samples is null");

        // 1) 先清理與排序（只保留 0..100 範圍內的點）
        var pts = samples
            .Where(p => !double.IsNaN(p.Percentage) && !double.IsInfinity(p.Percentage))
            .Select(p => new ProgressPoint(Math.Max(0, Math.Min(100, p.Percentage)), p.Time))
            .OrderBy(p => p.Time)
            .ToArray();

        if (pts.Length < 2)
            return new EstimationResult(false, null, null, 0, "need at least 2 samples to estimate");

        // 若最後一筆已經 >= 100%，直接回傳完成
        var last = pts.Last();
        if (last.Percentage >= 100.0)
            return new EstimationResult(true, TimeSpan.Zero, last.Time, double.PositiveInfinity, null);

        // 2) 轉換時間到秒（以第一個樣本為 t=0）
        var t0 = pts[0].Time;
        var xs = pts.Select(p => (p.Time - t0).TotalSeconds).ToArray(); // independent variable
        var ys = pts.Select(p => p.Percentage).ToArray();              // dependent variable

        // 3) 線性回歸 (least squares) 計算 slope b 與 intercept a
        double n = xs.Length;
        double sumX = xs.Sum();
        double sumY = ys.Sum();
        double sumXY = 0;
        double sumXX = 0;
        for (int i = 0; i < xs.Length; i++)
        {
            sumXY += xs[i] * ys[i];
            sumXX += xs[i] * xs[i];
        }

        double denom = (n * sumXX - sumX * sumX);
        if (Math.Abs(denom) < 1e-9)
        {
            // 幾乎垂直或時間差太小
            return new EstimationResult(false, null, null, 0, "insufficient time variation among samples");
        }

        double slope = (n * sumXY - sumX * sumY) / denom; // b
        double intercept = (sumY - slope * sumX) / n;     // a

        // slope 表示 percent 增加量 / 秒
        // 若 slope 非正，代表沒有正向進度（停滯或倒退），無法估算
        if (!(slope > 1e-12))
        {
            // 嘗試 fallback：用首尾兩點的平均速率
            var deltaPerc = ys.Last() - ys.First();
            var deltaSec = xs.Last() - xs.First();
            double fallbackRate = (deltaSec > 0) ? (deltaPerc / deltaSec) : 0;
            if (!(fallbackRate > 1e-12))
            {
                return new EstimationResult(false, null, null, 0, "non-positive progress rate (can't estimate)");
            }
            slope = fallbackRate;
            intercept = ys.First() - slope * xs.First();
        }

        // 4) 計算達到 100% 時的 t (seconds since t0)
        double tTo100 = (100.0 - intercept) / slope;

        // 若 tTo100 已經小於最後已知時間，則代表估算已經超過（可能數據雜訊），回報立即完成
        var lastX = xs.Last();
        if (tTo100 <= lastX)
        {
            return new EstimationResult(true, TimeSpan.Zero, t0.AddSeconds(tTo100), slope, null);
        }

        double remainingSec = tTo100 - lastX;
        var remaining = TimeSpan.FromSeconds(remainingSec);
        var eta = t0.AddSeconds(tTo100);

        return new EstimationResult(true, remaining, eta, slope, null);
    }








    #region 時間小數點處理

    // 簡單版：直接去掉小數點之後的部分
    public static string TruncateSimple(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        int idx = s.IndexOf('.');
        return idx >= 0 ? s.Substring(0, idx) : s;
    }

    // 穩健版：嘗試用 TimeSpan 解析（支援超過 24 小時），失敗時回 fallback
    public static string TruncateRobust(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;

        if (TimeSpan.TryParse(s, out var ts))
        {
            // 若想把小時顯示為至少兩位數（但若超過 99 也會完整顯示）
            var hours = (long)ts.TotalHours; // 用 TotalHours 以處理超過 24hr 的情況
            return $"{hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        // fallback：若解析失敗，採用簡單切割
        return TruncateSimple(s);
    }

    #endregion
}
