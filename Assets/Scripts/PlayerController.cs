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

    [SerializeField] private MovingPlatform [] _movingPlatforms;
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
        _movingPlatforms  = FindObjectsOfType<MovingPlatform>();
    }

    private void Update()
    {
        if(JumpInput())
        {
            Jump();
        }
    }
    // Update is called once per frame
    private void FixedUpdate()
    {
        PreFixedUpdate(); //Moving platform needs to move here
        SweepCollisionCheck(); // All collision flags will be set here
        ApplyGravity(); // Gravity
        Movement(); //Basic movement
        MoveRigidbody(); // Applying final velocity 
        CacheGlobalVelocity();
        PostFixedUpdate(); 
    }
    private void CacheGlobalVelocity()
    {
         _globalVelocity = GlobalVelocity;
    }
    private void Movement()
    {
        _velocity.x = Input.GetAxisRaw("Horizontal") * _horizontalMovementSpeed;
        //Debug.Log(_inputVelocityX);
    }
    private bool JumpInput()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }
    private void Jump()
    {
        if (!flatOnGround)
            return;
     
        _velocity.y = Mathf.Sqrt(-2f * Gravity * jumpHeight);
        if (_platformInContact)
        {
           _velocity += _platformInContact.Velocity;
           _platformInContact = null;
            detachTimer = Time.time;
        }       
    }
    private void SweepCollisionCheck()
    {
        RaycastHit groundHit;
        bool onGround = Physics.Raycast(FeetPostionWithOffset, Vector3.down, out groundHit, rayLength);
        //Physics.Raycast(FeetPostionWithOffset, Vector3.down, out groundHit, rayLength); // Boxcast
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
    // private void CollisionCheck()
    // {
    //     RaycastHit groundHit;
    //     bool onGround = Physics.Raycast(FeetPostionWithOffset, Vector3.down, out groundHit, rayLength); // Boxcast
    //     if (onGround)
    //     {
    //         float distanceBetweenFeetAndGround = Vector3.Distance(FeetPostion, groundHit.point);
    //         if (distanceBetweenFeetAndGround <= 0.05f)
    //         {
    //             flatOnGround = true;
    //         }
    //         else
    //         {
    //             flatOnGround = false;
    //             return;
    //         }
    //         _platformInContact= groundHit.transform.GetComponent<MovingPlatform>();
    //         if(detachTimer +0.1f >Time.time)
    //             _platformInContact = null;
    //         if (_platformInContact)
    //         {
    //             _movingPlatformVelocity = _platformInContact.Velocity;
    //             _localpositionOnPlatform = _platformInContact.transform.InverseTransformPoint(_rigidbody.position);
    //              _velocity.y = 0f;
    //         }
    //         else
    //         {
    //              DetachFromMovingPlatform();
    //         }

           
    //     }
    //     else
    //     {
    //         flatOnGround = false;
    //         DetachFromMovingPlatform();
    //     }
    // }
    private void DetachFromMovingPlatform()
    {
        _movingPlatformVelocity = Vector2.zero;
        _platformInContact = null;
    }

    private void PreFixedUpdate()
    {
        for(int i=0;i<_movingPlatforms.Length;i++)
        {
            _movingPlatforms[i].PreFixedUpdate();
        }
    }
    private void PostFixedUpdate()
    {
       VerticalSweepTest();
    }
    private void VerticalSweepTest()
    {
        RaycastHit hit;
        bool nextStepCollision = _rigidbody.SweepTest(_rigidbody.velocity.normalized, out hit, _rigidbody.velocity.magnitude * Time.fixedDeltaTime);
        if(nextStepCollision)
        {
            MovingPlatform movingPlatform = hit.transform.GetComponent<MovingPlatform>();
            if(movingPlatform)
            return;
            Debug.Log("Sweep");
            _rigidbody.velocity = _velocity = _rigidbody.velocity.normalized *(hit.distance/ Time.fixedDeltaTime);
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