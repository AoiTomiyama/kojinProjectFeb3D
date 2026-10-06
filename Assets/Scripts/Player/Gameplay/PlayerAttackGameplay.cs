using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class PlayerAttackGameplay : PlayerComponentBaseGameplay
{
    [SerializeField] private WeaponDefinitionConfiguration _definition;
    [SerializeField, Header("発射口")] private Transform _muzzle;

    private int _maxBulletCount;
    private int _synchronousBulletCount;
    private int _spreadAngle;
    private float _coolDown;
    private float _reloadTime;
    private BulletParametersConfiguration _bulletParameter;

    private BulletPoolInfrastructure _poolManager;
    private LevelUpCoordinatorGameplay _lvUpManager;
    private CancellationTokenSource _cts;
    private WeaponAmmoStateDomain _ammo;
    private bool _isPressedShootButton;
    private bool _isInitialized;
    private float _waitEndTime;

    // UIの再有効化時に待機表示を復元するための読み取り専用状態。待機の実行は攻撃側が所有する。
    public WeaponWaitKindDomain WaitKind => _ammo.PendingWait;
    public float WaitDurationSeconds { get; private set; }
    public float RemainingWaitSeconds => _ammo.IsReady ? 0f : Mathf.Max(0f, _waitEndTime - Time.time);

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
        if (_definition == null)
            throw new System.InvalidOperationException($"{name}: PlayerAttackGameplay._definition が設定されていません。");
        // 強化による変更は ScriptableObject に書き戻さず、この攻撃者だけに反映する。
        _maxBulletCount = _definition.MaxBulletCount;
        _synchronousBulletCount = _definition.SynchronousBulletCount;
        _spreadAngle = _definition.SpreadAngle;
        _coolDown = _definition.CoolDown;
        _reloadTime = _definition.ReloadTime;
        _bulletParameter = _definition.BulletParametersConfiguration;
        _ammo = new WeaponAmmoStateDomain(_maxBulletCount, _coolDown, _reloadTime);
    }

    private void OnEnable()
    {
        _cts = new CancellationTokenSource();
        // 無効化で中断した再装填・発射間隔は、再有効化後にやり直す。
        if (_isInitialized && !_ammo.IsReady) WaitShootCooldownAsync(_cts.Token);
    }
    void Start()
    {
        if (_muzzle == null)
            throw new System.InvalidOperationException($"{name}: PlayerAttackGameplay._muzzle が設定されていません。");
        _poolManager = SceneReferenceResolverInfrastructure.RequireUnique<BulletPoolInfrastructure>(this);
        _lvUpManager = SceneReferenceResolverInfrastructure.RequireUnique<LevelUpCoordinatorGameplay>(this);
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
    public void ApplyPowerUp(UpgradeParametersConfiguration powerUp)
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
        WaitDurationSeconds = waitTime;
        _waitEndTime = Time.time + waitTime;
        if (waitKind == WeaponWaitKindDomain.Reload)
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
        BulletFireSequenceGameplay.Fire(_poolManager, BulletTypeDomain.PlayerBullet, _bulletParameter,
            _muzzle, transform.forward, _synchronousBulletCount, _spreadAngle, _ammo,
            count => OnAmmoCountChanged?.Invoke(count));
    }
    private void OnDrawGizmos()
    {
        // 弾の発射予測線
        Gizmos.color = Color.yellow;
        int shots = Application.isPlaying ? _synchronousBulletCount : _definition != null ? _definition.SynchronousBulletCount : 0;
        int spread = Application.isPlaying ? _spreadAngle : _definition != null ? _definition.SpreadAngle : 0;
        for (int i = 0; i < shots; i++)
        {
            var angle = BulletSpreadCalculatorDomain.GetAngle(shots, spread, i);
            var dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            Gizmos.DrawLine(transform.position, transform.position + dir * 10);
        }
    }
}
