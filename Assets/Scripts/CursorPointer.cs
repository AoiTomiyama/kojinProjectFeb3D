using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorPointer : MonoBehaviour
{
    [SerializeField] LayerMask mask;
    void Update()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, 999, mask))
        {
            transform.position = hit.point;
        }
    }
}
