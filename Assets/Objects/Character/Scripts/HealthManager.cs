using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class HealthManager : NetworkBehaviour {

    [SyncVar(hook = nameof(HeadsChanged))] public int Heads = 1;
    [SyncVar(hook = nameof(DisplayColorChanged))] public Color DisplayColor = Color.white;
    public float DecapKnockbackForce = 5;
    public float DecapKnockupForce = 5;

    public List<SpriteRenderer> HeadRenderers = new List<SpriteRenderer>();
    public List<SpriteRenderer> ColoredParts = new List<SpriteRenderer>();
    new Rigidbody2D rigidbody;
    public Rigidbody2D Head_Decap_Prefab;
    PlatformerCharControl charControl;

    [Command] public void Behead(HealthManager other) {
        other.IncrementHeads(-1);

        PlatformerCharControl otherCharControl = other.GetComponent<PlatformerCharControl>();
        Vector2 pushVector = -(other.transform.position - transform.position).normalized;
        Vector2 otherPushVector = -(transform.position - other.transform.position).normalized;
        charControl.Push(pushVector * DecapKnockbackForce + Vector2.up * DecapKnockupForce);
        otherCharControl.Push(otherPushVector * DecapKnockbackForce + Vector2.up * DecapKnockupForce);
        
    }
    [Client] public void Attack(HealthManager other) { // this is performed in this script instead of the Hitbox script because mirror is weird and a little stupid.
        if(!isLocalPlayer) return;
        print(this.ToString() + " is attacking " + other.ToString());
        Behead(other);
    }
    void HeadsChanged(int oldHeads, int newHeads) {
        //int deltaHeads = newHeads - oldHeads;
        for(int i = 0; i < HeadRenderers.Count; i++) {
            HeadRenderers[i].enabled = Heads-1 >= i;
        }
    }
    [Server] public void IncrementHeads(int deltaHeads) {
        if(deltaHeads < 0 && Heads <= 0) return; // can't lose a head if you don't have a head to lose.
        Heads += deltaHeads;

        if (deltaHeads < 0) {
            for(int i = 0; i < Mathf.Abs(deltaHeads); i++) {
                Rigidbody2D headDecapInstance = Instantiate<Rigidbody2D>(Head_Decap_Prefab);
                Head_Decap decapClass = headDecapInstance.GetComponent<Head_Decap>();
                decapClass.Owner = this;
                headDecapInstance.transform.position = transform.position;
                headDecapInstance.velocity = rigidbody.velocity;
                NetworkServer.Spawn(headDecapInstance.gameObject);
            }
        }
    }
    void DisplayColorChanged(Color oldColor, Color newColor) {
        foreach(SpriteRenderer coloredPart in ColoredParts) {
            coloredPart.color = newColor;
        }
    }

    void Start() {
        rigidbody = GetComponent<Rigidbody2D>();
        charControl = GetComponent<PlatformerCharControl>();
        
        HeadsChanged(Heads, Heads); // set up the visual of how many heads the player has.
    }
    void Update() {

    }
}
