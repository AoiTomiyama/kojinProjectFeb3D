using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnumToObjectDatabase", menuName = "ScriptableObjects/EnumToObjectDatabase")]
public class EnumToObjectDatabase : ScriptableObject
{
    [Serializable]
    public struct EnumToGameObjectPair
    {
        public BulletTypeEnum Type;
        public GameObject Prefab;
    }

    public List<EnumToGameObjectPair> Mappings;
    /// <summary>
    /// �f�[�^�x�[�X����擾����B
    /// </summary>
    /// <param name="type">�v��������</param>
    /// <returns>�v���ƈ�v�����v���n�u</returns>
    public GameObject GetGameObject(BulletTypeEnum type)
    {
        foreach (var mapping in Mappings)
        {
            if (mapping.Type == type)
                return mapping.Prefab;
        }
        return null;
    }
}
