using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIViewer : PlayerComponentBaseGameplay
{
    [SerializeField] private Image _healthImage;
    [SerializeField] private Image _reloadTimeImage;
    [SerializeField] private Image _coolDownTimeImage;
    [SerializeField] private TextMeshProUGUI _ammoText;
    [SerializeField] private TextMeshProUGUI _healthText;
    private PlayerCoreGameplay _player;
    private PlayerAttackGameplay _attack;
    private bool _isInitialized;

    private void Start()
    {
        // 表示先を検証してから購読し、設定欠落時に購読だけを残さない。
        if (_healthImage == null)
            throw new System.InvalidOperationException($"{name}: PlayerUIViewer._healthImage が設定されていません。");
        if (_reloadTimeImage == null)
            throw new System.InvalidOperationException($"{name}: PlayerUIViewer._reloadTimeImage が設定されていません。");
        if (_coolDownTimeImage == null)
            throw new System.InvalidOperationException($"{name}: PlayerUIViewer._coolDownTimeImage が設定されていません。");
        if (_ammoText == null)
            throw new System.InvalidOperationException($"{name}: PlayerUIViewer._ammoText が設定されていません。");
        if (_healthText == null)
            throw new System.InvalidOperationException($"{name}: PlayerUIViewer._healthText が設定されていません。");
        _player = Core;
        _attack = _player.Attack;
        if (_attack == null) throw new System.InvalidOperationException("PlayerUIViewer: PlayerAttackGameplay が見つかりません。");
        // 最初の OnEnable は他コンポーネントの Awake より先になり得るため、Start で参照を確定する。
        _isInitialized = true;
        OnEnable();
    }

    private void OnEnable()
    {
        if (!_isInitialized) return;
        _player.OnHealthChanged += RefreshHealth;
        _attack.OnAmmoCountChanged += RefreshAmmo;
        _attack.OnCoolDownBegin += BeginCooldown;
        _attack.OnReloadBegin += BeginReload;
        // 無効期間の通知を再送させず、状態の所有者から現在値を読み直す。
        RefreshHealth();
        RefreshAmmo(_attack.RemainBulletCount);
        RestoreWaitBars();
    }
    private void RefreshHealth()
    {
        _healthImage.fillAmount = _player.MaxHealth > 0 ? (float)_player.Health / _player.MaxHealth : 0f;
        _healthText.text = $"{_player.Health}/{_player.MaxHealth}";
    }

    private void RefreshAmmo(int value) => _ammoText.text = value.ToString();

    private void BeginCooldown(float time)
    {
        _coolDownTimeImage.DOKill();
        _coolDownTimeImage.fillAmount = 0f;
        _coolDownTimeImage.DOFillAmount(1f, time);
    }

    private void BeginReload(float time)
    {
        _reloadTimeImage.DOKill();
        _reloadTimeImage.fillAmount = 0f;
        _reloadTimeImage.DOFillAmount(1f, time).SetEase(Ease.Linear);
    }

    private void RestoreWaitBars()
    {
        _coolDownTimeImage.fillAmount = 1f;
        _reloadTimeImage.fillAmount = 1f;
        float remaining = _attack.RemainingWaitSeconds;
        if (remaining <= 0f) return;
        float elapsed = _attack.WaitDurationSeconds - remaining;
        if (_attack.WaitKind == WeaponWaitKindDomain.Reload)
        {
            _reloadTimeImage.fillAmount = elapsed / _attack.WaitDurationSeconds;
            _reloadTimeImage.DOFillAmount(1f, remaining).SetEase(Ease.Linear);
        }
        else if (_attack.WaitKind == WeaponWaitKindDomain.Cooldown)
        {
            // 通常の発射間隔バーと同じイージングを、待機全体の経過時間から再現する。
            _coolDownTimeImage.DOFillAmount(1f, _attack.WaitDurationSeconds)
                .From(0f).Goto(elapsed, true);
        }
    }

    private void OnDisable()
    {
        if (_player != null) _player.OnHealthChanged -= RefreshHealth;
        if (_attack != null)
        {
            _attack.OnAmmoCountChanged -= RefreshAmmo;
            _attack.OnCoolDownBegin -= BeginCooldown;
            _attack.OnReloadBegin -= BeginReload;
        }
        // 表示が止まる期間に古いTweenが動いたり、再購読後の演出と競合したりしないようにする。
        if (_coolDownTimeImage != null) _coolDownTimeImage.DOKill();
        if (_reloadTimeImage != null) _reloadTimeImage.DOKill();
    }
}
