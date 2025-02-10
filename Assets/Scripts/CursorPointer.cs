using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorPointer : MonoBehaviour
{
    [SerializeField] LayerMask mask;
    [SerializeField] float _raycastMaxDistance;
    private void Start()
    {
        Cursor.visible = false;
    }
    void Update()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var rayEndPos = ray.origin + ray.direction * _raycastMaxDistance;
        Debug.DrawLine(ray.origin, rayEndPos, Color.green);
        if (Physics.Raycast(ray, out var hit, _raycastMaxDistance, mask))
        {
            transform.position = hit.point;
        }
    }
}
