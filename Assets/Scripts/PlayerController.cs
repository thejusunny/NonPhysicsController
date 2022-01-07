using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update
    private CapsuleCollider _capsuleCollider;
    private const float BaseGravity = -9.8f;
    [SerializeField, Range(1, 10f)] private float _gravityMultiplier=1f;
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
    private Vector2 _localVelocity;
    [SerializeField] private Vector2 GlobalVelocity=> _velocity+_movingPlatformVelocity;
    [SerializeField] private Vector2 _globalVelocity;
    [SerializeField]private Vector2 _movingPlatformVelocity;
    private float detachTimer;
    private float Gravity => BaseGravity * _gravityMultiplier;
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
        CollisionCheck();
        ApplyGravity();
        Movement();
        //SweepTest();
        MoveRigidbody();
        _globalVelocity = GlobalVelocity;
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
            {
                _velocity += _platformInContact.Velocity;
                _platformInContact = null;
                detachTimer = Time.time;
            }
                
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
            if (distanceBetweenFeetAndGround <= 0.05f)
            {
                flatOnGround = true;
            }
            else
            {
                flatOnGround = false;
                return;
            }
            _platformInContact= groundHit.transform.GetComponent<MovingPlatform>();
            if(detachTimer +0.1f >Time.time)
                _platformInContact = null;
            if (_platformInContact)
            {
                _movingPlatformVelocity = _platformInContact.Velocity;
                _localpositionOnPlatform = _platformInContact.transform.InverseTransformPoint(_rigidbody.position);
                 _velocity.y = 0f;
            }
            else
            {
                 DetachFromMovingPlatform();
            }

           
        }
        else
        {
            flatOnGround = false;
            DetachFromMovingPlatform();
        }
    }
    private void DetachFromMovingPlatform()
    {
        _movingPlatformVelocity = Vector2.zero;
        _platformInContact = null;
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
            if (_platformInContact && hit.distance <=0.1f)
            {
                _movingPlatformVelocity = _platformInContact.Velocity;
            }
            else
            {
                _platformInContact = null;
                _movingPlatformVelocity = Vector3.zero;
            }
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
        if(Mathf.Abs(_velocity.x)>0f && Mathf.Sign(_velocity.x)!= Mathf.Sign(_movingPlatformVelocity.x))
            _movingPlatformVelocity.x = 0f;
        _rigidbody.velocity = _velocity + _movingPlatformVelocity;
    }
}