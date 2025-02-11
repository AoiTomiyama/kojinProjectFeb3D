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
    /// データベースから取得する。
    /// </summary>
    /// <param name="type">要求する種類</param>
    /// <returns>要求と一致したプレハブ</returns>
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
