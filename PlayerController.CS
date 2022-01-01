using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Tweakables:")]
    [SerializeField]
    private float movementForce = 1f;
    [SerializeField]
    private float jumpForce = 5f;
    [SerializeField]
    private float maxSpeed = 5f;

    [SerializeField]
    private LayerMask groundLayer;
    private Vector3 forceDirection = Vector3.zero;
    private Vector2 move;
    private bool isJumping;
    public bool canMove;

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
        move.x = Input.GetAxis("Horizontal");
        move.y = Input.GetAxis("Vertical");

        if (!canMove)
        {
            move = Vector3.zero;
        }

        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            isJumping = true;
            animator.SetBool("IsJumping", isJumping);
        }
    }

    void FixedUpdate()
    {
        forceDirection += move.x * GetCameraRight(playerCamera) * movementForce;
        forceDirection += move.y * GetCameraForward(playerCamera) * movementForce;

        rb.AddForce(forceDirection, ForceMode.Impulse);
        forceDirection = Vector3.zero;

        if (isJumping)
        {
            rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
            isJumping = false;
            animator.SetBool("IsJumping", isJumping);
        }

        if (!IsGrounded())
        {
            animator.SetBool("IsFalling", true);
            rb.drag = 0;
        }
        else
        {
            animator.SetBool("IsFalling", false);
            rb.drag = 3.5f;
        }

        Vector3 horizontalVelocity = rb.velocity;
        horizontalVelocity.y = 0;
        if (horizontalVelocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            rb.velocity = horizontalVelocity.normalized * maxSpeed + Vector3.up * rb.velocity.y;
        }

        LookAt();
    }

    private void LookAt()
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
