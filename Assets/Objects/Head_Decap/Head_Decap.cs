using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Head_Decap : NetworkBehaviour {

    // reference
    new Rigidbody2D rigidbody;
    public Collider2D collectionTrigger;
    public Collider2D mainCollider;
    public ParticleSystem DestroyParticles;
    public ParticleSystem GlowParticles;

    // config
    public Vector2 StartVelocity = Vector2.up * 5;
    public float DelayBeforeCanBePickedUp = 1;

    // state
    //[SyncVar] bool Collectable = false;
    public HealthManager OriginalOwner;
    public HealthManager PreviousOwner;

    void Start() {
        rigidbody = GetComponent<Rigidbody2D>();

        rigidbody.velocity += StartVelocity;

        if (isServer)
            Invoke("SetCollectable", DelayBeforeCanBePickedUp);
    }

    void Update() {

    }

    [Server] public void SetCollectable() {
        //Collectable = true;
        collectionTrigger.enabled = true;
    }
    [Server] public void Destroy() {
        NetworkServer.Destroy(gameObject);
    }
    [ClientRpc] public void Hide() {
        DestroyParticles.Play();
        GlowParticles.Stop();
        gameObject.GetComponent<SpriteRenderer>().enabled = false;
        mainCollider.enabled = false;
    }

    [ServerCallback] private void OnTriggerEnter2D(Collider2D collision) {
        HealthManager collectingChar = collision.GetComponent<HealthManager>();
        if (collectingChar) {
            print(collectingChar.name + " collected a head.");
            if(collectingChar != PreviousOwner) {
                //collectingChar.IncrementHeads(1);
                collectingChar.AddHeads(new List<HealthManager> { OriginalOwner });
                Destroy(); // if this was replace with an Invoke then the game wouldn't end immediately once there is only one player with a head left because the game waits for all head objects to despawn.
            } else {
                collectionTrigger.enabled = false;
                Invoke("Destroy", 5);
                Hide();
            }

        }
    }
}
