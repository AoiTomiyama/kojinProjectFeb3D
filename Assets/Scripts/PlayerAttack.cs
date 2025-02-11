using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField, Header("�ő�e��")] private int _maxBulletCount;
    [SerializeField, Header("���ˊԊu")] private float _coolDown;
    [SerializeField, Header("�đ��U����")] private float _reloadTime;
    [SerializeField, Header("�e�̏����l")] private BulletParameter _bulletParameter;

    private BulletObjectPoolManager _poolManager;
    private CancellationTokenSource _cts = new();
    private int _remainBulletCount;
    private bool _isPressedShootButton;
    private bool _isEnableToShoot = true;

    void Start()
    {
        _poolManager = FindAnyObjectByType<BulletObjectPoolManager>();
        _remainBulletCount = _maxBulletCount;
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
            WaitShootCooldownAsync(_cts.Token);
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
    }
}
