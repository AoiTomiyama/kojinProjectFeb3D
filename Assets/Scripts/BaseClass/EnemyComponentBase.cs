using UnityEngine;

public class EnemyComponentBase : MonoBehaviour
{
    private EnemyCore _core;
    protected EnemyCore Core
    {
        get
        {
            if (_core == null)
            {
                _core = GetComponent<EnemyCore>();
            }
            return _core;
        }
    }
}
