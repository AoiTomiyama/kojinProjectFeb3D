using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class PlayerAttack : PlayerComponentBase
{
    [SerializeField, Header("最大装弾数")] private int _maxBulletCount;
    [SerializeField, Header("同時発射数")] private int _synchronousBulletCount;
    [SerializeField, Header("拡散範囲"), Range(1, 180)] private int _spreadAngle;
    [SerializeField, Header("発射間隔")] private float _coolDown;
    [SerializeField, Header("再装填時間")] private float _reloadTime;
    [SerializeField, Header("弾の初期値")] private BulletParameter _bulletParameter;
    [SerializeField, Header("発射口")] private Transform _muzzle;

    private BulletObjectPoolManager _poolManager;
    private LevelUpSystemManager _lvUpManager;
    private CancellationTokenSource _cts;
    private WeaponAmmoState _ammo;
    private bool _isPressedShootButton;
    private bool _isInitialized;

    public Action<int> OnAmmoCountChanged;
    public Action<float> OnReloadBegin;
    public Action<float> OnCoolDownBegin;
    
    public int DamageBoost { get; set; }
    public int RemainBulletCount 
    {
        get => _ammo.RemainingAmmo;
        set
        {
            _ammo.SetRemainingAmmo(value);
            OnAmmoCountChanged?.Invoke(RemainBulletCount);
        }
    }

    public int MaxBulletCount 
    {
        get => _ammo.Capacity;
        set
        {
            _maxBulletCount = Mathf.Max(1, value);
            _ammo.SetCapacity(_maxBulletCount);
        }
    }

    private void Awake()
    {
        _ammo = new WeaponAmmoState(_maxBulletCount, _coolDown, _reloadTime);
    }

    private void OnEnable()
    {
        _cts = new CancellationTokenSource();
        // 無効化で中断した再装填・発射間隔は、再有効化後にやり直す。
        if (_isInitialized && !_ammo.IsReady) WaitShootCooldownAsync(_cts.Token);
    }
    void Start()
    {
        _poolManager = SceneReferenceResolver.RequireUnique<BulletObjectPoolManager>(this);
        _lvUpManager = SceneReferenceResolver.RequireUnique<LevelUpSystemManager>(this);
        RemainBulletCount = MaxBulletCount;
        _isInitialized = true;
    }
    void Update()
    {
        if (!_isInitialized) return;
        if (Input.GetButtonDown("Fire")) _isPressedShootButton = true;
        if (Input.GetButtonUp("Fire")) _isPressedShootButton = false;

        if (_isPressedShootButton && _ammo.IsReady && !_lvUpManager.IsMenuActivated)
        {
            Shoot();

            CancellationToken token = _cts.Token;
            WaitShootCooldownAsync(token);
        }
    }
    public void ApplyPowerUp(PowerUpParameter powerUp)
    {
        _bulletParameter.Damage += powerUp.DamageAdd;
        _bulletParameter.Damage = (int)(_bulletParameter.Damage * powerUp.DamageMultiply);
        _bulletParameter.RicochetCount += powerUp.RicochetAdd;
        _bulletParameter.Speed += powerUp.SpeedAdd;
        _bulletParameter.Speed *= powerUp.SpeedMultiply;
        _coolDown += powerUp.CoolTimeAdd;
        _coolDown *= powerUp.CoolTimeMultiply;
        _reloadTime += powerUp.ReloadTimeAdd;
        _reloadTime *= powerUp.ReloadTimeMultiply;
        _ammo.SetTimings(_coolDown, _reloadTime);
        MaxBulletCount += powerUp.MaxAmmoSizeAdd;
        _synchronousBulletCount += powerUp.SyncBulletAdd;

        OnAmmoCountChanged?.Invoke(RemainBulletCount);
    }

    private async void WaitShootCooldownAsync(CancellationToken token)
    {
        var waitKind = _ammo.StartWait();
        var waitTime = _ammo.PendingWaitSeconds;
        if (waitKind == WeaponWaitKind.Reload)
        {
            OnReloadBegin?.Invoke(waitTime);
        }
        else
        {
            OnCoolDownBegin?.Invoke(waitTime);
        }

        bool isCancelled = await UniTask.Delay((int)(1000 * waitTime), cancellationToken: token)
            .SuppressCancellationThrow();
        if (isCancelled) return;
        
        if (_ammo.CompleteWait()) OnAmmoCountChanged?.Invoke(RemainBulletCount);
    }
    private void OnDisable()
    {
        _isPressedShootButton = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private void Shoot()
    {
        BulletFireSequence.Fire(_poolManager, BulletTypeEnum.PlayerBullet, _bulletParameter,
            _muzzle, transform.forward, _synchronousBulletCount, _spreadAngle, _ammo,
            count => OnAmmoCountChanged?.Invoke(count));
    }
    private void OnDrawGizmos()
    {
        // 弾の発射予測線
        Gizmos.color = Color.yellow;
        for (int i = 0; i < _synchronousBulletCount; i++)
        {
            var angle = BulletSpread.GetAngle(_synchronousBulletCount, _spreadAngle, i);
            var dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            Gizmos.DrawLine(transform.position, transform.position + dir * 10);
        }
    }
}
