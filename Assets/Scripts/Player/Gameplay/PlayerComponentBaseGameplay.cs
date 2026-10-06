using UnityEngine;

public class PlayerComponentBaseGameplay : MonoBehaviour
{
    private PlayerCoreGameplay _core;
    private bool _hasResolvedCore;
    protected PlayerCoreGameplay Core
    {
        get
        {
            // 破棄時は取得済みの参照を返し、破棄されたCoreを再検索しない。
            if (!_hasResolvedCore)
            {
                _core = GetComponent<PlayerCoreGameplay>();
                if (_core == null) throw new System.InvalidOperationException($"{name}: PlayerCoreGameplay が同じGameObjectに必要です。");
                _hasResolvedCore = true;
            }
            return _core;
        }
    }
}
