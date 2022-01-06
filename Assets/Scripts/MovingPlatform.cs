using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Vector2 direction;
    private Rigidbody _rigidbody;
    private Vector3 prevPosition;
    [SerializeField]private Vector3 _velocity;
    public Vector2 Velocity => _velocity;
    private float _directionChangeTimestamp;
    [SerializeField]private float _movementSpeed;
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.isKinematic = true;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        prevPosition = _rigidbody.position;
    }

    public  void PreFixedUpdate()
    {
        if (Time.time > _directionChangeTimestamp + 2.5f)
        {
            direction *= -1;
            _directionChangeTimestamp = Time.time;
        }

        Vector3 nextPosition = _rigidbody.position + _movementSpeed * (Vector3) direction * Time.fixedDeltaTime;
        _rigidbody.MovePosition(nextPosition);
        _velocity = (nextPosition - _rigidbody.position)/Time.fixedDeltaTime;
        prevPosition = _rigidbody.position;
        // Debug.Log(_rigidbody.position+ _movementSpeed*(Vector3) direction* Time.fixedDeltaTime);
        // Debug.Log(_rigidbody.position);
    }
}
