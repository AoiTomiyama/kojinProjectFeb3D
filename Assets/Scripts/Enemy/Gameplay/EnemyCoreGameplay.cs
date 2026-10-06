using System;
using UnityEngine;
using UnityEngine.Events;

public class EnemyCoreGameplay : MonoBehaviour, IDamageableDomain
{
    private Transform _target;
    private int _playerLayer;
    private int _playerLayerMask;
    public Action OnHealthChanged;
    private Action<int> OnDeath;
    public UnityEvent OnDied;

    [SerializeField, Header("死亡時のエフェクト")]
    private GameObject _deathEffect;

    [SerializeField, Header("倒したときの経験値量")]
    private int _expAmount;

    [SerializeField, Header("射程距離")] 
    private float _shootRange;

    [Header("最大体力")]
    public int MaxHealth;
    private int _health;
    public int Health
    {
        get => _health;
        set
        {
            _health = value;
            OnHealthChanged?.Invoke();
        }
    }
    public Transform Target { get => _target; }
    public int PlayerLayer { get => _playerLayer; }
    public int PlayerLayerMask { get => _playerLayerMask; }
    public float ShootRange { get => _shootRange; }

    void Start()
    {
        Health = MaxHealth;
        _target = SceneReferenceResolverInfrastructure.RequireUnique<PlayerCoreGameplay>(this).transform;
        _playerLayer = _target.gameObject.layer;
        // Physics.CheckSphere には番号ではなくビットマスクを渡す。
        _playerLayerMask = 1 << _playerLayer;
        
        var lvUpManager = SceneReferenceResolverInfrastructure.RequireUnique<LevelUpCoordinatorGameplay>(this);
        OnDeath += lvUpManager.GainExperience;
    }
    public void Damage(int damageAmount)
    {
        Health -= damageAmount;
        if (Health <= 0)
        {
            if (_deathEffect == null)
                throw new System.InvalidOperationException($"{name}: EnemyCoreGameplay._deathEffect が設定されていません。");
            OnDeath?.Invoke(_expAmount);
            OnDied?.Invoke();
            Instantiate(_deathEffect, transform.position, Quaternion.identity);
            gameObject.SetActive(false);
        }
    }
}
