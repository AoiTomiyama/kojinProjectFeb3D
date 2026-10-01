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
    private int _remainBulletCount;
    private bool _isPressedShootButton;
    private bool _isEnableToShoot = true;
    private bool _isInitialized;

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
            _maxBulletCount = Mathf.Max(1, value);
            _remainBulletCount = _maxBulletCount;
        }
    }

    private void OnEnable()
    {
        _cts = new CancellationTokenSource();
        // 無効化で中断した再装填・発射間隔は、再有効化後にやり直す。
        if (_isInitialized && !_isEnableToShoot) WaitShootCooldownAsync(_cts.Token);
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

        OnAmmoCountChanged?.Invoke(_remainBulletCount);
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
        _isPressedShootButton = false;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
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
            // パラメーターを設定してから初期化処理を行う。
            bullet.OnGetFromPool();

            RemainBulletCount--;
            if (RemainBulletCount == 0) return;
        }
    }
    private void OnDrawGizmos()
    {
        // 弾の発射予測線
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
