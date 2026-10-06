using System;

/// <summary>プレイヤーと敵に共通する残弾・発射待機・再装填の状態。</summary>
public sealed class WeaponAmmoStateDomain
{
    public int Capacity { get; private set; }
    public int RemainingAmmo { get; private set; }
    public bool IsReady { get; private set; } = true;
    public WeaponWaitKindDomain PendingWait { get; private set; }
    public float CooldownSeconds { get; private set; }
    public float ReloadSeconds { get; private set; }
    public float PendingWaitSeconds => PendingWait == WeaponWaitKindDomain.Reload ? ReloadSeconds :
        PendingWait == WeaponWaitKindDomain.Cooldown ? CooldownSeconds : 0f;

    public WeaponAmmoStateDomain(int capacity, float cooldownSeconds, float reloadSeconds)
    {
        SetCapacity(capacity);
        SetTimings(cooldownSeconds, reloadSeconds);
    }

    public void SetCapacity(int capacity)
    {
        Capacity = Math.Max(1, capacity);
        // 最大装弾数の強化時は、現行仕様に合わせて残弾も満タンにする。
        RemainingAmmo = Capacity;
    }

    public void SetRemainingAmmo(int count)
    {
        RemainingAmmo = Math.Max(0, Math.Min(Capacity, count));
    }

    public void SetTimings(float cooldownSeconds, float reloadSeconds)
    {
        CooldownSeconds = cooldownSeconds;
        ReloadSeconds = reloadSeconds;
    }

    public bool TryConsumeShot()
    {
        if (RemainingAmmo <= 0) return false;
        RemainingAmmo--;
        return true;
    }

    public WeaponWaitKindDomain StartWait()
    {
        // 無効化で中断した待機も、再有効化時に現在の残弾から最初から判定する。
        PendingWait = RemainingAmmo <= 0 ? WeaponWaitKindDomain.Reload : WeaponWaitKindDomain.Cooldown;
        IsReady = false;
        return PendingWait;
    }

    public bool CompleteWait()
    {
        if (PendingWait == WeaponWaitKindDomain.None)
            throw new InvalidOperationException("開始していない待機は完了できません。");

        bool reloaded = RemainingAmmo <= 0;
        if (reloaded) RemainingAmmo = Capacity;
        PendingWait = WeaponWaitKindDomain.None;
        IsReady = true;
        return reloaded;
    }
}
