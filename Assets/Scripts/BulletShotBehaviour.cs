using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BulletShotBehaviour : PooledAttackBase
{
    private CancellationTokenSource _cts;
    private Rigidbody _rb;
    private int _hitCount;
    public override void OnInitialize()
    {
        _rb = GetComponent<Rigidbody>();
    }
    public override void OnGetFromPool()
    {
        _cts = new CancellationTokenSource();
        CancellationToken token = _cts.Token;
        WaitAndDisposeSelfAsync(token);

        _rb.velocity = Parameter.Speed * transform.forward;
        _hitCount = 0;
    }
    private async void WaitAndDisposeSelfAsync(CancellationToken token)
    {
        var isCancelled = await UniTask.Delay((int)(1000 * Parameter.Duration), cancellationToken: token)
            .SuppressCancellationThrow();

        if (isCancelled) return;

        OnReturnToPool?.Invoke();
    }
    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
    private void OnCollisionEnter(Collision collision)
    {
        _hitCount++;
        if (_hitCount > Parameter.RicochetCount)
        {
            OnReturnToPool?.Invoke();
        }
        if (collision.gameObject.TryGetComponent<IDamageable>(out var component))
        {
            component.Damage(Parameter.Damage);
        }
    }
}
