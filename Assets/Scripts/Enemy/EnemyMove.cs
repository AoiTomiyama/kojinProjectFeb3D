using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMove : EnemyComponentBase
{
    void Update()
    {
        transform.LookAt(Core.Target.position);
    }
}
