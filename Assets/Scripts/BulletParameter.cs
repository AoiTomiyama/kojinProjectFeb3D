using System;
using UnityEngine;

/// <summary>
/// �e�̃p�����[�^�[�B�З͂⑬�x�̒l�����B
/// </summary>
[Serializable]
public struct BulletParameter
{
    [Header("�e�̈З�")]
    public int Damage;

    [Header("�e�̑��x")]
    public float Speed;

    [SerializeField, Header("�e�̑؍ݎ���"), Range(1f, 100f)]
    private float duration;

    [Header("�e�̔��ˉ�")]
    public int RicochetCount;

    public float Duration 
    {
        get => duration; 
        set => duration = Mathf.Max(1f, value); 
    }
}
