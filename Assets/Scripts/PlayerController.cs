using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private float _radius;
    private CapsuleCollider _capsuleCollider;
    private const float Gravity = -9.8f;
    [SerializeField] private Vector2 _velocity;
    [SerializeField] private float rayLength = 0.5f;
    private Rigidbody _rigidbody;
    [SerializeField] private float jumpHeight = 4f;
    [SerializeField] private bool flatOnGround;
    public Vector3 FeetPostion => new Vector3(_capsuleCollider.bounds.center.x, _capsuleCollider.bounds.min.y);

    public Vector3 FeetPostionWithOffset =>
        new Vector3(_capsuleCollider.bounds.center.x, _capsuleCollider.bounds.min.y + 0.02f);

    [SerializeField] private MovingPlatform _movingPlatform;
    [SerializeField] private MovingPlatform _platformInContact;
    [SerializeField] private float _horizontalMovementSpeed;
    private Vector3 _localpositionOnPlatform;
    [SerializeField] private Vector2 GlobalVelocity=> _velocity+_movingPlatformVelocity;
    private Vector2 _movingPlatformVelocity;

    void Start()
    {
        _capsuleCollider = GetComponent<CapsuleCollider>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        Jump();
    }
    // Update is called once per frame
    private void FixedUpdate()
    {
        PreFixedUpdate();
        ApplyGravity();
        Movement();
        CollisionCheck();
        SweepTest();
        MoveRigidbody();
        PostFixedUpdate();
    }

    private void Movement()
    {
        _velocity.x = Input.GetAxisRaw("Horizontal") * _horizontalMovementSpeed;
        //Debug.Log(_inputVelocityX);
    }
    private void Jump()
    {
        if (!flatOnGround)
            return;
        if (Input.GetKeyDown(KeyCode.Space) )
        {
            _velocity.y = Mathf.Sqrt(-2f * Gravity * jumpHeight);
            if (_platformInContact)
                _velocity += _platformInContact.Velocity;
            //Debug.Break();
        }
    }
    private void CollisionCheck()
    {
        RaycastHit groundHit;
        bool onGround = Physics.Raycast(FeetPostionWithOffset, Vector3.down, out groundHit, rayLength);
        if (onGround)
        {
            float distanceBetweenFeetAndGround = Vector3.Distance(FeetPostion, groundHit.point);
            // MovingPlatform platform = groundHit.transform.GetComponent<MovingPlatform>();
            // if (platform && distanceBetweenFeetAndGround <= 0.01f)
            // {
            //     _movingPlatformVelocity = platform.Velocity;
            //     _localpositionOnPlatform = platform.transform.InverseTransformPoint(_rigidbody.position);
            // }
            // else
            // {
            //     _movingPlatformVelocity = Vector2.zero;
            // }

            if (distanceBetweenFeetAndGround <= 0.01f)
            {
                flatOnGround = true;
            }
            else
            {
                flatOnGround = false;
            }
        }
        else
        {
            flatOnGround = false;
        }
    }

    private void PreFixedUpdate()
    {
        _movingPlatform.PreFixedUpdate();
    }
    private void PostFixedUpdate()
    {
    }
    private void SweepTest()
    {
        RaycastHit hit;
        if (_rigidbody.SweepTest(Vector3.down, out hit,rayLength))
        {
            //_velocity.y = Mathf.Sign(_velocity.y) * hit.distance;
            _platformInContact = hit.transform.GetComponent<MovingPlatform>();
            if (_platformInContact && hit.distance <=0.1f  )
            {
                _movingPlatformVelocity = _platformInContact.Velocity;
                //Debug.Log("Applying");
            }
            else
            {
                _platformInContact = null;
                _movingPlatformVelocity = Vector3.zero;
            }
            //Debug.Log("Applying"+ hit.distance);
        }
        else
        {
            _platformInContact = null;
            _movingPlatformVelocity = Vector3.zero;
        }
    }
    public void OnCollisionEnter(Collision collision)
    {
        OnCollison(collision);
    }

    public void OnCollisionStay(Collision collision)
    {
        MovingPlatform _platform = collision.transform.GetComponent<MovingPlatform>();
        OnCollison(collision);
    }

    private void OnCollison(Collision collision)
    {
        // for (int i = 0; i < collision.contactCount; i++)
        // {
        //     ContactPoint contactPoint = collision.contacts[i];
        //     MovingPlatform platform = collision.transform.GetComponent<MovingPlatform>();
        //     if (platform)
        //     {
        //         platformInContact = platform;
        //     }
        // }
    }

    private void ApplyGravity()
    {
        _velocity += Vector2.up * Gravity * Time.fixedDeltaTime;
        if (flatOnGround && _velocity.y < 0)
            _velocity.y = 0f;
    }

    private void MoveRigidbody()
    {
        _rigidbody.velocity = _velocity + _movingPlatformVelocity;
    }
}