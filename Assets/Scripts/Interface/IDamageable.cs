/// <summary>
/// ダメージを受ける物であることを保証するインターフェイス
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 何かしらからダメージを受けた際に呼び出される関数
    /// </summary>
    /// <param name="damageAmount">ダメージの値</param>
    public void Damage(int damageAmount);
}
