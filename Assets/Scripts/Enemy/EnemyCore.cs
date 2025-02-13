using System;
using UnityEngine;
using UnityEngine.Events;

public class EnemyCore : MonoBehaviour, IDamageable
{
    private Transform _target;
    private int _playerLayer;
    private int _playerLayerIndex;
    public Action OnHealthChanged;
    private Action<int> OnDeath;
    public UnityEvent OnDied;

    [SerializeField, Header("���S���̃G�t�F�N�g")]
    private GameObject _deathEffect;

    [SerializeField, Header("�|�����Ƃ��̌o���l��")]
    private int _expAmount;

    [SerializeField, Header("�˒�����")] 
    private float _shootRange;

    [Header("�ő�̗�")]
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
    public int PlayerLayerMask { get => _playerLayer; }
    public int PlayerLayerIndex { get => _playerLayerIndex; }
    public float ShootRange { get => _shootRange; }

    void Start()
    {
        Health = MaxHealth;
        _target = FindAnyObjectByType<PlayerCore>().transform;
        _playerLayer = _target.gameObject.layer;
        _playerLayerIndex = 1 << _playerLayer;
        
        var lvUpManager = FindAnyObjectByType<LevelUpSystemManager>();
        OnDeath += lvUpManager.GainExperience;
    }
    public void Damage(int damageAmount)
    {
        Health -= damageAmount;
        if (Health <= 0)
        {
            OnDeath?.Invoke(_expAmount); 
            OnDied?.Invoke();
            Instantiate(_deathEffect, transform.position, Quaternion.identity);
            gameObject.SetActive(false);
        }
    }
}
