using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIViewer : PlayerComponentBase
{
    [SerializeField] private Image _healthImage;
    [SerializeField] private Image _reloadTimeImage;
    [SerializeField] private Image _coolDownTimeImage;
    [SerializeField] private TextMeshProUGUI _ammoText;
    [SerializeField] private TextMeshProUGUI _healthText;
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
        Core.OnHealthChanged += RefreshHealth;
        // PlayerCore.Start より後に実行されても初期体力を表示できるようにする。
        RefreshHealth();

        var attack = Core.Attack;
        if (attack == null) throw new System.InvalidOperationException("PlayerUIViewer: PlayerAttack が見つかりません。");
        attack.OnAmmoCountChanged += value => _ammoText.text = value.ToString();
        // 攻撃処理の Start が先でも、通知を取り逃した初期弾数を表示する。
        _ammoText.text = attack.RemainBulletCount.ToString();
        attack.OnCoolDownBegin += time =>
        {
            _coolDownTimeImage.fillAmount = 0;
            _coolDownTimeImage.DOFillAmount(1, time);
        };

        attack.OnReloadBegin += time =>
        {
            _reloadTimeImage.fillAmount = 0;
            _reloadTimeImage.DOFillAmount(1, time).SetEase(Ease.Linear);
        };
    }
    private void RefreshHealth()
    {
        _healthImage.fillAmount = Core.MaxHealth > 0 ? (float)Core.Health / Core.MaxHealth : 0f;
        _healthText.text = $"{Core.Health}/{Core.MaxHealth}";
    }

    private void OnDestroy()
    {
        if (Core != null) Core.OnHealthChanged -= RefreshHealth;
    }
}
