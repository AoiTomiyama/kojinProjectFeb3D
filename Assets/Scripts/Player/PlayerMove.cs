using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : PlayerComponentBase
{
    Rigidbody _rb;
    LineRenderer _lr;
    Transform _camera;
    private float _speed;
    [SerializeField] private Transform _lookAt;

    public float Speed { get => _speed; set => _speed = value; }

    private void Awake()
    {
        // 強化後の速度は各プレイヤーの実行時値として保持する。
        _speed = GetComponent<PlayerCore>().InitialStats.MoveSpeed;
    }

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _lr = GetComponent<LineRenderer>();
        _camera = SceneReferenceResolver.RequireUnique<Camera>(this).transform;
    }
    void Update()
    {
        Move();
        LookAt();
    }

    private void LookAt()
    {
        if (_lookAt == null)
        {
            return;
        }
        var lookAtPos = _lookAt.position;
        lookAtPos.y = transform.position.y;
        transform.LookAt(lookAtPos);

        _lr.SetPosition(0, transform.position);
        _lr.SetPosition(1, lookAtPos);
    }

    void Move()
    {
        var forward = (transform.position - _camera.position);
        forward.y = 0;
        forward = forward.normalized;
        var right = Quaternion.AngleAxis(90, Vector3.up) * forward;
        var h = Input.GetAxisRaw("Horizontal");
        var v = Input.GetAxisRaw("Vertical");
        var dir = (forward * v + right * h).normalized;
        _rb.AddForce(Speed * Time.deltaTime * dir);
    }
}
