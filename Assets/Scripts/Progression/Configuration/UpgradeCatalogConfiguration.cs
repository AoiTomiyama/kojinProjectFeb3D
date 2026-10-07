using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>強化候補の一覧。UIの階層や並びをIDとして利用しない。</summary>
[CreateAssetMenu(menuName = "Game/Upgrade Catalog")]
public sealed class UpgradeCatalogConfiguration : ScriptableObject
{
    [SerializeField] private UpgradeDefinitionConfiguration[] _definitions = Array.Empty<UpgradeDefinitionConfiguration>();

    public UpgradeDefinitionConfiguration[] CreateSnapshot()
    {
        if (_definitions == null) throw new InvalidOperationException($"{name}: 強化一覧がありません。");
        var ids = new HashSet<int>();
        foreach (var definition in _definitions)
        {
            if (definition == null || definition.Id <= 0 || !ids.Add(definition.Id))
                throw new InvalidOperationException($"{name}: 強化は参照あり・正の一意なIDが必要です。");
        }
        // 一覧の実行時操作で、共有アセットの配列を書き換えない。
        return (UpgradeDefinitionConfiguration[])_definitions.Clone();
    }
}
