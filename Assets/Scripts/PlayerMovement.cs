using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Values")]
    [SerializeField] private float walkSpeed = 20f;
    [SerializeField] private float runSpeed = 50f;
    [SerializeField] private float[] speedMultipliers = { 1f, 2f, 2.5f, 3f}; 
    //[SerializeField] private float[] gearsSmoothTime = { 0.1f ,0.3f, 0.5f, 0.8f }; //smooth damp time for every gear (SmoothDamp approach)
    [SerializeField] private float[] gearsAccelerations = { 60f, 10f, 5f, 2f }; //MoveTowards approach
    [SerializeField] private float[] gearsAngularAccelerations = { 60f, 40f, 30f, 20f };

    [Tooltip("Rotation speed of the character")]
    public float RotationSpeed = 1.0f;

    [Header("Air and Jump Values")]
    public bool isAirControlActive = true;
    [SerializeField] private float jumpForce = 10f;

    [Header("Player Components")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private PlayerInputs _playerInput;

    [Header("Player Floor Checks")]
    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;
    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.1f;
    public LayerMask groundMask;
    public LayerMask slopeMask;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject cinemachineCameraTarget;
    [Tooltip("Crouch reference")]
    public GameObject cinemachineCrouchTarget;
    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 90.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -90.0f;
    public Cinemachine.CinemachineVirtualCamera followPlayerCamera;


    private Vector2 _moveInput;
    public enum MovementState { Grounded, InAir, OnWall, OnEdge, OnSlope}

    [Header("Player Current State")]
    public MovementState currentState;

    private bool isSprinting = false;
    private bool isCrouching = false;
    private bool wasCrouching = false;
    private bool isSliding = false;
    private bool wasSliding = false;
    private bool isGearShiftActive = false;
    private Vector3 currentVelocity;
    private int currentGear; //0: no sprint; 1: gear1; 2: gear2; 3: gear3
    private Vector3 istantAcceleration;
    private float currentGearAcceleration; //it goes up
    private Vector2 currentHorizontalVelocity;
    private RaycastHit slopeHit;
    /*
    private Vector3 refCurrentVel;
    private float currentGearSmoothTime;
    private const float velocityLerpingTreshold = 0.2f;
    */

    private Vector3 refCurrentCrouchingVel;
    private float _rotationVelocity;
    private const float _threshold = 0.01f;
    private float _cinemachineTargetPitch;
    private Vector3 _cinemachineTargetStartPos;
    private Vector3 _lastPlayerInputDirection;
    private Vector3 _lastPlayerVelocityAlongPlayerCoords;

    private float inAirTimer = 0f;

    // Start is called before the first frame update
    void Start()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        if (_playerInput == null)
            _playerInput = GetComponent<PlayerInputs>();

        _moveInput = new(0, 0);
        currentGear = 0;
        currentVelocity = _rigidbody.velocity;


        followPlayerCamera = GameObject.FindObjectOfType<Cinemachine.CinemachineVirtualCamera>();
        followPlayerCamera.Follow = cinemachineCameraTarget.transform;

        currentGearAcceleration = 0f;
        _cinemachineTargetStartPos = cinemachineCameraTarget.transform.localPosition;

        //currentGearSmoothTime = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        _moveInput.x = _playerInput.move.x;
        _moveInput.y = _playerInput.move.y;
        
        currentHorizontalVelocity = new Vector2(_rigidbody.velocity.x, _rigidbody.velocity.z);

        switch (currentState)
        {
            case MovementState.Grounded:
                HandleGroundState();
                break;
            case MovementState.InAir:
                HandleInAirState();
                break;
            case MovementState.OnWall:
                break;
            case MovementState.OnEdge:
                HandleOnEdgeState();
                break;
            case MovementState.OnSlope:
                HandleOnSlopeState();
                break;
        }
    }

    private void FixedUpdate()
    {
        currentVelocity = _rigidbody.velocity;
        Vector3 vel;

        switch (currentState)
        {
            case MovementState.Grounded:
                //move player
                if (_moveInput.magnitude > 0.1f || isSliding)
                {
                    vel = ComputeVelocity(false);
                    _rigidbody.velocity = new Vector3(vel.x, _rigidbody.velocity.y, vel.z);
                }
                break;
            case MovementState.InAir:

                if(isAirControlActive)
                    ComputeAirControl();

                break;
            case MovementState.OnWall:
                break;
            case MovementState.OnEdge:
                break;
            case MovementState.OnSlope:
                if (_moveInput.magnitude > 0.1f || isSliding)
                {
                    vel = ComputeVelocity(true);
                    _rigidbody.velocity = new Vector3(vel.x, _rigidbody.velocity.y, vel.z);
                }
                break;
        }

    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    void HandleGroundState()
    {
        //Check for Jump
        CheckForJump();
        //Check For Sprint
        CheckForSprint();
        //Check for Slide
        CheckForCrouchSlide();

        //Check for Ground
        if (!CheckForGround())
        {
            //NOTE: Keep an eye on parameters reset
            currentState = MovementState.InAir;
            return;
        }

        //Check for Slope
        if (CheckForSlope())
        {
            currentState = MovementState.OnSlope;
            return;
        }

    }

    void HandleInAirState()
    {
        //Check for Ground
        if (CheckForGround())
        {
            //NOTE: Keep an eye on parameters reset
            inAirTimer = 0f;
            currentState = MovementState.Grounded;
            return;
        }
        //Check for Slope
        if (CheckForSlope())
        {
            inAirTimer = 0f;
            currentState = MovementState.OnSlope;
            return;
        }
        inAirTimer += Time.deltaTime;
        //Check for Wall
    }

    void HandleOnSlopeState()
    {
        //Check for SprintState
        CheckForSprint();
        //Check for the jump
        CheckForJump();
        //Check coruchslide
        CheckForCrouchSlide();

        //groundcheck
        if (CheckForGround())
        {
            //NOTE: Keep an eye on parameters reset
            currentState = MovementState.Grounded;
            return;
        }

        //slopecheck
        if (!CheckForGround() && !CheckForSlope())
        {
            currentState = MovementState.InAir;
            return;
        }
    }

    void HandleOnEdgeState()
    {
        if (_playerInput.jump)
        {
            //check if is last edge
            RaycastHit hit;

            Physics.Raycast(transform.position + Vector3.up * transform.GetComponent<CapsuleCollider>().height + Vector3.up, transform.forward, out hit, 0.4f);

            if(hit.transform == null)
            {
               
            }

        }
    }

    void ComputeAirControl()
    {
        Vector3 inputDir = (transform.right * _moveInput.x + transform.forward * _moveInput.y).normalized;
       
        Vector3 finalVel;

        if (inAirTimer < 0.8f)
        {
            finalVel = Vector3.MoveTowards(_rigidbody.velocity, _rigidbody.velocity.magnitude * inputDir, gearsAccelerations[currentGear] * Time.fixedDeltaTime);

            Vector2 horizontalFinalVel = new Vector2(finalVel.x, finalVel.z);

            if(horizontalFinalVel.magnitude>currentHorizontalVelocity.magnitude)
                finalVel = _rigidbody.velocity;
        }
        else
            finalVel = _rigidbody.velocity;

        if(inputDir.magnitude>0.1f)
            _rigidbody.velocity = new Vector3(finalVel.x, _rigidbody.velocity.y, finalVel.z);
    }

    Vector3 ComputeVelocity(bool onSlope)
    {
        //Direction computing
        Vector3 inputDir = transform.right * _moveInput.x + transform.forward * _moveInput.y;

        if (isSliding)
            inputDir = _lastPlayerInputDirection;

        Vector3 movDir;

        if (onSlope)
            movDir = Vector3.ProjectOnPlane(inputDir, slopeHit.normal);
        else
            movDir = inputDir;

        //Speed computing
        float targetSpeed = isSprinting ? runSpeed : walkSpeed;

        if (currentGear > 0)
            targetSpeed = runSpeed;

        targetSpeed *= speedMultipliers[currentGear]; //we add the gear speed boost on max velocity
        movDir = movDir.normalized;
        //Lerp Speed -> based on the maximum speed of the gear
        //Lerp Direction Change -> based on a percentage of control based on the current gear
        targetSpeed = Mathf.MoveTowards(_rigidbody.velocity.magnitude, targetSpeed, gearsAccelerations[currentGear] * Time.fixedDeltaTime);

        //Vector3 targetVel = targetSpeed * LerpDirectionChange(movDir, gearsAngularAccelerations[currentGear]);
        //Vector3 targetVel = LerpSpeed(targetSpeed, gearsAccelerations[currentGear]) *LerpDirectionChange(movDir, gearsAngularAccelerations[currentGear]);

        Vector3 finalVel;

        if (isSliding)
            finalVel = _rigidbody.velocity.magnitude * movDir;
        else
            finalVel = targetSpeed * LerpDirectionChange(movDir, gearsAngularAccelerations[currentGear], onSlope);


        if (!isSliding)
            _lastPlayerInputDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;

        //limit velocity on slope
        if (onSlope)
            if (finalVel.magnitude > targetSpeed)
                finalVel = _rigidbody.velocity.magnitude * movDir;

        return finalVel;
    }


    Vector3 LerpDirectionChange(Vector3 targetDir, float angularAccel, bool onSlope)
    {
        Vector3 orientationVector = transform.right * _moveInput.x + transform.forward * _moveInput.y;

        Vector3 currentDir = onSlope ? Vector3.ProjectOnPlane(orientationVector, slopeHit.normal) : _rigidbody.velocity.normalized;

        Vector3 dir = Vector3.MoveTowards(currentDir, targetDir, angularAccel * Time.fixedDeltaTime);

        return dir;
    }

    private void CameraRotation()
    {
        // if there is an input
        if (_playerInput.look.sqrMagnitude >= _threshold)
        {
            //Don't multiply mouse input by Time.deltaTime
            float deltaTimeMultiplier = 1.0f;

            _cinemachineTargetPitch += _playerInput.look.y * RotationSpeed * deltaTimeMultiplier;
            _rotationVelocity = _playerInput.look.x * RotationSpeed * deltaTimeMultiplier;

            // clamp our pitch rotation
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Update Cinemachine camera target pitch
            cinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

            // rotate the player left and right
            transform.Rotate(Vector3.up * _rotationVelocity);
            //_rigidbody.MoveRotation(Quaternion.Euler(Vector3.up * _rotationVelocity));
        }
    }
    void CheckForCrouchSlide()
    {
        if (_playerInput.crouch_slide)
        {

            if (_rigidbody.velocity.magnitude > walkSpeed)
            {
                isSliding = true;
                wasSliding = isSliding;
            }
            else
            {
                isCrouching = true;
                wasCrouching = isCrouching;
            }


            cinemachineCameraTarget.transform.localPosition = Vector3.SmoothDamp(cinemachineCameraTarget.transform.localPosition,
                cinemachineCrouchTarget.transform.localPosition, ref refCurrentCrouchingVel, 0.1f);

            Debug.Log("CROUCHING");
        }
        else
        {
            isCrouching = false;
            isSliding = false;


            cinemachineCameraTarget.transform.localPosition = Vector3.SmoothDamp(cinemachineCameraTarget.transform.localPosition,
                _cinemachineTargetStartPos, ref refCurrentCrouchingVel, 0.1f);

            wasSliding = false;
            wasCrouching = false;
        }
    }

    void CheckForSprint()
    {
        //Check for SprintState
        if (_playerInput.sprint)
        {
            isSprinting = true;
        }
        else
            isSprinting = false;
    }
    public bool CheckForGround()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        bool Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, groundMask, QueryTriggerInteraction.Ignore);

        return Grounded;
    }

    public bool CheckForSlope()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        bool OnSlope = Physics.CheckSphere(spherePosition, GroundedRadius, slopeMask, QueryTriggerInteraction.Ignore);

        if (OnSlope)
        {
            Physics.Raycast(transform.position, Vector3.down, out slopeHit, 0.5f);
        }

        Debug.DrawRay(transform.position, Vector3.down, Color.blue, 0.5f);

        return OnSlope;
    }

    public void CheckForJump()
    {

        if (_playerInput.jump)
        {
            _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            _playerInput.jump = false;
        }
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void OnDrawGizmos()
    {
        Vector3 spherePositionGroundCheck = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spherePositionGroundCheck, GroundedRadius);

    }

    public int GetGear()
    {
        return currentGear;
    }

    public float GetAcceleration()
    {
        return istantAcceleration.magnitude;
    }
}
