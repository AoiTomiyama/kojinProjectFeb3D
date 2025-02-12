using UnityEngine;

public class PlayerCore : MonoBehaviour, IDamageable
{
    // 被弾時に攻撃力や移動速度を上げるためにアクセス可能にした。
    private PlayerMove _move;
    private PlayerAttack _attack;

    public int MaxHealth;
    private int _health;

    public int Health { get => _health; set => _health = value; }
    public PlayerMove Move { get => _move; }
    public PlayerAttack Attack { get => _attack; }

    private void Start()
    {
        _move = GetComponent<PlayerMove>();
        _attack = GetComponentInChildren<PlayerAttack>();
    }

    public void Damage(int damageAmount)
    {
        _health -= damageAmount;
    }
}
