using UnityEngine;

public class UpgradeButtonBehaviour : MonoBehaviour
{
    [SerializeField]
    PowerUpParameter _powerUp;

    LevelUpSystemManager _lvUpManager;
    private void Start()
    {
        _lvUpManager = SceneReferenceResolverInfrastructure.RequireUnique<LevelUpSystemManager>(this);
    }
    public void Upgrade()
    {
        if (_lvUpManager.TrySpendUpgradeChoice())
        {
            gameObject.SetActive(false);
            _lvUpManager.ApplyPowerUp(_powerUp);
        }
    }
}
