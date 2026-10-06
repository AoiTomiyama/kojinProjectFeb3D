using UnityEngine;

public class UpgradeButtonBehaviour : MonoBehaviour
{
    [SerializeField]
    UpgradeParametersConfiguration _powerUp;

    LevelUpCoordinatorGameplay _lvUpManager;
    private void Start()
    {
        _lvUpManager = SceneReferenceResolverInfrastructure.RequireUnique<LevelUpCoordinatorGameplay>(this);
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
