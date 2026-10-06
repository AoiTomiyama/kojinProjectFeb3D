using System;
using UnityEngine;

/// <summary>拡散角をUnityの向きへ変換し、プールから得た弾を発射する共通手順。</summary>
public static class BulletFireSequence
{
    public static void Fire(BulletPoolInfrastructure poolManager, BulletTypeDomain bulletType,
        BulletParametersConfiguration parameter, Transform muzzle, Vector3 forward, int requestedShots,
        float spreadAngle, WeaponAmmoStateDomain ammo, Action<int> onAmmoChanged = null)
    {
        int shotCount = BulletSpreadCalculatorDomain.GetShotCount(requestedShots, ammo.RemainingAmmo);
        for (int i = 0; i < shotCount; i++)
        {
            var bullet = poolManager.Get(bulletType);
            bullet.Parameter = parameter;
            bullet.transform.position = muzzle.position;
            float angle = BulletSpreadCalculatorDomain.GetAngle(requestedShots, spreadAngle, i);
            bullet.transform.forward = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            // 速度と効果音の初期化が向きを参照するため、配置後に呼ぶ。
            bullet.OnGetFromPool();
            ammo.TryConsumeShot();
            onAmmoChanged?.Invoke(ammo.RemainingAmmo);
        }
    }
}
