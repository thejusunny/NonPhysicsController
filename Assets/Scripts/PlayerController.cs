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
    [SerializeField] private Vector2 _localVelocity ;
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
    [SerializeField] private Vector2 GlobalVelocity=> _localVelocity +_movingPlatformVelocity+_platformDetachVelocity;
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
    [SerializeField]Vector2 transformVelocity;
    Vector2 prevPosition;
    float jumpTimeStamp;
    [SerializeField] bool disableGravity;
    float _horizontalInputSpeed;
    [SerializeField]Vector2 _platformDetachVelocity;
    [SerializeField]float _airDrag=0.1f;
    [SerializeField] bool _carryPlatformMomentum;
    bool pushedFromSides;
    void Start()
    {
        _capsuleCollider = GetComponent<CapsuleCollider>();
        _rigidbody = GetComponent<Rigidbody>();
        _movingPlatforms  = FindObjectsOfType<MovingPlatform>();
        prevPosition = _rigidbody.position;
    }
    private void Update()
    {
        if(JumpInput())
        {
            Jump();
        }
        if(DashInput())
            InitiateDash();
    }
    // Update is called once per frame
    private void FixedUpdate()
    {
        transformVelocity = ((Vector2)_rigidbody.position - prevPosition)/ Time.fixedDeltaTime;
        prevPosition = _rigidbody.position;
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
            Dash();
            return;
        }
        _horizontalInputSpeed = inputX* _horizontalMovementSpeed;
        _localVelocity.x = _horizontalInputSpeed ;
    }
    private void Dash()
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
        if(speed % _dashDistance ==0) // Defining pixel perfect dash
        {
            //debug.Log("Linear speed"); 
        }
        _localVelocity  = dashDirection * speed;
        Vector3 lastPoint = _dashStartPosition + dashDirection*_dashDistance;
        float completion = InverseLerp(_dashStartPosition, lastPoint, _rigidbody.position);
        if(completion>0.99f)
        {
            _isDashing = false;
             _localVelocity  *=0.99f;
        }
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
    private void InitiateDash()
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
        _localVelocity .y = Mathf.Sqrt(-2f * Gravity * jumpHeight);
        jumpTimeStamp = Time.time;
        //Check for the ground your standing and transfer the velocity 
        if (_platformInContact)
        {
            detachTimer = Time.time;
           _platformDetachVelocity = _platformInContact.Velocity; // velocity of the platform you jumped on, this will added to the total
        }       
    }
    private void ResetLocalVerticalVelocity()
    {
        _localVelocity.y = 0f;
    }
    private bool JumpDidntHappenLastFewFrames()
    {
        return Time.time> jumpTimeStamp +0.1f;
    }
    private bool PerformedJumpInLastFewFrames()
    {
        return jumpTimeStamp +0.1> Time.time;
    }
    private void SweepCollisionCheck()
    {
        RaycastHit groundHit;
        RaycastHit leftHit, rightHit, downHit;
        pushedFromSides = false;
        bool onGround = Physics.Raycast(FeetPostionWithOffset, Vector3.down, out groundHit, rayLength);
        bool downSweep = CapsuleCastWrapper(_rigidbody.position, _capsuleCollider.height*0.95f,_capsuleCollider.radius,Vector2.down,0.5f,out downHit);
        if(downSweep)
        {
            if(downHit.distance <=0.08f)
            {
                flatOnGround = true;
                 _platformInContact= downHit.transform.GetComponent<MovingPlatform>();
                 if(!PerformedJumpInLastFewFrames()) // there was no jump pressed in last few frames
                    _localVelocity.y= 0f;  // When player touches the ground, local velocity should be reset to zero, but only if jump was not initiated in last few frames
                else
                    DetachFromMovingPlatform();  // if jump pressed then don't detect moving platform anymore
                if(_platformInContact)
                {
                    UpdatePlayerVelocityOnMovingPlatform(_platformInContact);
                }
            }
            else
                flatOnGround = false;
        }
        else
            flatOnGround = false;
        if(!flatOnGround)
            DetachFromMovingPlatform();
        bool leftSweep = CapsuleCastWrapper(_rigidbody.position, _capsuleCollider.height,_capsuleCollider.radius*0.9f,Vector2.left,0.12f,out leftHit);
        bool rightSweep = CapsuleCastWrapper(_rigidbody.position, _capsuleCollider.height,_capsuleCollider.radius*0.9f,Vector2.right,0.12f,out rightHit);
        if(leftSweep )
        {
            if(!IsInContactWithMovingPlatform())
            {
               HandleMovingPlatformCollisionFromSides(leftHit, Vector2.left);
               pushedFromSides = true;
            }
        }
        if(rightSweep)
        {
            if(!IsInContactWithMovingPlatform())
            {
                HandleMovingPlatformCollisionFromSides(rightHit, Vector2.right);
                 pushedFromSides = true;
            }
        }
        if(!IsInContactWithMovingPlatform())
            DetachFromMovingPlatform();
    }
    void HandleMovingPlatformCollisionFromSides(RaycastHit hit, Vector2 side)
    {
        MovingPlatform currentPlatform = hit.transform.GetComponent<MovingPlatform>();
        if(currentPlatform)
        {
            _platformInContact = currentPlatform;
            if(Vector2.Dot(_platformInContact.Velocity,  side)>0f)
            {
                DetachFromMovingPlatform();
            }
            else
                UpdatePlayerVelocityOnMovingPlatform(_platformInContact);
        }
    }
    bool IsInContactWithMovingPlatform(){return _platformInContact;}
    bool  CapsuleCastWrapper(Vector3 pos, float height, float radius,Vector2 direction,float distance,out  RaycastHit hit)
    {
        return Physics.CapsuleCast(pos+Vector3.up*(height/2- radius), pos + Vector3.down* (height/2-radius), radius, direction,out hit,distance);
    }
    private void UpdatePlayerVelocityOnMovingPlatform(MovingPlatform standingPlatform)
    { 
        _movingPlatformVelocity = standingPlatform.Velocity;
        _localpositionOnPlatform = standingPlatform.transform.InverseTransformPoint(_rigidbody.position);
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
        bool nextStepCollision = CapsuleCastWrapper(_rigidbody.position, _capsuleCollider.height, _capsuleCollider.radius*0.98f,_rigidbody.velocity.normalized, _rigidbody.velocity.magnitude*Time.fixedDeltaTime, out hit);
        //_rigidbody.SweepTest(_rigidbody.velocity.normalized, out hit, _rigidbody.velocity.magnitude * Time.fixedDeltaTime);
        if(nextStepCollision)
        {
            MovingPlatform movingPlatform = hit.transform.GetComponent<MovingPlatform>();
            if(movingPlatform)
            {
               return;
            }
            float distanceFromBottom = hit.distance - _capsuleCollider.radius*0.02f; // the extra offset that was reduced on capsule cast radius
            _horizontalVelocity = _rigidbody.velocity.x; //Storing the horizontal component so that,it can be added at end of the frame
            _rigidbody.velocity = _localVelocity  = _rigidbody.velocity.normalized *(distanceFromBottom/ Time.fixedDeltaTime);
        }
    }
    public void OnCollisionEnter(Collision collision)
    {
        _localVelocity .x = _horizontalVelocity;
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
        _localVelocity  += Vector2.up * Gravity * Time.fixedDeltaTime;
        if (flatOnGround && _localVelocity .y < 0)
            _localVelocity .y = 0f;
    }
    void ApplyDragToPlatformMomentum()
    {
          _platformDetachVelocity.x *= (1f / (1f + (_airDrag * Time.deltaTime)));// _airDrag 
        if(flatOnGround && Time.time> jumpTimeStamp+0.1f)
        {
            if(_platformInContact)
                _platformDetachVelocity.x = 0f; // no drag
            else
                _platformDetachVelocity.x *= 0.9f; // Ground drag
                _platformDetachVelocity.y = 0f;
        }
    }
    private void MoveRigidbody()
    {
         if(!_carryPlatformMomentum)
            _platformDetachVelocity = Vector2.zero;
        if(pushedFromSides)
        {
            if(Vector2.Dot(_localVelocity, _movingPlatformVelocity)<0)
                _localVelocity.x = 0f;
        }
         _rigidbody.velocity = _localVelocity  + _movingPlatformVelocity + _platformDetachVelocity;
        ApplyDragToPlatformMomentum();
    }
    Vector3 InverseTransformPoint(Vector3 transforPos, Quaternion transformRotation, Vector3 transformScale, Vector3 pos) 
    {
        Matrix4x4 matrix = Matrix4x4.TRS(transforPos, transformRotation, transformScale);
        Matrix4x4 inverse = matrix.inverse;
        return inverse.MultiplyPoint3x4(pos);
    }
    
}