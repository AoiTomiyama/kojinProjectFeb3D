using UnityEngine;

public class PlayerCore : MonoBehaviour, IDamageable
{
    // ��e���ɍU���͂�ړ����x���グ�邽�߂ɃA�N�Z�X�\�ɂ����B
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
