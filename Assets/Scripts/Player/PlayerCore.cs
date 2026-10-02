using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayerCore : MonoBehaviour, IDamageable
{
    // 被弾時に攻撃力や移動速度を上げるためにアクセス可能にした。
    private PlayerMove _move;
    private PlayerAttack _attack;
    public PlayerMove Move { get => _move; }
    public PlayerAttack Attack { get => _attack; }

    public Action OnHealthChanged;
    public UnityEvent OnDied;

    [SerializeField, Header("死亡時のエフェクト")]
    private GameObject _deathEffect;

    [SerializeField] private PlayerInitialStats _initialStats;
    private int _maxHealth;
    private int _health;
    public PlayerInitialStats InitialStats => _initialStats;
    public int Health
    {
        get => _health;
        set
        {
            _health = value;
            OnHealthChanged?.Invoke();
        }
    }

    public int MaxHealth 
    {
        get => _maxHealth;
        set
        {
            if (_maxHealth != value)
            {
                // 最大体力の値が変更されたとき、体力を合わせる。
                _maxHealth = value;
                Health = value;
            }
        }
    }

    private void Awake()
    {
        if (_initialStats == null) throw new InvalidOperationException("PlayerCore: 初期能力値が設定されていません。");
        // ScriptableObject の値は変更せず、このプレイヤーだけの実行時値へ複製する。
        _maxHealth = _initialStats.MaxHealth;
        _move = GetComponent<PlayerMove>();
        _attack = GetComponentInChildren<PlayerAttack>();
    }

    private void Start()
    {
        Health = MaxHealth;
    }

    public void ApplyPowerUp(PowerUpParameter powerUp)
    {
        // 能力値ごとの変更を担当コンポーネントへ振り分ける。
        MaxHealth = (int)((MaxHealth + powerUp.MaxHealthAdd) * powerUp.MaxHealthMultiply);
        _move.Speed *= powerUp.MoveSpeedMultiply;
        _attack.ApplyPowerUp(powerUp);
    }

    public void Damage(int damageAmount)
    {
        Health -= damageAmount;
        if (Health <= 0)
        {
            Instantiate(_deathEffect, transform.position, Quaternion.identity);
            OnDied?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
