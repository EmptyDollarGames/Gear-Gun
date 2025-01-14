using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossRobot : MonoBehaviour
{
    public enum RobotState { Idle, Attacking, Stunned, Dead};

    public RobotState currentState;

    public float jumpForce = 10f;

    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;
    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;
    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.5f;
    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    private bool isJumping = false;
    public bool DebugJumpTrigger = false;

    private void Start()
    {
        currentState = RobotState.Idle;
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
    }

    void HandleIdleState()
    {
        //Switch state
        //Debug
        if (Grounded && !isJumping && DebugJumpTrigger)
        {
            Debug.Log("Chiamato");
            DebugJumpTrigger = false;
            StartCoroutine(JumpAndSmash());           
        }

    }

    void HandleAttackState()
    {
        //TODO:

    }

    IEnumerator JumpAndSmash()
    {
        isJumping = true;
        GetComponent<Animator>().SetBool("Jump", true);

        yield return new WaitForSeconds(1f);

        GetComponent<Animator>().SetBool("InAir", true);
        GetComponent<Rigidbody>().AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

        float elapsedTime = 0f;
        float duration = 3f;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        while (elapsedTime < duration)
        {
            if (Mathf.Sign(GetComponent<Rigidbody>().velocity.y) < 0)
                if (Grounded)
                    break;
            Vector3 dir = -(transform.position - player.transform.position).normalized;

            GetComponent<Rigidbody>().AddForce(dir * 1f, ForceMode.Force);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        while(!Grounded)
        {
            yield return null;
        }

        GetComponent<Animator>().SetBool("Jump", false);
        GetComponent<Animator>().SetBool("InAir", false);
        isJumping = false;
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
