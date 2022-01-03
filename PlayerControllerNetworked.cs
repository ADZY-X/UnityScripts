using UnityEngine;
using Mirror;
using System.Collections;

public class PlayerController : NetworkBehaviour
{
    [Header("Tweakables:")]
    [SerializeField]
    private float movementForce = 1f;
    [SerializeField]
    private float jumpForce = 5f;
    [SerializeField]
    private float maxSpeed = 5f;
    private float timeGrounded;

    [SerializeField]
    private LayerMask groundLayer;
    private Vector3 forceDirection = Vector3.zero;
    private Vector2 move;
    [SyncVar]
    private bool isJumping;
    [SyncVar]
    private bool isFalling;
    private bool canMove;

    private Camera playerCamera;
    private Animator animator;
    private Rigidbody rb;

    void Start()
    {
        rb = this.GetComponent<Rigidbody>();
        animator = this.GetComponent<Animator>();
        playerCamera = GameObject.FindObjectOfType<Camera>();
        canMove = true;
    }

    private void Update()
    {
        if (!isLocalPlayer) { return; }

        move.x = Input.GetAxis("Horizontal");
        move.y = Input.GetAxis("Vertical");

        if (!canMove)
        {
            move = Vector3.zero;
        }

        if (Input.GetButtonDown("Jump") && IsGrounded() && canMove)
        {
            isJumping = true;
            animator.SetBool("IsJumping", isJumping);
        }
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) { return; }
        CmdMovePlayer(move, GetCameraRight(playerCamera), GetCameraForward(playerCamera));

        if (isJumping)
        {
            CmdJump(isJumping);
            isJumping = false;
            animator.SetBool("IsJumping", isJumping);
        }

        if (!IsGrounded())
        {
            isFalling = true;
            animator.SetBool("IsFalling", isFalling);
        }
        else
        {
            isFalling = false;
            animator.SetBool("IsFalling", isFalling);
        }

        CmdLookAt(move);
    }

    [Server]
    private Vector3 CalculateMovement(float x, float y, Vector3 camRight, Vector3 camForward)
    {
        forceDirection += x * camRight * movementForce;
        forceDirection += y * camForward * movementForce;

        return forceDirection;
    }

    [Client]
    private Vector3 CalculateLocalMovement(float x, float y, Vector3 camRight, Vector3 camForward)
    {
        forceDirection += x * camRight * movementForce;
        forceDirection += y * camForward * movementForce;

        return forceDirection;
    }

    [Server]
    private Vector3 CalculateJumpForce()
    {
        return new Vector3(0, jumpForce, 0);
    }

    [Client]
    private Vector3 CalculateLocalJumpForce()
    {
        return new Vector3(0, jumpForce, 0);
    }

    [Command]
    private void CmdMovePlayer(Vector2 move, Vector3 cameraRight, Vector3 cameraForward)
    {
        if (CalculateMovement(move.x, move.y, cameraRight, cameraForward) != CalculateLocalMovement(move.x, move.y, cameraRight, cameraForward))
        {
            rb.AddForce(CalculateMovement(move.x, move.y, cameraRight, cameraForward), ForceMode.Impulse);
            forceDirection = Vector3.zero;
        }
        else
        {
            rb.AddForce(CalculateLocalMovement(move.x, move.y, cameraRight, cameraForward), ForceMode.Impulse);
            forceDirection = Vector3.zero;
        }

        Vector3 horizontalVelocity = rb.velocity;
        horizontalVelocity.y = 0;
        if (horizontalVelocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            rb.velocity = horizontalVelocity.normalized * maxSpeed + Vector3.up * rb.velocity.y;
        }
    }

    [Command]
    private void CmdJump(bool jumping)
    {
        if (!jumping) return;
        if (CalculateJumpForce() != CalculateLocalJumpForce())
        {
            rb.AddForce(CalculateJumpForce(), ForceMode.Impulse);
        }
        else
        {
            rb.AddForce(CalculateLocalJumpForce(), ForceMode.Impulse);
        }
        
        isJumping = false;
    }

    [Command]
    private void CmdLookAt(Vector2 move)
    {
        Vector3 direction = rb.velocity;
        direction.y = 0f;

        if (move.sqrMagnitude > 0.1f && direction.sqrMagnitude > 0.1f)
        {
            Quaternion toRotate = Quaternion.LookRotation(direction, Vector3.up);
            this.rb.rotation = Quaternion.RotateTowards(rb.rotation, toRotate, 20f);
        }
        else
        { 
            rb.angularVelocity = Vector3.zero;
        }
    }

    private Vector3 GetCameraForward(Camera playerCamera)
    {
        Vector3 forward = playerCamera.transform.forward;
        forward.y = 0;
        return forward.normalized;
    }

    private Vector3 GetCameraRight(Camera playerCamera)
    {
        Vector3 right = playerCamera.transform.right;
        right.y = 0;
        return right.normalized;
    }

    private bool IsGrounded()
    {
        return Physics.CheckSphere(transform.position - new Vector3(0, 0, 0), 0.25f, groundLayer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position - new Vector3(0, 0, 0) , 0.25f);
    }
}
