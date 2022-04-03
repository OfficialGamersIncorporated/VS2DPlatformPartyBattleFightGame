using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitbox_Attack : MonoBehaviour {

    // reference
    HealthManager healthMngr;

    // state
    List<HealthManager> charsHitThisAttack = new List<HealthManager>();
    private void OnTriggerStay2D(Collider2D collision) {
        print("trigger entered " + collision.ToString() );
        HealthManager otherHealthMngr = collision.GetComponent<HealthManager>();
        if(!otherHealthMngr) return;
        if(charsHitThisAttack.Contains(otherHealthMngr)) return;
        charsHitThisAttack.Add(otherHealthMngr);

        healthMngr.Attack(otherHealthMngr);
    }
    private void OnDisable() {
        charsHitThisAttack = new List<HealthManager>();
    }
    void Awake() {
        healthMngr = GetComponentInParent<HealthManager>();
    }
    void Update() {

    }
}
