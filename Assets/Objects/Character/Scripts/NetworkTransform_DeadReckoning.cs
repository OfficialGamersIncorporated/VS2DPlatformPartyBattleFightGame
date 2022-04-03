using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkTransform_DeadReckoning : NetworkBehaviour {

    // config
    public float MaxUnitOfInaccuracyPerUnitOfVelocity_OnAxis = .5f;
    public float MaxUnitOfInaccuracyPerUnitOfVelocity_OffAxis = .25f;
    public float MaxVelocityInaccuracy = 2;
    public float PositionSensitivity = .01f;
    public float VelocitySensitivity = .1f;

    // state
    [SyncVar] Vector2 LastKnownPosition = Vector2.zero;
    [SyncVar] Vector2 LastKnownVelocity = Vector2.zero;
    Vector2 _lastPosition = Vector2.zero;
    Vector2 _lastVelocity = Vector2.zero;

    // references
    new Rigidbody2D rigidbody;

    void Correct() {
        transform.position = LastKnownPosition;
        rigidbody.velocity = LastKnownVelocity;
    }
    void Start() {
        rigidbody = GetComponent<Rigidbody2D>();

        if(isServer) {
            LastKnownPosition = transform.position;
            LastKnownVelocity = rigidbody.velocity;
        }
    }
    void FixedUpdate() {
        if (isLocalPlayer) {
            if(Vector2.Distance(_lastPosition, transform.position) > PositionSensitivity)
                NetworkSetLastKnownPosition(transform.position);
            if(Vector2.Distance(_lastVelocity, rigidbody.velocity) > VelocitySensitivity)
                NetworkSetLastKnownVelocity(rigidbody.velocity);
            _lastPosition = transform.position;
            _lastVelocity = rigidbody.velocity;
        }

        if(isLocalPlayer) return;

        Vector2 velocity = rigidbody.velocity;
        Vector2 positionRelitiveToLastKnown = ((Vector2)transform.position - LastKnownPosition);

        float velocityInaccuracy = Vector2.Distance(velocity, LastKnownVelocity);
        float onAxisInaccuracy = Vector2.Distance(transform.position, LastKnownPosition);
        float offAxisInaccuracy = Vector2.Distance(transform.position, LastKnownPosition);
        if (velocity.magnitude > .5f) {
            onAxisInaccuracy = Vector3.Project(positionRelitiveToLastKnown, velocity.normalized).magnitude;
            offAxisInaccuracy = Vector3.ProjectOnPlane(positionRelitiveToLastKnown, velocity.normalized).magnitude;
        }

        if (
            (onAxisInaccuracy > rigidbody.velocity.magnitude * MaxUnitOfInaccuracyPerUnitOfVelocity_OnAxis
            || offAxisInaccuracy > rigidbody.velocity.magnitude * MaxUnitOfInaccuracyPerUnitOfVelocity_OffAxis)
            && Mathf.Max(offAxisInaccuracy, onAxisInaccuracy) > PositionSensitivity) {
            Correct();
        }
        if(velocityInaccuracy > MaxVelocityInaccuracy)
            Correct();
    }
    [Command] void NetworkSetLastKnownPosition(Vector2 pos) {
        LastKnownPosition = pos;
    }
    [Command] void NetworkSetLastKnownVelocity(Vector2 vel) {
        LastKnownVelocity = vel;
    }
    private void OnDrawGizmosSelected() {
        if(!rigidbody) rigidbody = GetComponent<Rigidbody2D>();
        Debug.DrawLine(transform.position, LastKnownPosition, Color.red, .2f);
        Debug.DrawRay(transform.position, LastKnownVelocity, Color.cyan);
        Debug.DrawRay(transform.position, rigidbody.velocity, Color.blue);
        Gizmos.DrawSphere(LastKnownPosition, .25f);
    }
}
