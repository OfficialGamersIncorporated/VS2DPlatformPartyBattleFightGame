using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlatformerCharControl : NetworkBehaviour {

    [Header("Input"), SyncVar] 
    public Vector2 MoveVector = Vector2.zero;
    [SyncVar] public bool JumpInputDown = false;
    private bool JumpInputThisFrame = false;
    private bool _LastJumpInputDown = false;

    [Header("Config")]
    public float GroundMoveSpeed = 10;
    public float AirMoveSpeed = 10;
    public float GroundAcceleration = 40;
    public float AirAcceleration = 20;
    public bool MovementSnapping = true;
    public float MovementSnappingThreshold = .25f;
    public float MaxWalkableSlopeAngle = 46;
    public float JumpPower = 50;
    public float JumpBufferWindow = .2f;
    private float _LastJumpThisFrameTick = -100;

    [Header("State")]
    public bool IsGrounded = false;

    // references
    new private Rigidbody2D rigidbody; // An object that handles physics like gravity, velocity, and acceleration.
    new private Collider2D collider; // An object that handles collisions with other objects.

    public void Move(Vector2 moveVect) { // This should be used when setting MoveVector instead of setting the value directly so its magnitude gets clamped.
        MoveVector = Vector2.ClampMagnitude(moveVect, 1);
    }
    void Start() {
        // This funcion is called once when the scene first loads before any Update functions get called.

        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
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

        float deltaTime = Time.fixedDeltaTime; // Change to the appropriate version of DeltaTime if changed from FixedUpdate.
        bool softIsGrounded = IsGrounded; // TODO add coyote timing
        JumpInputThisFrame = false;
        if (_LastJumpInputDown != JumpInputDown) {
            _LastJumpInputDown = JumpInputDown;
            JumpInputThisFrame = JumpInputDown;
        }
        if(JumpInputThisFrame)
            _LastJumpThisFrameTick = Time.time;
        bool softJumpInputThisFrame = Time.time - _LastJumpThisFrameTick < JumpBufferWindow;

        // Movement snapping
        if (IsGrounded && MovementSnapping && (rigidbody.velocity.x >= 0) != (MoveVector.x >= 0) && Mathf.Abs(MoveVector.x) > MovementSnappingThreshold) {
            rigidbody.velocity = new Vector2(0, rigidbody.velocity.y);
        }

        // Jumping
        if (softJumpInputThisFrame && softIsGrounded) {
            _LastJumpThisFrameTick = -100; // if this is not done then softJumpInputThisFrame will stay true for the full JumpBufferWindow. It needs to stay true just until it is used.
            rigidbody.velocity += Vector2.up * JumpPower;
        }

        // Apply movement
        float moveSpeed = GroundMoveSpeed;
        float acceleration = GroundAcceleration;
        if (!IsGrounded) {
            moveSpeed = AirMoveSpeed;
            acceleration = AirAcceleration;
        }
        // normally you shouldn't directly set rigidbody.velocity but it's sometimes nessesary.
        rigidbody.velocity = Vector2.MoveTowards(rigidbody.velocity, new Vector2(MoveVector.x * moveSpeed, rigidbody.velocity.y), acceleration * deltaTime);

        // Reset at the end of the frame. Will be set back to true by OnCollisionStay2D.
        IsGrounded = false;
    }

    // network trash that is more complicated than it needs to be
    [Command] public void NetworkSetJumpInputDown(bool jumpInput) {
        JumpInputDown = jumpInput;
    }
    [Command] public void NetworkMove(Vector2 moveVect) {
        Move(moveVect);
    }
}
