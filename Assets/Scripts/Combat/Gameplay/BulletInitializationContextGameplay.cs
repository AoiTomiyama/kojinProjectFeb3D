using UnityEngine;

/// <summary>プール生成時に派生弾へ渡すシーンの依存。音声を使わない弾は音源を省略できる。</summary>
public readonly struct BulletInitializationContextGameplay
{
    public AudioSource AudioSource { get; }

    public BulletInitializationContextGameplay(AudioSource audioSource)
    {
        AudioSource = audioSource;
    }
}
