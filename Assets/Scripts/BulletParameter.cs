using System;
using UnityEngine;

/// <summary>
/// 弾のパラメーター。威力や速度の値を持つ。
/// </summary>
[Serializable]
public struct BulletParameter
{
    public int Damage;
    public float Speed;
    private float duration;
    public int PiercingCount;
    public int RicochetCount;

    public float Duration 
    {
        get => Mathf.Max(1f, duration); 
        set => duration = Mathf.Max(1f, value); 
    }
}
