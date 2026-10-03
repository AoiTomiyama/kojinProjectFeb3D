using UnityEngine;

public class FollowObject : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _offset;
    private void Start()
    {
        if (_target == null)
            throw new System.InvalidOperationException($"{name}: FollowObject._target が設定されていません。");
    }
    void Update()
    {
        transform.position = _target.position + _offset;
    }
}
