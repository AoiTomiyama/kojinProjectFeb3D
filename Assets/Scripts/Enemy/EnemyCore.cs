using UnityEngine;

public class EnemyCore : MonoBehaviour, IDamageable
{
    public int MaxHealth;
    private int _health;
    private Transform _target;

    public int Health { get => _health; set => _health = value; }
    public Transform Target { get => _target; set => _target = value; }

    void Start()
    {
        _target = FindAnyObjectByType<PlayerCore>().transform;
    }
    public void Damage(int damageAmount)
    {
        _health -= damageAmount;
    }
}
