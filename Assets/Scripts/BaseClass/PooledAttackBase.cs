using System;
using UnityEngine;

/// <summary>
/// 敵や自機から発射される弾の基底クラス。
/// </summary>
public abstract class PooledAttackBase : MonoBehaviour
{
    /// <summary>
    /// オブジェクトプールに戻す際に実行するデリゲート
    /// </summary>
    public Action OnReturnToPool { get; set; }
    /// <summary>
    /// 弾のパラメーター
    /// </summary>
    public BulletParameter Parameter { get; set; }
}
