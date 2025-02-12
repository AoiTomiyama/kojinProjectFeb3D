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
    /// <summary>
    /// �I�u�W�F�N�g�v�[��������o���ۂɎ��s����֐�
    /// �\���̂̕ϐ����󂯎��֌W��AOnEnable�̏�����C�ӂ̃^�C�~���O�ōs���K�v��������B
    /// </summary>
    public abstract void OnGetFromPool();
    /// <summary>
    /// �I�u�W�F�N�g�v�[���ɓo�^����ۂɎ��s����֐�
    /// �����I��Start�֐��Ɠ��`�B
    /// </summary>
    public abstract void OnInitialize();
}
