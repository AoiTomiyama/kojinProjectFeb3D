using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField, Header("�ő呕�e��")] private int _maxBulletCount;
    [SerializeField, Header("�������ː�")] private int _synchronousBulletCount;
    [SerializeField, Header("�g�U�͈�"), Range(1, 180)] private int _spreadAngle;
    [SerializeField, Header("���ˊԊu")] private float _coolDown;
    [SerializeField, Header("�đ��U����")] private float _reloadTime;
    [SerializeField, Header("�e�̏����l")] private BulletParameter _bulletParameter;

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
            var bullet = _poolManager.Get(BulletTypeEnum.PlayerBullet);
            bullet.Parameter = _bulletParameter;
            bullet.gameObject.transform.position = transform.position;

            var angle = _spreadAngle / 2f - i * th;
            var dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.parent.forward;
            bullet.gameObject.transform.forward = dir;
            // �p�����[�^�[��ݒ肵�Ă��珉�����������s���B
            bullet.OnGetFromPool();

            _remainBulletCount--;
            if (_remainBulletCount == 0) return;
        }
    }
    private void OnDrawGizmos()
    {
        // �e�̔��˗\����
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
