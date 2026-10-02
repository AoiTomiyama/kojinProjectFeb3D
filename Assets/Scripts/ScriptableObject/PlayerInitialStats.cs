using UnityEngine;

/// <summary>プレイヤーの初期能力値。シーンごとの設定を実行時状態の生成元にする。</summary>
[CreateAssetMenu(fileName = "PlayerInitialStats", menuName = "Game/Player Initial Stats")]
public sealed class PlayerInitialStats : ScriptableObject
{
    [SerializeField] private int _maxHealth;
    [SerializeField] private float _moveSpeed;

    public int MaxHealth => _maxHealth;
    public float MoveSpeed => _moveSpeed;
}
