using UnityEngine;

public class EnemyComponentBaseGameplay : MonoBehaviour
{
    private EnemyCoreGameplay _core;
    private bool _hasResolvedCore;
    protected EnemyCoreGameplay Core
    {
        get
        {
            // 破棄時は取得済みの参照を返し、破棄されたCoreを再検索しない。
            if (!_hasResolvedCore)
            {
                _core = GetComponent<EnemyCoreGameplay>();
                if (_core == null) throw new System.InvalidOperationException($"{name}: EnemyCoreGameplay が同じGameObjectに必要です。");
                _hasResolvedCore = true;
            }
            return _core;
        }
    }
}
