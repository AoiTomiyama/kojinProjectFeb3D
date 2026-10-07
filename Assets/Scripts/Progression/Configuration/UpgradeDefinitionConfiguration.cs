using UnityEngine;
using System;

/// <summary>強化1項目の不変な設定。取得回数や選択状態は使用者側に保持する。</summary>
[CreateAssetMenu(menuName = "Game/Upgrade Definition")]
public sealed class UpgradeDefinitionConfiguration : ScriptableObject
{
    [Serializable]
    public struct DisplayColumnConfiguration
    {
        public string Label;
        public string Value;
    }
    [SerializeField] private int _id;
    [SerializeField] private string _title;
    [SerializeField] private string _primaryLabel;
    [SerializeField] private string _primaryValue;
    [SerializeField] private string _secondaryLabel;
    [SerializeField] private string _secondaryValue;
    [SerializeField] private DisplayColumnConfiguration[] _additionalColumns = Array.Empty<DisplayColumnConfiguration>();
    [SerializeField] private UpgradeParametersConfiguration _effect = new UpgradeParametersConfiguration
    {
        DamageMultiply = 1, SpeedMultiply = 1, MoveSpeedMultiply = 1,
        CoolTimeMultiply = 1, ReloadTimeMultiply = 1, MaxHealthMultiply = 1
    };
    [SerializeField] private bool _available = true;
    [SerializeField, Min(0)] private int _minimumLevel;

    public int Id => _id;
    public string Title => _title;
    public string PrimaryLabel => _primaryLabel;
    public string PrimaryValue => _primaryValue;
    public string SecondaryLabel => _secondaryLabel;
    public string SecondaryValue => _secondaryValue;
    public System.Collections.Generic.IReadOnlyList<DisplayColumnConfiguration> AdditionalColumns => _additionalColumns;
    public UpgradeParametersConfiguration Effect => _effect;
    public bool IsEligible(int level) => _available && level >= _minimumLevel;
}
