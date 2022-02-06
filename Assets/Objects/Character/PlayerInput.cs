using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerInput : NetworkBehaviour {

    // config
    public float MoveVectNetworkDeadzone = .1f;

    // state
    Vector2 _lastMoveVect = Vector2.zero;
    bool _lastJumpInput = false;

    // references
    PlatformerCharControl charControl;

    void Start() {
        charControl = GetComponent<PlatformerCharControl>();
    }
    void Update() {
        if(!isLocalPlayer) return;

        Vector2 moveVect = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        charControl.Move(moveVect);
        charControl.JumpInputDown = Input.GetButton("Jump");

        if(isServer) return;
        bool jumpInput = Input.GetButton("Jump");
        if (jumpInput != _lastJumpInput) {
            _lastJumpInput = jumpInput;
            charControl.NetworkSetJumpInputDown(Input.GetButton("Jump"));
        }
        if (Vector2.Distance(moveVect, _lastMoveVect) > MoveVectNetworkDeadzone) {
            charControl.NetworkMove(moveVect);
            _lastMoveVect = moveVect;
        }
    }
}
