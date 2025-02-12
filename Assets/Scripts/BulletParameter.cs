using System;
using UnityEngine;

/// <summary>
/// �e�̃p�����[�^�[�B�З͂⑬�x�̒l�����B
/// </summary>
[Serializable]
public struct BulletParameter
{
    public int Damage;
    public float Speed;
    [SerializeField, Range(1f, 100f)]
    private float duration;
    public int PiercingCount;
    public int RicochetCount;

    public float Duration 
    {
        get => Mathf.Max(1f, duration); 
        set => duration = Mathf.Max(1f, value); 
    }
}
