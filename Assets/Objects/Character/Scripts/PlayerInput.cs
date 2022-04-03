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
        if(!isLocalPlayer) return; // this script shouldn't do anything if it's not running on the local player's character.

        Vector2 moveVect = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        charControl.Move(moveVect);
        charControl.JumpInputDown = Input.GetButton("Jump");
        if(Input.GetButtonDown("Fire1")) {
            charControl.Attack();
        }

        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 0;
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(mousePos);
        Vector2 mouseRelitivePos = (Vector2)mouseWorldPos - (Vector2)transform.position;
        charControl.Look(mouseRelitivePos.normalized);

        if(isServer) return; // code beyond this point should only run if the local player is not the host.
        bool jumpInput = Input.GetButton("Jump");
        if (jumpInput != _lastJumpInput) {
            _lastJumpInput = jumpInput;
            charControl.NetworkSetJumpInputDown(Input.GetButton("Jump"));
        }
        if (Vector2.Distance(moveVect, _lastMoveVect) > MoveVectNetworkDeadzone) {
            charControl.NetworkMove(moveVect);
            _lastMoveVect = moveVect;
        }
        charControl.NetworkLook(mouseRelitivePos.normalized);
        if(Input.GetButtonDown("Fire1")) {
            charControl.NetworkAttack();
        }
    }
}
