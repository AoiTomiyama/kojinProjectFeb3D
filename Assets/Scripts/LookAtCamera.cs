using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _offset;
    void Update()
    {
        transform.position = _target.position + _offset;
        transform.LookAt(Camera.main.transform.position);
        transform.forward = -transform.forward;
    }
}
