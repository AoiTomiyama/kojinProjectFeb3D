using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class EnemyAttack : EnemyComponentBase
{
    [SerializeField] private WeaponDefinition _definition;
    [SerializeField, Header("発射口")] private Transform _muzzle;

    private int _synchronousBulletCount;
    private int _spreadAngle;
    private BulletParameter _bulletParameter;

    private BulletObjectPoolManager _poolManager;
    private CancellationTokenSource _cts;
    private WeaponAmmoState _ammo;
    private bool _isInitialized;

    public int DamageBoost { get; set; }

    private void Awake()
    {
        if (_definition == null)
            throw new System.InvalidOperationException($"{name}: EnemyAttack._definition が設定されていません。");
        // 同じ定義を使う敵同士でも、残弾と射撃状態は個体ごとに持つ。
        _synchronousBulletCount = _definition.SynchronousBulletCount;
        _spreadAngle = _definition.SpreadAngle;
        _bulletParameter = _definition.BulletParameter;
        _ammo = new WeaponAmmoState(_definition.MaxBulletCount, _definition.CoolDown, _definition.ReloadTime);
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
            throw new System.InvalidOperationException($"{name}: EnemyAttack._muzzle が設定されていません。");
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
        int shots = Application.isPlaying ? _synchronousBulletCount : _definition != null ? _definition.SynchronousBulletCount : 0;
        int spread = Application.isPlaying ? _spreadAngle : _definition != null ? _definition.SpreadAngle : 0;
        for (int i = 0; i < shots; i++)
        {
            var angle = BulletSpread.GetAngle(shots, spread, i);
            var dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
            Gizmos.DrawLine(transform.position, transform.position + dir * 10);
        }
    }
}
