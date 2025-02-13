using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class PlayerAttack : PlayerComponentBase
{
    [SerializeField, Header("ç≈ëÂëïíeêî")] private int _maxBulletCount;
    [SerializeField, Header("ìØéûî≠éÀêî")] private int _synchronousBulletCount;
    [SerializeField, Header("ägéUîÕàÕ"), Range(1, 180)] private int _spreadAngle;
    [SerializeField, Header("î≠éÀä‘äu")] private float _coolDown;
    [SerializeField, Header("çƒëïìUéûä‘")] private float _reloadTime;
    [SerializeField, Header("íeÇÃèâä˙íl")] private BulletParameter _bulletParameter;
    [SerializeField, Header("î≠éÀå˚")] private Transform _muzzle;

    private BulletObjectPoolManager _poolManager;
    private LevelUpSystemManager _lvUpManager;
    private CancellationTokenSource _cts;
    private int _remainBulletCount;
    private bool _isPressedShootButton;
    private bool _isEnableToShoot = true;

    public Action<int> OnAmmoCountChanged;
    public Action<float> OnReloadBegin;
    public Action<float> OnCoolDownBegin;
    
    public int DamageBoost { get; set; }
    public int RemainBulletCount 
    {
        get => _remainBulletCount;
        set
        {
            _remainBulletCount = value;
            OnAmmoCountChanged?.Invoke(value);
        }
    }

    public int MaxBulletCount 
    {
        get => _maxBulletCount;
        set
        {
            _maxBulletCount = value;
            _remainBulletCount = value;
        }
    }

    void Start()
    {
        _poolManager = FindAnyObjectByType<BulletObjectPoolManager>();
        _lvUpManager = FindAnyObjectByType<LevelUpSystemManager>();
        RemainBulletCount = MaxBulletCount;
        _cts = new CancellationTokenSource();
    }
    void Update()
    {
        if (Input.GetButtonDown("Fire")) _isPressedShootButton = true;
        if (Input.GetButtonUp("Fire")) _isPressedShootButton = false;

        if (_isPressedShootButton && _isEnableToShoot && !_lvUpManager.IsMenuActivated)
        {
            Shoot();

            _isEnableToShoot = false;
            CancellationToken token = _cts.Token;
            WaitShootCooldownAsync(token);
        }
    }
    public void ApplyPowerUp(PowerUpParameter powerUp)
    {
        Core.MaxHealth += powerUp.MaxHealthAdd;
        Core.MaxHealth = (int)(Core.MaxHealth * powerUp.MaxHealthMultiply);
        Core.Move.Speed *= powerUp.MoveSpeedMultiply;
        _bulletParameter.Damage += powerUp.DamageAdd;
        _bulletParameter.Damage = (int)(_bulletParameter.Damage * powerUp.DamageMultiply);
        _bulletParameter.RicochetCount += powerUp.RicochetAdd;
        _bulletParameter.Speed += powerUp.SpeedAdd;
        _bulletParameter.Speed *= powerUp.SpeedMultiply;
        _coolDown += powerUp.CoolTimeAdd;
        _coolDown *= powerUp.CoolTimeMultiply;
        _reloadTime += powerUp.ReloadTimeAdd;
        _reloadTime *= powerUp.ReloadTimeMultiply;
        MaxBulletCount += powerUp.MaxAmmoSizeAdd;
        _synchronousBulletCount += powerUp.SyncBulletAdd;
    }

    private async void WaitShootCooldownAsync(CancellationToken token)
    {
        if (RemainBulletCount <= 0)
        {
            OnReloadBegin?.Invoke(_reloadTime);
        }
        else
        {
            OnCoolDownBegin?.Invoke(_coolDown);
        }

        var waitTime = (RemainBulletCount <= 0) ? _reloadTime : _coolDown;
        bool isCancelled = await UniTask.Delay((int)(1000 * waitTime), cancellationToken: token)
            .SuppressCancellationThrow();
        if (isCancelled) return;
        
        if (RemainBulletCount <= 0) RemainBulletCount = MaxBulletCount;

        _isEnableToShoot = true;
    }
    private void OnDisable()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    private void Shoot()
    {
        var th = 1f * _spreadAngle / (_synchronousBulletCount + 1);

        for (int i = 1; i <= _synchronousBulletCount; i++)
        {
            var bullet = _poolManager.Get(BulletTypeEnum.PlayerBullet);
            bullet.Parameter = _bulletParameter;
            bullet.gameObject.transform.position = _muzzle.position;

            var angle = _spreadAngle / 2f - i * th;
            var dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            bullet.gameObject.transform.forward = dir;
            // ÉpÉâÉÅÅ[É^Å[Çê›íËÇµÇƒÇ©ÇÁèâä˙âªèàóùÇçsÇ§ÅB
            bullet.OnGetFromPool();

            RemainBulletCount--;
            if (RemainBulletCount == 0) return;
        }
    }
    private void OnDrawGizmos()
    {
        // íeÇÃî≠éÀó\ë™ê¸
        Gizmos.color = Color.yellow;
        float th = 1f * _spreadAngle / (_synchronousBulletCount + 1f);

        for (int i = 1; i <= _synchronousBulletCount; i++)
        {
            var angle = _spreadAngle / 2f - i * th;
            var dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            Gizmos.DrawLine(transform.position, transform.position + dir * 10);
        }
    }
}
