using UnityEngine;
using UnityEngine.UI;

public class EnemyUIViewer : EnemyComponentBase
{
    [SerializeField] private Image _healthImage;
    private void Start()
    {
        if (_healthImage == null)
            throw new System.InvalidOperationException($"{name}: EnemyUIViewer._healthImage が設定されていません。");
        Core.OnHealthChanged += () => _healthImage.fillAmount = 1f * Core.Health / Core.MaxHealth;
    }
}
