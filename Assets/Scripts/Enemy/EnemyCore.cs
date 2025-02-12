using UnityEngine;

public class EnemyCore : MonoBehaviour, IDamageable
{
    public int MaxHealth;
    private int _health;
    private Transform _target;
    private int _playerLayer;
    private int _playerLayerIndex;
    [SerializeField, Header("ŽË’ö‹——£")] private float _shootRange;

    public int Health { get => _health; set => _health = value; }
    public Transform Target { get => _target; }
    public int PlayerLayerMask { get => _playerLayer; }
    public int PlayerLayerIndex { get => _playerLayerIndex; }
    public float ShootRange { get => _shootRange; }

    void Start()
    {
        _health = MaxHealth;
        _target = FindAnyObjectByType<PlayerCore>().transform;
        _playerLayer = _target.gameObject.layer;
        _playerLayerIndex = 1 << _playerLayer;
    }
    public void Damage(int damageAmount)
    {
        _health -= damageAmount;
    }
}
