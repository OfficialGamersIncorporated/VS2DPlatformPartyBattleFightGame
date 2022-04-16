using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class HealthManager : NetworkBehaviour {

    //private int Heads = 1; // syncvar(hook = nameof(HeadsChanged))
    public List<HealthManager> HeadOwners = new List<HealthManager>();
    [SyncVar(hook = nameof(DisplayColorChanged))] public Color DisplayColor = Color.white;
    [SyncVar(hook = nameof(DisplayNameChanged))] public string DisplayName = "";
    public float DecapKnockbackForce = 5;
    public float DecapKnockupForce = 5;
    public Color NameplateLocalPlayerColor = new Color();

    public List<SpriteRenderer> HeadRenderers = new List<SpriteRenderer>();
    public List<SpriteRenderer> ColoredParts = new List<SpriteRenderer>();
    public TextMeshPro Nameplate;
    new Rigidbody2D rigidbody;
    public Rigidbody2D Head_Decap_Prefab;
    PlatformerCharControl charControl;

    [Command] public void Behead(HealthManager other) {
        //other.IncrementHeads(-1);
        other.RemoveHeads(1);

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
    void HeadsChanged() {
        //int deltaHeads = newHeads - oldHeads;
        for(int i = 0; i < HeadRenderers.Count; i++) {
            SpriteRenderer head = HeadRenderers[i];
            head.enabled = HeadOwners.Count - 1 >= i;
            if(head.enabled) {
                if(HeadOwners[i] != null) // can be null if the head wasn't previously owned by a player.
                    head.color = HeadOwners[i].DisplayColor;
                else
                    head.color = NameplateLocalPlayerColor; // todo maybe make this a different value.
            }
        }
    }
    void HeadsChanged(SyncList<HealthManager>.Operation op, int index, HealthManager oldItem, HealthManager newItem) {
        HeadsChanged();
    }
    //[Server] public void IncrementHeads(int deltaHeads) {
    //    if(deltaHeads < 0 && Heads <= 0) return; // can't lose a head if you don't have a head to lose.
    //    Heads += deltaHeads;

    //    if (deltaHeads < 0) {
    //        for(int i = 0; i < Mathf.Abs(deltaHeads); i++) {
    //            Rigidbody2D headDecapInstance = Instantiate<Rigidbody2D>(Head_Decap_Prefab);
    //            Head_Decap decapClass = headDecapInstance.GetComponent<Head_Decap>();
    //            decapClass.Owner = this;
    //            headDecapInstance.transform.position = transform.position;
    //            headDecapInstance.velocity = rigidbody.velocity;
    //            NetworkServer.Spawn(headDecapInstance.gameObject);
    //        }
    //    }
    //}
    [ClientRpc]
    public void SetHeads(List<HealthManager> headOwners) {
        HeadOwners = headOwners;
        //Heads = headOwners.Count;
        HeadsChanged();
    }
    [Server] public void AddHeads(List<HealthManager> owners) {
        foreach(HealthManager owner in owners) {
            HeadOwners.Add(owner);
        }
        //Heads = HeadOwners.Count;
        SetHeads(HeadOwners);
    }
    [Server] public void RemoveHeads(int deltaHeads) {
        if(HeadOwners.Count <= 0) return;
        deltaHeads = Mathf.Min(HeadOwners.Count, Mathf.Abs(deltaHeads));

        for(int i = 0; i < Mathf.Abs(deltaHeads); i++) {
            HealthManager headToRemove = HeadOwners[HeadOwners.Count - 1];
            HeadOwners.Remove(headToRemove);

            Rigidbody2D headDecapInstance = Instantiate<Rigidbody2D>(Head_Decap_Prefab);
            Head_Decap decapClass = headDecapInstance.GetComponent<Head_Decap>();
            decapClass.PreviousOwner = this;
            decapClass.OriginalOwner = headToRemove;
            headDecapInstance.transform.position = transform.position;
            headDecapInstance.velocity = rigidbody.velocity;
            NetworkServer.Spawn(headDecapInstance.gameObject);
        }

        SetHeads(HeadOwners);
    }
    void DisplayColorChanged(Color oldColor, Color newColor) {
        foreach(SpriteRenderer coloredPart in ColoredParts) {
            coloredPart.color = newColor;
        }
        if(isLocalPlayer)
            Nameplate.color = NameplateLocalPlayerColor;
        else
            Nameplate.color = newColor;
    }
    void DisplayNameChanged(string oldName, string newName) {
        if(isLocalPlayer)
            Nameplate.text = "YOU";
        else
            Nameplate.text = DisplayName;
    }

    void Start() {
        rigidbody = GetComponent<Rigidbody2D>();
        charControl = GetComponent<PlatformerCharControl>();
        //HeadOwners = new List<HealthManager> { this };

        DisplayNameChanged(DisplayName, DisplayName);
        DisplayColorChanged(DisplayColor, DisplayColor);

        if(isServer) {
            //HeadOwners.Clear();
            //HeadOwners.Add(this);
        }

        //HeadOwners.Callback += HeadsChanged;
        
        HeadsChanged(); // set up the visual of how many heads the player has.
    }
    private void Update() {
        Nameplate.transform.localScale = new Vector3(transform.localScale.x, 1, 1);
    }
}
