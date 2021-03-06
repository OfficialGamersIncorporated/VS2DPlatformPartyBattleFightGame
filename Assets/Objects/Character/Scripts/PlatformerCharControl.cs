using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;

public class PlatformerCharControl : NetworkBehaviour {

    [Header("Input"), SyncVar] 
    public Vector2 MoveVector = Vector2.zero;
    [SyncVar] public Vector2 LookVector = Vector2.right;
    [SyncVar] public bool JumpInputDown = false;
    private bool JumpInputThisFrame = false;
    private bool _LastJumpInputDown = false;

    [Header("Config")]
    public float GroundMoveSpeed = 10;
    public float AirMoveSpeed = 10;
    public float GroundAcceleration = 40;
    public float AirAcceleration = 20;
    public float GravityMultiplier = 4;
    [Tooltip("The gravity multiplier will be set to this once the apex of the jump has been reached or once the player releases the jump button.")]
    public float GravityMultiplier_PostApex = 8;
    public bool MovementSnapping = true;
    public float MovementSnappingThreshold = .25f;
    public bool FaceMoveDirection = false;
    public float AttackingDeceleration = .1f;
    public float MaxWalkableSlopeAngle = 46;
    public float JumpPower = 50;
    public float JumpBufferWindow = .2f;
    public float CoyoteTimingWindow = .2f;

    [Header("State")]
    public bool IsGrounded = false;
    private float _LastJumpThisFrameTick = -100; // jump buffering
    private float _LastGroundedTick = -100; // coyote timing
    [Tooltip("Jumping will be disabled for this many frames. _LastGroundedTick will not be set either.")]
    private int _JumpingDisabledFrames = 0;

    [Header("References")]
    new private Rigidbody2D rigidbody; // A component that handles physics like gravity, velocity, and acceleration.
    new private Collider2D collider; // A component that handles collisions with other objects.
    private SpriteRenderer MainRenderer; // A component that renders a 2d sprite based on the transform component's data.
    private Animator animator;

    public void Move(Vector2 moveVect) { // This should be used when setting MoveVector instead of setting the value directly so its magnitude gets clamped.
        if(IsAttacking()) return;
        MoveVector = Vector2.ClampMagnitude(moveVect, 1);
    }
    public void Attack() {
        if(animator && !IsAttacking()) {
            animator.SetTrigger("Attack");
        }
    }
    public void Look(Vector2 lookVect) { // This should be used when setting LookVector instead of setting the value directly.
        if (lookVect.magnitude > .1f && !IsAttacking()) // && IsGrounded
            LookVector = lookVect.normalized;
    }
    [ClientRpc] public void Push(Vector2 pushForce) {
        rigidbody.AddForce(pushForce, ForceMode2D.Impulse);
        IsGrounded = false;
    }
    [ClientRpc] public void SetLocked(bool locked) {
        rigidbody.isKinematic = locked;
    }
    [ClientRpc] public void Teleport(Vector3 position) {
        transform.position = position;
    }
    bool IsAttacking() {
        if(animator)
            return animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack");
        else
            return false;
    }
    void Awake() {
        // This funcion is called once when the object first loads before any Update functions get called.

        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        MainRenderer = GetComponent<SpriteRenderer>();

        if(rigidbody.gravityScale != 1) Debug.LogWarning(rigidbody.ToString() + "'s gravityScale value has been changed. Set " + this.ToString() + ".GravityMultiplier instead.", this);
    }
    void OnCollisionStay2D(Collision2D collision) {
        // This function gets called every physics frame before FixedUpdate so long as the character is touching something (like the ground).

        ContactPoint2D[] contacts = new ContactPoint2D[collision.contactCount];
        collision.GetContacts(contacts);
        foreach(ContactPoint2D contact in contacts) {
            if(Vector2.Angle(contact.normal, Vector2.up) <= MaxWalkableSlopeAngle)
                IsGrounded = true;
        }
    }
    void FixedUpdate() {
        // This function is called every physics frame.

        // locals
        float deltaTime = Time.fixedDeltaTime; // Change to the appropriate version of DeltaTime if changed from FixedUpdate.
        if(IsGrounded && _JumpingDisabledFrames <= 0) _LastGroundedTick = Time.time;
        bool softIsGrounded = Time.time - _LastGroundedTick < CoyoteTimingWindow; // TODO add coyote timing
        JumpInputThisFrame = false;
        if (_LastJumpInputDown != JumpInputDown) {
            _LastJumpInputDown = JumpInputDown;
            JumpInputThisFrame = JumpInputDown;
        }
        if(JumpInputThisFrame)
            _LastJumpThisFrameTick = Time.time;
        bool softJumpInputThisFrame = Time.time - _LastJumpThisFrameTick < JumpBufferWindow;

        // facing direction
        if(FaceMoveDirection && MoveVector.magnitude > .1)
            transform.localScale = new Vector3(Convert.ToInt32(MoveVector.x > 0) * 2 - 1, 1, 1);
        else if (!FaceMoveDirection)
            transform.localScale = new Vector3(Convert.ToInt32(LookVector.x > 0) * 2 - 1, 1, 1);

        // Movement snapping
        if (IsGrounded && MovementSnapping && (rigidbody.velocity.x >= 0) != (MoveVector.x >= 0) && Mathf.Abs(MoveVector.x) > MovementSnappingThreshold) {
            rigidbody.velocity = new Vector2(0, rigidbody.velocity.y);
        }

        // Jumping
        //print("softJumpInputThisFrame: " + softJumpInputThisFrame.ToString() + ", softIsGrounded: " + softIsGrounded.ToString() + ", IsGrounded: " + IsGrounded); // debug
        if (softJumpInputThisFrame && softIsGrounded && _JumpingDisabledFrames <= 0 && !IsAttacking()) {

            _JumpingDisabledFrames = 2;
            _LastJumpThisFrameTick = -100; // if this is not done then softJumpInputThisFrame will stay true for the full JumpBufferWindow. It needs to stay true just until it is used.
            _LastGroundedTick = -100; // If either softIsGrounded or softJumpInputThisFrame isn't
            //IsGrounded = false;
            //JumpInputThisFrame = false;
            rigidbody.velocity += Vector2.up * JumpPower;
            if(rigidbody.velocity.y < JumpPower)
                rigidbody.velocity = new Vector2(rigidbody.velocity.x, JumpPower);

            if(animator)
                animator.SetTrigger("Jump");
        }

        // Gravity
        if((rigidbody.velocity.y > 0 && JumpInputDown) || (IsGrounded && _JumpingDisabledFrames <= 0))
            rigidbody.gravityScale = GravityMultiplier;
        else
            rigidbody.gravityScale = GravityMultiplier_PostApex;

        // Apply movement
        float moveSpeed = GroundMoveSpeed;
        float acceleration = GroundAcceleration;
        if(IsAttacking()) {
            moveSpeed = 0;
            acceleration = AttackingDeceleration;
        } else if (!IsGrounded) {
            moveSpeed = AirMoveSpeed;
            acceleration = AirAcceleration;
        }
        // normally you shouldn't directly set rigidbody.velocity but it's sometimes nessesary.
        if(rigidbody.isKinematic)
            rigidbody.velocity = Vector2.zero;
        else
            rigidbody.velocity = Vector2.MoveTowards(rigidbody.velocity, new Vector2(MoveVector.x * moveSpeed, rigidbody.velocity.y), acceleration * deltaTime);

        // Animation
        if (animator) {
            animator.SetFloat("CurrentMoveSpeed", Mathf.Abs(MoveVector.x) * moveSpeed);
            animator.SetBool("IsGrounded", IsGrounded);
        }

        // Reset at the end of the frame. Will be set back to true by OnCollisionStay2D.
        IsGrounded = false;
        if (_JumpingDisabledFrames > 0) _JumpingDisabledFrames--;
    }

    // network trash that is more complicated than it needs to be
    [Command] public void NetworkSetJumpInputDown(bool jumpInput) {
        JumpInputDown = jumpInput;
    }
    [Command] public void NetworkMove(Vector2 moveVect) {
        Move(moveVect);
    }
    [Command] public void NetworkLook(Vector2 lookVect) {
        Look(lookVect);
    }
    [Command] public void NetworkAttack() {
        Attack();
    }
}
