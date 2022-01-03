using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Black.ClientSidePrediction;

public class PlayerMovement : MovementEntity
{
    [Header("Tweakables:")]
    [SerializeField]
    private float movementForce = 1f;
    [SerializeField]
    private float jumpForce = 5f;
    [SerializeField]
    private LayerMask groundLayer;
    private Vector3 forceDirection = Vector3.zero;
    private Vector2 move;
    private bool pressedJump;
    private bool isGrounded;
    private Vector3 yaw, pitch;

    private Camera playerCamera;
    private Animator animator;
    private Rigidbody rb;

    protected override void Start()
    {
        rb = this.GetComponent<Rigidbody>();
        animator = this.GetComponent<Animator>();
        playerCamera = GameObject.FindObjectOfType<Camera>();
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
        forceDirection += move.x * yaw * movementForce;
        forceDirection += move.y * pitch * movementForce;

        rb.AddForce(forceDirection, ForceMode.Impulse);
        forceDirection = Vector3.zero;

        isGrounded = Physics.CheckSphere(transform.position - new Vector3(0, 0, 0), 0.25f, groundLayer);

        if (pressedJump && isGrounded)
        {
            rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
            animator.SetBool("IsJumping", true);
        }
        else
        {
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

        Vector3 direction = rb.velocity;
        direction.y = 0f;

        if (move.sqrMagnitude > 0.1f && direction.sqrMagnitude > 0.1f)
        {
            Quaternion toRotate = Quaternion.LookRotation(direction, Vector3.up);
            this.rb.rotation = Quaternion.RotateTowards(rb.rotation, toRotate, 30f);
        }
        else
        {
            rb.angularVelocity = Vector3.zero;
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
            Jump = Input.GetKey(KeyCode.Space)
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
}
