using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    Rigidbody _rb;
    Transform _camera;
    [SerializeField] private float _speed;
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _camera = FindAnyObjectByType<CinemachineVirtualCamera>().transform;
    }
    void Update()
    {
        Move();
    }
    void Move()
    {
        var forward = (transform.position - _camera.position);
        forward.y = 0;
        forward = forward.normalized;
        var right = Quaternion.AngleAxis(90, Vector3.up) * forward;
        var h = Input.GetAxisRaw("Horizontal");
        var v = Input.GetAxisRaw("Vertical");
        _rb.AddForce((forward * v + right * h) * _speed);
    }
}
