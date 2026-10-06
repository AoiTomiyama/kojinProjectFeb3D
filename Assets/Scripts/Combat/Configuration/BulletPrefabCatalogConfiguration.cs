using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BulletPrefabCatalogConfiguration", menuName = "ScriptableObjects/BulletPrefabCatalogConfiguration")]
public class BulletPrefabCatalogConfiguration : ScriptableObject
{
    [Serializable]
    public struct BulletPrefabMappingConfiguration
    {
        public BulletTypeDomain Type;
        public GameObject Prefab;
    }

    public List<BulletPrefabMappingConfiguration> Mappings;
    /// <summary>
    /// データベースから取得する。
    /// </summary>
    /// <param name="type">要求する種類</param>
    /// <returns>要求と一致したプレハブ</returns>
    public GameObject GetGameObject(BulletTypeDomain type)
    {
        foreach (var mapping in Mappings)
        {
            if (mapping.Type == type)
                return mapping.Prefab;
        }
        return null;
    }
}
