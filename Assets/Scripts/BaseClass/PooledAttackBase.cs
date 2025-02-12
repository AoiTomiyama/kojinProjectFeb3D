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
    /// <summary>
    /// オブジェクトプールから取り出す際に実行する関数
    /// 構造体の変数を受け取る関係上、OnEnableの処理を任意のタイミングで行う必要性がある。
    /// </summary>
    public abstract void OnGetFromPool();
    /// <summary>
    /// オブジェクトプールに登録する際に実行する関数
    /// 実質的にStart関数と同義。
    /// </summary>
    public abstract void OnInitialize();
}
