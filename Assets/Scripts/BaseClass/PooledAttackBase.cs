using System;
using UnityEngine;

/// <summary>
/// �G�⎩�@���甭�˂����e�̊��N���X�B
/// </summary>
public abstract class PooledAttackBase : MonoBehaviour
{
    /// <summary>
    /// �I�u�W�F�N�g�v�[���ɖ߂��ۂɎ��s����f���Q�[�g
    /// </summary>
    public Action OnReturnToPool { get; set; }
    /// <summary>
    /// �e�̃p�����[�^�[
    /// </summary>
    public BulletParameter Parameter { get; set; }
}
