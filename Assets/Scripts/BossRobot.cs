using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BossRobot : MonoBehaviour
{
    public enum RobotState { Idle, Attacking, Stunned, Dead};

    public RobotState currentState;

    public float jumpForce = 10f;
    public float dashAcceleration = 50f;
    public float dashSpeed = 100f;
    public float rotationVelocity = 3f;
    public float stoppingDistanceFromTarget = 5f;

    public NavMeshAgent _navMesh;

    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;
    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;
    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.5f;
    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    private Rigidbody rb;

    private bool isJumping = false;
    public bool DebugJumpTrigger = false;
    private Transform target;
    private Vector3 currentVelocity;

    private void Start()
    {
        currentState = RobotState.Idle;
        target = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        switch (currentState)
        {
            case RobotState.Idle:
                HandleIdleState();
                break;
            case RobotState.Attacking:
                HandleAttackState();
                break;
        }
        CheckGrounded();

        //Debug
        if (Input.GetKeyDown(KeyCode.F))
            DebugJumpTrigger = true;

        RotateTowardPlayer();
    }

    void HandleIdleState()
    {
        //Switch state
        //Debug
        if (Grounded && !isJumping && DebugJumpTrigger)
        {
            Debug.Log("Chiamato");
            DebugJumpTrigger = false;          
            StartCoroutine(DashPunch());
        }

    }

    void HandleAttackState()
    {
        //TODO:

    }

    IEnumerator DashPunch()
    {
        //Dash
        _navMesh.acceleration = dashAcceleration;
        _navMesh.autoBraking = true;
        _navMesh.speed = dashSpeed;
        _navMesh.stoppingDistance = stoppingDistanceFromTarget;
        _navMesh.SetDestination(target.position);

        while (!_navMesh.isStopped)
        {
            if (Vector3.Distance(target.position, transform.position) <= stoppingDistanceFromTarget)
            {
                _navMesh.stoppingDistance = 0f;
                _navMesh.SetDestination(transform.position);
                break;
            }
            else
                _navMesh.SetDestination(target.position);
            yield return null;
        }

        //Little Pause

        //Trigger Punch Animation

        yield return null;
    }

    void RotateTowardPlayer()
    {
        Vector3 newDir = -(transform.position - target.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(newDir, transform.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationVelocity);
    }

    void CheckGrounded()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
    }  
}
