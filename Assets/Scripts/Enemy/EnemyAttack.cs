using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class EnemyAttack : EnemyComponentBase
{
    [SerializeField, Header("最大装弾数")] private int _maxBulletCount;
    [SerializeField, Header("同時発射数")] private int _synchronousBulletCount;
    [SerializeField, Header("拡散範囲"), Range(1, 180)] private int _spreadAngle;
    [SerializeField, Header("発射間隔")] private float _coolDown;
    [SerializeField, Header("再装填時間")] private float _reloadTime;
    [SerializeField, Header("弾の初期値")] private BulletParameter _bulletParameter;
    [SerializeField, Header("発射口")] private Transform _muzzle;

    private BulletObjectPoolManager _poolManager;
    private CancellationTokenSource _cts;
    private WeaponAmmoState _ammo;
    private bool _isInitialized;

    public int DamageBoost { get; set; }

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
        _isInitialized = true;
    }
    void Update()
    {
        if (!_isInitialized) return;
        var isPlayerInRange = Physics.CheckSphere(transform.position, Core.ShootRange, Core.PlayerLayerMask);

        // 範囲内にプレイヤーが存在するかどうか
        if (!isPlayerInRange) return;

        // クールダウンを終えているか
        if (!_ammo.IsReady) return;

        // レイキャストを飛ばし、その命中先にプレイヤーがいたか
        var dir = (Core.Target.position - _muzzle.position).normalized;
        var ray = new Ray(_muzzle.position, dir);
        if (Physics.Raycast(ray, out var hit, Core.ShootRange) && hit.collider.gameObject.layer == Core.PlayerLayer)
        {
            Shoot();

            CancellationToken token = _cts.Token;
            WaitShootCooldownAsync(token);
        }
    }

    private async void WaitShootCooldownAsync(CancellationToken token)
    {
        _ammo.StartWait();
        var waitTime = _ammo.PendingWaitSeconds;
        bool isCancelled = await UniTask.Delay((int)(1000 * waitTime), cancellationToken: token)
            .SuppressCancellationThrow();
        if (isCancelled) return;

        _ammo.CompleteWait();
    }
    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private void Shoot()
    {
        BulletFireSequence.Fire(_poolManager, BulletTypeEnum.EnemyBullet, _bulletParameter,
            _muzzle, transform.forward, _synchronousBulletCount, _spreadAngle, _ammo);
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
