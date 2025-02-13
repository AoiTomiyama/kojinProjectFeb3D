using UnityEngine;
using UnityEngine.UI;

public class EnemyUIViewer : EnemyComponentBase
{
    [SerializeField] private Image _healthImage;
    private void Start()
    {
        Core.OnHealthChanged += () => _healthImage.fillAmount = 1f * Core.Health / Core.MaxHealth;
    }
}
