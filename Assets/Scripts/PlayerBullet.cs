using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class PlayerBullet : PooledAttackBase
{
    private CancellationTokenSource _cts;
    private void OnEnable()
    {
        _cts = new();
        CancellationToken token = _cts.Token;
        WaitAndDisposeSelfAsync(token);
    }
    private void Update()
    {
        transform.position += Parameter.Speed * Time.deltaTime * transform.forward;
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
    private void OnTriggerEnter(Collider other)
    {
        
    }
}
