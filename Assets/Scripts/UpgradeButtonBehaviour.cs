using UnityEngine;

public class UpgradeButtonBehaviour : MonoBehaviour
{
    [SerializeField]
    PowerUpParameter _powerUp;

    LevelUpSystemManager _lvUpManager;
    private void Start()
    {
        _lvUpManager = FindAnyObjectByType<LevelUpSystemManager>();
    }
    public void Upgrade()
    {
        if (_lvUpManager.PickCount > 0)
        {
            gameObject.SetActive(false);
            _lvUpManager.PickCount--;
            _lvUpManager.ApplyPowerUp(_powerUp);
        }
    }
}
