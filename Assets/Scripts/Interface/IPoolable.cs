using System;

public interface IPoolable
{
    /// <summary>
    /// �I�u�W�F�N�g�v�[���ɖ߂��ۂɎ��s����f���Q�[�g
    /// </summary>
    public Action OnReturnToPool { get; set; }
}
