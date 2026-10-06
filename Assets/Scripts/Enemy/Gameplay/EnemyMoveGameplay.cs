using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMoveGameplay : EnemyComponentBaseGameplay
{
    [SerializeField, Header("感知範囲")] 
    private float _detectRange;
    NavMeshAgent _agent;
    bool _playerIsInDetectRange;
    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null) throw new System.InvalidOperationException($"{name}: NavMeshAgent が同じGameObjectに必要です。");
        // Editorの事前読込みとPlayerの初回読込みで、NavMesh登録の順序が異なるため起動時に確認する。
        Debug.Assert(_agent.isOnNavMesh, $"{name}: EnemyMoveGameplay に有効なNavMeshが必要です。");
    }
    void Update()
    {
        _playerIsInDetectRange = Physics.CheckSphere(transform.position, _detectRange, Core.PlayerLayerMask);
        if (_playerIsInDetectRange)
        {
            _agent.SetDestination(Core.Target.position);
        }

        // プレイヤーが範囲内かつ、プレイヤーまでに遮蔽がないときに停止する。
        var playerIsInFireRange = Physics.CheckSphere(transform.position, Core.ShootRange, Core.PlayerLayerMask);
        if (!playerIsInFireRange) return;

        transform.LookAt(Core.Target.position);
        var dir = (Core.Target.position - transform.position).normalized;
        var ray = new Ray(transform.position, dir);

        if (Physics.Raycast(ray, out var hit, Core.ShootRange) && hit.collider.gameObject.layer == Core.PlayerLayer)
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
