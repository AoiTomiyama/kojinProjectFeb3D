using System;
using UnityEngine;

/// <summary>
/// 弾のパラメーター。威力や速度の値を持つ。
/// </summary>
[Serializable]
public struct BulletParameter
{
    [Header("弾の威力")]
    public int Damage;

    [Header("弾の速度")]
    public float Speed;

    [SerializeField, Header("弾の滞在時間"), Range(1f, 100f)]
    private float duration;

    [Header("弾の反射回数")]
    public int RicochetCount;

    public float Duration 
    {
        get => duration; 
        set => duration = Mathf.Max(1f, value); 
    }
}
