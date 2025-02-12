using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMove : EnemyComponentBase
{
    [SerializeField, Header("感知範囲")] private float _detectRange;
    NavMeshAgent _agent;
    bool _playerIsInDetectRange;
    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
    }
    void Update()
    {
        _playerIsInDetectRange = Physics.CheckSphere(transform.position, _detectRange, Core.PlayerLayerIndex);
        if (_playerIsInDetectRange)
        {
            _agent.SetDestination(Core.Target.position);
        }

        // プレイヤーが範囲内かつ、プレイヤーまでに遮蔽がないときに停止する。
        var playerIsInFireRange = Physics.CheckSphere(transform.position, Core.ShootRange, Core.PlayerLayerIndex);
        if (!playerIsInFireRange) return;

        transform.LookAt(Core.Target.position);
        var dir = (Core.Target.position - transform.position).normalized;
        var ray = new Ray(transform.position, dir);

        if (Physics.Raycast(ray, out var hit, Core.ShootRange) && hit.collider.gameObject.layer == Core.PlayerLayerMask)
        {
            _agent.SetDestination(transform.position);
        }
    }
    private void OnDrawGizmos()
    {
        // 感知範囲
        Gizmos.color = (_playerIsInDetectRange) ? Color.yellow : Color.green;
        Gizmos.DrawWireSphere(transform.position, _detectRange);

        // 射程距離
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Core.ShootRange);
    }
}
