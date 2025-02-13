using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : PlayerComponentBase
{
    Rigidbody _rb;
    LineRenderer _lr;
    Transform _camera;
    [SerializeField] private float _speed;
    [SerializeField] private Transform _lookAt;

    public float SpeedBoost { get; set; }

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _lr = GetComponent<LineRenderer>();
        _camera = Camera.main.transform;
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
        _rb.AddForce(_speed * Time.deltaTime * dir);
    }
}
