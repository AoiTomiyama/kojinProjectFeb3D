using UnityEngine;

public class PlayerComponentBase : MonoBehaviour
{
    private PlayerCore _core;
    private bool _hasResolvedCore;
    protected PlayerCore Core
    {
        get
        {
            // 破棄時は取得済みの参照を返し、破棄されたCoreを再検索しない。
            if (!_hasResolvedCore)
            {
                _core = GetComponent<PlayerCore>();
                if (_core == null) throw new System.InvalidOperationException($"{name}: PlayerCore が同じGameObjectに必要です。");
                _hasResolvedCore = true;
            }
            return _core;
        }
    }
}
