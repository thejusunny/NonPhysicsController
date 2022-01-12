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
    [SerializeField, Range(0.1f,10f)]private float _dashDistance=3f;
    [SerializeField, Range(0.05f,0.5f)] private float _dashDuration;
    private bool _isDashing;
    private Vector2 _dashInputXY;
    [SerializeField]private float _facingDirection=1;
    private Vector2 _dashStartPosition;
    float _horizontalVelocity;
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
        if(DashInput())
            Dash();
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
        float inputX =Input.GetAxisRaw("Horizontal");
        if(Mathf.Abs(inputX)>0)
            _facingDirection= inputX;
        if(_isDashing)
        {
            Vector2 dashDirection = _dashInputXY;
            if(_dashInputXY.magnitude>1)
            {
                dashDirection = _dashInputXY.normalized;
            }
            else if(_dashInputXY.magnitude<=0)
            {
                dashDirection.x = _facingDirection;
            }
            float speed = _dashDistance/ _dashDuration;
            if(speed % _dashDistance ==0)
            {
                //ebug.Log("Linear speed");
            }
            _velocity = dashDirection * speed;
            Vector3 lastPoint = _dashStartPosition + dashDirection*_dashDistance;
            float completion = InverseLerp(_dashStartPosition, lastPoint, _rigidbody.position);
            if(completion>0.99f)
            {
                _isDashing = false;
                _velocity *=0.5f;
                //_velocity *=0.25f;
            }
            return;
        }
        _velocity.x = inputX* _horizontalMovementSpeed;
        //Mathf.Lerp(_velocity.x, inputX * _horizontalMovementSpeed, _horizontalMovementSpeed*Time.fixedDeltaTime);

        //Debug.Log(_inputVelocityX);
    }
    public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
     {
         Vector3 AB = b - a;
         Vector3 AV = value - a;
         return Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB);
     }
    private bool JumpInput()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }
    private bool DashInput()
    {
        return Input.GetKeyDown(KeyCode.Z);
    }
    private void Dash()
    {
        if(_isDashing)
            return;
        _isDashing = true;
        _dashStartPosition = _rigidbody.position;
        _dashInputXY.x = Input.GetAxisRaw("Horizontal");
        _dashInputXY.y = Input.GetAxisRaw("Vertical");
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
                //return;
            }
            // _platformInContact= groundHit.transform.GetComponent<MovingPlatform>();
            // if(detachTimer +0.1f >Time.time)
            //     _platformInContact = null;
            // if (_platformInContact)
            // {
            //     _movingPlatformVelocity = _platformInContact.Velocity;
            //     _localpositionOnPlatform = _platformInContact.transform.InverseTransformPoint(_rigidbody.position);
            //      _velocity.y = 0f;
            // }
            // else
            // {
            //      DetachFromMovingPlatform();
            // }
        }
        else
        {
            flatOnGround = false;
            //DetachFromMovingPlatform();
        }
        RaycastHit leftHit, rightHit, downHit;
        bool leftSweep = CapsuleCastWrapper(_rigidbody.position, _capsuleCollider.height,_capsuleCollider.radius*0.9f,Vector2.left,0.15f,out leftHit);
        bool rightSweep = CapsuleCastWrapper(_rigidbody.position, _capsuleCollider.height,_capsuleCollider.radius*0.9f,Vector2.right,0.15f,out rightHit);
        bool downSweep = CapsuleCastWrapper(_rigidbody.position, _capsuleCollider.height,_capsuleCollider.radius*0.9f,Vector2.down,0.3f,out downHit);
        //_rigidbody.SweepTest(Vector3.down, out downHit, 0.1f);
        MovingPlatform movingPlatform = null;
        if(leftSweep)
        {
             _platformInContact= leftHit.transform.GetComponent<MovingPlatform>();
            if(_platformInContact)
            {
                if(Vector2.Dot(_platformInContact.Velocity, Vector2.left)>0)
                {
                    DetachFromMovingPlatform();
                    return;
                }
                UpdatePlayerOnMovingPlatform();
                 return;
            }
        }
        if(rightSweep)
        {
             _platformInContact= rightHit.transform.GetComponent<MovingPlatform>();
            if(_platformInContact)
            {
                if(Vector2.Dot(_platformInContact.Velocity, Vector2.right)>0)
                {
                    DetachFromMovingPlatform();
                    return;
                }
                UpdatePlayerOnMovingPlatform();
                return;
            }
        }
        if(downSweep)
        {
            _platformInContact= downHit.transform.GetComponent<MovingPlatform>();
            if(_platformInContact)
            {
               
                UpdatePlayerOnMovingPlatform();
                 return;
            }
        }
        if(!downSweep&& !rightSweep&& !leftSweep)
            DetachFromMovingPlatform();
    }
    bool  CapsuleCastWrapper(Vector3 pos, float height, float radius,Vector2 direction,float distance,out  RaycastHit hit)
    {
        return Physics.CapsuleCast(pos+Vector3.up*(height/2- radius), pos + Vector3.down* (height/2-radius), radius, direction,out hit,distance);
    }
    private void UpdatePlayerOnMovingPlatform()
    {
            
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
             _horizontalVelocity = _rigidbody.velocity.x;
            _rigidbody.velocity = _velocity = _rigidbody.velocity.normalized *(hit.distance/ Time.fixedDeltaTime);
        }
    }
    public void OnCollisionEnter(Collision collision)
    {
        _velocity.x = _horizontalVelocity;
        OnCollison(collision);
    }
    public void OnCollisionStay(Collision collision)
    {
        OnCollison(collision);
    }

    private void OnCollison(Collision collision)
    {
        
    }

    private void ApplyGravity()
    {
       if(_isDashing)
       return;
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