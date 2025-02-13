using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCore : MonoBehaviour, IDamageable
{
    // ��e���ɍU���͂�ړ����x���グ�邽�߂ɃA�N�Z�X�\�ɂ����B
    private PlayerMove _move;
    private PlayerAttack _attack;
    public PlayerMove Move { get => _move; }
    public PlayerAttack Attack { get => _attack; }

    public Action OnHealthChanged;

    [Header("�ő�̗�")]
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
    }
}
