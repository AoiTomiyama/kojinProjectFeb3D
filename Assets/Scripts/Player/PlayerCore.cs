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

    [SerializeField, Header("最大体力")]
    private int _maxHealth;
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

    public int MaxHealth 
    {
        get => _maxHealth;
        set
        {
            _maxHealth = value;
            _health = value;
        }
    }

    private void Start()
    {
        Health = MaxHealth;
        _move = GetComponent<PlayerMove>();
        _attack = GetComponentInChildren<PlayerAttack>();
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
