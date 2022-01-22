using UnityEngine;
using Mirror;
using Black.ClientSidePrediction;
using System;

public class PlayerMovement : MovementEntity
{
    [Header("Tweakables:")]
    [SerializeField] private float movementForce = 1f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private ParticleSystem jumpParticle;
    [SerializeField] private AudioClip jumpSound;
    private AudioSource audioSource;

    private Vector3 forceDirection = Vector3.zero;
    private Vector2 move;
    private Vector3 yaw, pitch;

    private bool pressedJump;
    private bool isGrounded;

    private Camera playerCamera;
    private Animator animator;
    private Rigidbody rb;

    protected override void Start()
    {
        rb = this.GetComponent<Rigidbody>();
        animator = this.GetComponent<Animator>();
        playerCamera = GameObject.FindObjectOfType<Camera>();
        audioSource = GetComponent<AudioSource>();
        base.Start();
    }

    public override void OnStartAuthority()
    {
        if (!isServer)
        {
            GetComponent<NetworkTransform>().enabled = false;
        }
    }

    public override void ApplyMovement()
    {
        Movement();

        Jump();

        Respawn();

        LookDirection();
    }

    private void Movement()
    {
        forceDirection += move.x * yaw * movementForce;
        forceDirection += move.y * pitch * movementForce;

        rb.AddForce(forceDirection, ForceMode.VelocityChange);
        forceDirection = Vector3.zero;

        if (rb.velocity.magnitude > 5)
        {
            rb.velocity = rb.velocity.normalized * 5;
        }
    }

    private void Jump()
    {
        isGrounded = Physics.CheckSphere(transform.position + new Vector3(0, 0, 0), 0.1f, groundLayer);

        if (pressedJump && isGrounded)
        {
            if (isLocalPlayer)
            {
                CmdJumpEffects();
            }
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            animator.SetBool("IsJumping", true);
        }
        else
        {
            rb.AddForce(Physics.gravity * 1.2f);
            animator.SetBool("IsJumping", false);
        }

        if (!isGrounded)
        {
            animator.SetBool("IsFalling", true);
        }
        else
        {
            animator.SetBool("IsFalling", false);
        }
    }

    private void Respawn()
    {
        if (transform.position.y < -5)
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }
    }

    private void LookDirection()
    {
        Vector3 direction = rb.velocity;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            rb.rotation = Quaternion.RotateTowards(rb.rotation, toRotation, 720 * Time.deltaTime);
        }
    }

    public override ServerResult GetResult()
    {
        var state = new ServerResult
        {
            Position = transform.localPosition,
            Rotation = transform.localRotation,
            Velocity = rb.velocity,
            IsGrounded = isGrounded,
        };

        return state;
    }

    public override void SetInput(ClientInput input)
    {
        move.x = input.Horizontal;
        move.y = input.Vertical;
        yaw = input.Yaw;
        pitch = input.Pitch;
        pressedJump = input.Jump;
    }
    protected override ClientInput GetInput()
    {
        var input = new ClientInput
        {
            Horizontal = Input.GetAxisRaw("Horizontal"),
            Vertical = Input.GetAxisRaw("Vertical"),
            Yaw = GetCameraRight(playerCamera),
            Pitch = GetCameraForward(playerCamera),
            Jump = Input.GetButtonDown("Jump")
        };

        return input;
    }

    protected override void SetResult(ServerResult result)
    {
        transform.localPosition = result.Position;
        transform.localRotation = result.Rotation;
        rb.velocity = result.Velocity;
        isGrounded = result.IsGrounded;
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

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position + new Vector3(0, 0.05f, 0), 0.1f);
    }

    [Command]
    private void CmdJumpEffects()
    {
        if (!isServer) { return; }
        RpcJumpEffects();
    }

    [ClientRpc]
    private void RpcJumpEffects()
    {
        audioSource.PlayOneShot(jumpSound);
        ParticleSystem jp = Instantiate(jumpParticle, transform.position, transform.rotation * Quaternion.Euler(90f, 0f, 0f));
    }
}
