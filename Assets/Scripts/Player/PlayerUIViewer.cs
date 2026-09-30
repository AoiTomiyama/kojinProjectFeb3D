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
        Core.OnHealthChanged += () =>
        {
            _healthImage.fillAmount = 1f * Core.Health / Core.MaxHealth;
            _healthText.text = $"{Core.MaxHealth}/{Core.Health}";
        };

        var attack = Core.Attack;
        if (attack == null) throw new System.InvalidOperationException("PlayerUIViewer: PlayerAttack ‚ªŒ©‚Â‚©‚è‚Ü‚¹‚ñB");
        attack.OnAmmoCountChanged += value => _ammoText.text = value.ToString();
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
}
