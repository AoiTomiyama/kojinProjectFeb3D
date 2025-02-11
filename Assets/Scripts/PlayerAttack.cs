using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField, Header("Å‘å’e”")] private int _maxBulletCount;
    [SerializeField, Header("”­ËŠÔŠu")] private float _coolDown;
    [SerializeField, Header("Ä‘•“UŠÔ")] private float _reloadTime;
    [SerializeField, Header("’e‚Ì‰Šú’l")] private BulletParameter _bulletParameter;

    private BulletObjectPoolManager _poolManager;
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
            //Shoot();
            Debug.Log("Pew!");

            _remainBulletCount--;
            _isEnableToShoot = false;
            WaitCooldown();
        }
    }

    private async void WaitCooldown()
    {
        if (_remainBulletCount > 0)
        {
            await UniTask.Delay((int)(1000 * _coolDown));
        }
        else
        {
            await UniTask.Delay((int)(1000 * _reloadTime));
            _remainBulletCount = _maxBulletCount;
        }
        _isEnableToShoot = true;
    }

    private void Shoot()
    {
        var bullet = _poolManager.Get(BulletTypeEnum.PlayerBullet);
        bullet.Parameter = _bulletParameter;
        bullet.gameObject.transform.position = transform.position;
    }
}
