using UnityEngine;

public class PlayerComponentBase : MonoBehaviour
{
    private PlayerCore _core;
    protected PlayerCore Core
    {
        get
        {
            if (_core == null)
            {
                _core = GetComponent<PlayerCore>();
            }
            return _core;
        }
    }
}
