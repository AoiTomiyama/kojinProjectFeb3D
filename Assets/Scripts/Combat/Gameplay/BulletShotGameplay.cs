using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class BulletShotGameplay : PooledAttackBaseGameplay
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
    public void SetAudioSource(AudioSource audioSource)
    {
        if (audioSource == null)
        {
            throw new System.ArgumentNullException(nameof(audioSource));
        }
        _aus = audioSource;
    }

    public override void OnInitialize()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null) throw new System.InvalidOperationException($"{name}: Rigidbody が同じGameObjectに必要です。");
    }
    public override void OnGetFromPool()
    {
        if (_aus == null)
        {
            throw new System.InvalidOperationException("BulletShotGameplay: 効果音用 AudioSource が初期化されていません。");
        }
        if (_shootClip == null)
            throw new System.InvalidOperationException($"{name}: BulletShotGameplay._shootClip が設定されていません。");
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
        // 返却後に再取得され、発射初期化前に破棄される場合も二重に破棄しない。
        _cts = null;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (_hitParticle == null)
            throw new System.InvalidOperationException($"{name}: BulletShotGameplay._hitParticle が設定されていません。");
        Instantiate(_hitParticle, transform.position, Quaternion.identity);
        _hitCount++;
        if (_hitCount > Parameter.RicochetCount)
        {
            OnReturnToPool?.Invoke();
        }
        if (collision.gameObject.TryGetComponent<IDamageableDomain>(out var component))
        {
            if (_damageText == null)
                throw new System.InvalidOperationException($"{name}: BulletShotGameplay._damageText が設定されていません。");
            component.Damage(Parameter.Damage);
            var textObject = Instantiate(_damageText, transform.position, Quaternion.identity);
            var text = textObject.GetComponent<TextMeshPro>();
            if (text == null) throw new System.InvalidOperationException($"{textObject.name}: TextMeshPro が必要です。");
            text.text = Parameter.Damage.ToString();
        }
    }
}
