using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Camera _camera;

    private void Start()
    {
        _camera = SceneReferenceResolverInfrastructure.RequireUnique<Camera>(this);
    }

    void Update()
    {
        transform.LookAt(_camera.transform.position);
        transform.forward = -transform.forward;
    }
}
