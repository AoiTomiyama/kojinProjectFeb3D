using System;

public interface IPoolable
{
    /// <summary>
    /// オブジェクトプールに戻す際に実行するデリゲート
    /// </summary>
    public Action OnReturnToPool { get; set; }
}
