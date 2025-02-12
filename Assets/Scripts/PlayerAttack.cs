using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField, Header("最大弾数")] private int _maxBulletCount;
    [SerializeField, Header("発射間隔")] private float _coolDown;
    [SerializeField, Header("再装填時間")] private float _reloadTime;
    [SerializeField, Header("弾の初期値")] private BulletParameter _bulletParameter;

    private BulletObjectPoolManager _poolManager;
    private CancellationTokenSource _cts;
    private int _remainBulletCount;
    private bool _isPressedShootButton;
    private bool _isEnableToShoot = true;

    void Start()
    {
        _poolManager = FindAnyObjectByType<BulletObjectPoolManager>();
        _remainBulletCount = _maxBulletCount;
        _cts = new CancellationTokenSource();
    }
    void Update()
    {
        if (Input.GetButtonDown("Fire")) _isPressedShootButton = true;
        if (Input.GetButtonUp("Fire")) _isPressedShootButton = false;

        if (_isPressedShootButton && _isEnableToShoot)
        {
            Shoot();

            _remainBulletCount--;
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
        var bullet = _poolManager.Get(BulletTypeEnum.PlayerBullet);
        bullet.Parameter = _bulletParameter;
        bullet.gameObject.transform.position = transform.position;
        bullet.gameObject.transform.forward = transform.parent.forward;
        // パラメーターを設定してから初期化処理を行う。
        bullet.OnGetFromPool();
    }
}
