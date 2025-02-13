using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class PlayerAttack : PlayerComponentBase
{
    [SerializeField, Header("Å‘å‘•’e”")] private int _maxBulletCount;
    [SerializeField, Header("“¯”­Ë”")] private int _synchronousBulletCount;
    [SerializeField, Header("ŠgU”ÍˆÍ"), Range(1, 180)] private int _spreadAngle;
    [SerializeField, Header("”­ËŠÔŠu")] private float _coolDown;
    [SerializeField, Header("Ä‘•“UŠÔ")] private float _reloadTime;
    [SerializeField, Header("’e‚Ì‰Šú’l")] private BulletParameter _bulletParameter;

    private BulletObjectPoolManager _poolManager;
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

    void Start()
    {
        _poolManager = FindAnyObjectByType<BulletObjectPoolManager>();
        RemainBulletCount = _maxBulletCount;
        _cts = new CancellationTokenSource();
    }
    void Update()
    {
        if (Input.GetButtonDown("Fire")) _isPressedShootButton = true;
        if (Input.GetButtonUp("Fire")) _isPressedShootButton = false;

        if (_isPressedShootButton && _isEnableToShoot)
        {
            Shoot();

            _isEnableToShoot = false;
            CancellationToken token = _cts.Token;
            WaitShootCooldownAsync(token);
        }
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
        
        if (RemainBulletCount <= 0) RemainBulletCount = _maxBulletCount;

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
            bullet.gameObject.transform.position = transform.position;

            var angle = _spreadAngle / 2f - i * th;
            var dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.parent.forward;
            bullet.gameObject.transform.forward = dir;
            // ƒpƒ‰ƒ[ƒ^[‚ğİ’è‚µ‚Ä‚©‚ç‰Šú‰»ˆ—‚ğs‚¤B
            bullet.OnGetFromPool();

            RemainBulletCount--;
            if (RemainBulletCount == 0) return;
        }
    }
    private void OnDrawGizmos()
    {
        // ’e‚Ì”­Ë—\‘ªü
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
