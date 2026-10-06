using UnityEngine;

/// <summary>武器の初期値。プレイ中は変更せず、各攻撃コンポーネントが値を複製する。</summary>
[CreateAssetMenu(fileName = "WeaponDefinitionConfiguration", menuName = "Game/Weapon Definition")]
public sealed class WeaponDefinitionConfiguration : ScriptableObject
{
    [SerializeField] private int _maxBulletCount;
    [SerializeField] private int _synchronousBulletCount;
    [SerializeField, Range(1, 180)] private int _spreadAngle;
    [SerializeField] private float _coolDown;
    [SerializeField] private float _reloadTime;
    [SerializeField] private BulletParametersConfiguration _bulletParameter;

    public int MaxBulletCount => _maxBulletCount;
    public int SynchronousBulletCount => _synchronousBulletCount;
    public int SpreadAngle => _spreadAngle;
    public float CoolDown => _coolDown;
    public float ReloadTime => _reloadTime;
    public BulletParametersConfiguration BulletParametersConfiguration => _bulletParameter;
}
