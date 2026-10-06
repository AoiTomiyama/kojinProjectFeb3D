using System;

/// <summary>同時発射数と残弾から、発射数と各弾の拡散角を決める。</summary>
public static class BulletSpreadCalculatorDomain
{
    public static int GetShotCount(int requestedShots, int remainingAmmo)
    {
        return Math.Min(Math.Max(0, requestedShots), Math.Max(0, remainingAmmo));
    }

    public static float GetAngle(int requestedShots, float spreadAngle, int shotIndex)
    {
        if (requestedShots <= 0 || shotIndex < 0 || shotIndex >= requestedShots)
            throw new ArgumentOutOfRangeException(nameof(shotIndex));

        // 残弾不足でも配置を詰めず、元の同時発射数に対する角度を保つ。
        return spreadAngle / 2f - (shotIndex + 1) * (spreadAngle / (requestedShots + 1f));
    }
}
