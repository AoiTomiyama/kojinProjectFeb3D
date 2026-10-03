using UnityEngine;
using UnityEngine.UI;

public class EnemyUIViewer : EnemyComponentBase
{
    [SerializeField] private Image _healthImage;
    private EnemyCore _enemy;
    private bool _isInitialized;

    private void Start()
    {
        if (_healthImage == null)
            throw new System.InvalidOperationException($"{name}: EnemyUIViewer._healthImage が設定されていません。");
        // 初回は全コンポーネントの Awake 後に参照を確定する。
        _enemy = Core;
        _isInitialized = true;
        OnEnable();
    }

    private void OnEnable()
    {
        if (!_isInitialized) return;
        _enemy.OnHealthChanged += RefreshHealth;
        // Start の実行順や無効期間中の通知に依存せず、現在の体力を描画する。
        RefreshHealth();
    }

    private void RefreshHealth()
    {
        _healthImage.fillAmount = _enemy.MaxHealth > 0 ? (float)_enemy.Health / _enemy.MaxHealth : 0f;
    }

    private void OnDisable()
    {
        if (_enemy != null) _enemy.OnHealthChanged -= RefreshHealth;
    }
}
