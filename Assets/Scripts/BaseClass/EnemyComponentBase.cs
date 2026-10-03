using UnityEngine;

public class EnemyComponentBase : MonoBehaviour
{
    private EnemyCore _core;
    private bool _hasResolvedCore;
    protected EnemyCore Core
    {
        get
        {
            // 破棄時は取得済みの参照を返し、破棄されたCoreを再検索しない。
            if (!_hasResolvedCore)
            {
                _core = GetComponent<EnemyCore>();
                if (_core == null) throw new System.InvalidOperationException($"{name}: EnemyCore が同じGameObjectに必要です。");
                _hasResolvedCore = true;
            }
            return _core;
        }
    }
}
