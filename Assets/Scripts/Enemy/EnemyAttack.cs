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

    private BulletObjectPoolManager _poolManager;
    private CancellationTokenSource _cts;
    private int _remainBulletCount;
    private bool _isEnableToShoot = true;

    public int DamageBoost { get; set; }

    void Start()
    {
        _poolManager = FindAnyObjectByType<BulletObjectPoolManager>();
        _remainBulletCount = _maxBulletCount;
        _cts = new CancellationTokenSource();
    }
    void Update()
    {
        var isPlayerInRange = Physics.CheckSphere(transform.position, Core.ShootRange, Core.PlayerLayerIndex);

        // 範囲内にプレイヤーが存在するかどうか
        if (!isPlayerInRange) return;

        // クールダウンを終えているか
        if (!_isEnableToShoot) return;

        // レイキャストを飛ばし、その命中先にプレイヤーがいたか
        var dir = (Core.Target.position - transform.position).normalized;
        var ray = new Ray(transform.position, dir);
        if (Physics.Raycast(ray, out var hit, Core.ShootRange) && hit.collider.gameObject.layer == Core.PlayerLayerMask)
        {
            Shoot();

            _isEnableToShoot = false;
            CancellationToken token = _cts.Token;
            WaitShootCooldownAsync(token);
        }
    }

    private async void WaitShootCooldownAsync(CancellationToken token)
    {
        var waitTime = (_remainBulletCount <= 0) ? _reloadTime : _coolDown;
        bool isCancelled = await UniTask.Delay((int)(1000 * waitTime), cancellationToken: token)
            .SuppressCancellationThrow();
        if (isCancelled) return;

        if (_remainBulletCount <= 0) _remainBulletCount = _maxBulletCount;

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
            var bullet = _poolManager.Get(BulletTypeEnum.EnemyBullet);
            bullet.Parameter = _bulletParameter;
            bullet.gameObject.transform.position = transform.position;

            var angle = _spreadAngle / 2f - i * th;
            var dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.parent.forward;
            bullet.gameObject.transform.forward = dir;
            // パラメーターを設定してから初期化処理を行う。
            bullet.OnGetFromPool();

            _remainBulletCount--;
            if (_remainBulletCount == 0) return;
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
            var dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.parent.forward;
            Gizmos.DrawLine(transform.position, transform.position + dir * 10);
        }
    }
}
