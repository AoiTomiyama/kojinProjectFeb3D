using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class BulletShotBehaviour : PooledAttackBase
{
    private CancellationTokenSource _cts;
    private Rigidbody _rb;
    private int _hitCount;
    [SerializeField, Header("衝突時のエフェクト")] 
    private GameObject _hitParticle;
    [SerializeField, Header("ダメージ表記")]
    private GameObject _damageText;
    [SerializeField]
    private AudioClip _shootClip;
    private AudioSource _aus;
    public override void OnInitialize()
    {
        _rb = GetComponent<Rigidbody>();
        _aus = GameObject.Find("SE").GetComponent<AudioSource>();
    }
    public override void OnGetFromPool()
    {
        _aus.PlayOneShot(_shootClip);
        
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
        Instantiate(_hitParticle, transform.position, Quaternion.identity);
        _hitCount++;
        if (_hitCount > Parameter.RicochetCount)
        {
            OnReturnToPool?.Invoke();
        }
        if (collision.gameObject.TryGetComponent<IDamageable>(out var component))
        {
            component.Damage(Parameter.Damage);
            var text = Instantiate(_damageText, transform.position, Quaternion.identity).GetComponent<TextMeshPro>();
            text.text = Parameter.Damage.ToString();
        }
    }
}
