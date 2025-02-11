using System;

/// <summary>
/// 弾のパラメーター。威力や速度の値を持つ。
/// </summary>
[Serializable]
public struct BulletParameter
{
    public int Damage;
    public float Speed;
    public float Duration;
    public int PiercingCount;
    public int RicochetCount;
}
