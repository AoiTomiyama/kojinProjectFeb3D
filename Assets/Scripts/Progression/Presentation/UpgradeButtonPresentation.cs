using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UpgradeButtonPresentation : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private TextMeshProUGUI _primaryLabel;
    [SerializeField] private TextMeshProUGUI _primaryValue;
    [SerializeField] private TextMeshProUGUI _secondaryLabel;
    [SerializeField] private TextMeshProUGUI _secondaryValue;
    private Action<int> _onSelected;
    private readonly List<GameObject> _additionalColumns = new List<GameObject>();
    public UpgradeDefinitionConfiguration Definition { get; private set; }

    public void Bind(UpgradeDefinitionConfiguration definition, Action<int> onSelected)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        if (_title == null || _primaryLabel == null || _primaryValue == null || _secondaryLabel == null || _secondaryValue == null)
            throw new InvalidOperationException($"{name}: 強化ボタンの表示参照が設定されていません。");
        Definition = definition;
        // Prefabの既存onClickからUpgradeを1回呼ぶ。再Bindで購読を追加しない。
        _onSelected = onSelected;
        _title.text = definition.Title;
        _primaryLabel.text = definition.PrimaryLabel;
        _primaryValue.text = definition.PrimaryValue;
        _secondaryLabel.text = definition.SecondaryLabel;
        _secondaryValue.text = definition.SecondaryValue;
        foreach (var column in _additionalColumns)
        {
            column.SetActive(false);
            column.transform.SetParent(null);
            Destroy(column);
        }
        _additionalColumns.Clear();
        foreach (var column in definition.AdditionalColumns)
        {
            // 旧ボタンの追加表示列も同じPrefabの列を再利用する。項目ごとのUI配置は不要。
            var label = Instantiate(_primaryLabel, _primaryLabel.transform.parent, false);
            var value = label.GetComponentInChildren<TextMeshProUGUI>();
            foreach (var child in label.GetComponentsInChildren<TextMeshProUGUI>()) if (child != label) value = child;
            label.text = column.Label;
            value.text = column.Value;
            _additionalColumns.Add(label.gameObject);
        }
    }
    public void Unbind() => _onSelected = null;
    public void Upgrade()
    {
        if (isActiveAndEnabled && Definition != null) _onSelected?.Invoke(Definition.Id);
    }
}
