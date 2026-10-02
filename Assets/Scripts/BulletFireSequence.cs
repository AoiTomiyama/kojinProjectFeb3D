using System;
using UnityEngine;

/// <summary>拡散角をUnityの向きへ変換し、プールから得た弾を発射する共通手順。</summary>
public static class BulletFireSequence
{
    public static void Fire(BulletObjectPoolManager poolManager, BulletTypeEnum bulletType,
        BulletParameter parameter, Transform muzzle, Vector3 forward, int requestedShots,
        float spreadAngle, WeaponAmmoState ammo, Action<int> onAmmoChanged = null)
    {
        int shotCount = BulletSpread.GetShotCount(requestedShots, ammo.RemainingAmmo);
        for (int i = 0; i < shotCount; i++)
        {
            var bullet = poolManager.Get(bulletType);
            bullet.Parameter = parameter;
            bullet.transform.position = muzzle.position;
            float angle = BulletSpread.GetAngle(requestedShots, spreadAngle, i);
            bullet.transform.forward = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            // 速度と効果音の初期化が向きを参照するため、配置後に呼ぶ。
            bullet.OnGetFromPool();
            ammo.TryConsumeShot();
            onAmmoChanged?.Invoke(ammo.RemainingAmmo);
        }
    }
}
