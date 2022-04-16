using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkManager_Custom : NetworkManager {

    // config
    public List<Color> PlayerDisplayColors = new List<Color>();

    // references
    public List<Gamemode> GamemodePrefabs = new List<Gamemode>();
    public static NetworkManager_Custom Singleton;

    // state
    private bool _LockPlayers = false;
    public Gamemode CurrentGamemode = null;


    public override void Start() {
        base.Start();
        Singleton = this;
    }
    public override void OnServerAddPlayer(NetworkConnection conn) {
        base.OnServerAddPlayer(conn);

        HealthManager playerCharacter = GetPlayerCharacter(conn);
        if(playerCharacter) {
            //playerCharacter.HeadOwners = new List<HealthManager> { playerCharacter };
            playerCharacter.HeadOwners = new List<HealthManager>();
            playerCharacter.SetHeads(playerCharacter.HeadOwners);

            PlatformerCharControl charControl = playerCharacter.GetComponent<PlatformerCharControl>();
            charControl.SetLocked(_LockPlayers);
        }

        // refresh every players color and name when a new one joins. This isn't ideal but prevents duplicates.
        int i = 0;
        foreach(KeyValuePair<int, NetworkConnectionToClient> otherConnection in NetworkServer.connections) {
            HealthManager character = GetPlayerCharacter(otherConnection.Value);
            if (character) {
                character.DisplayColor = PlayerDisplayColors[i];
                character.DisplayName = "Player" + (i+1).ToString();
                character.SetHeads(character.HeadOwners);
            }
            i++;
        }
    }
    public override void OnStartServer() {
        base.OnStartServer();

        StartGamemode(SelectRandomGamemode());
    }
    [Server] public void ClearGamemode() {
        if(CurrentGamemode) {
            NetworkServer.Destroy(CurrentGamemode.gameObject);
        }
    }
    [Server] public void StartGamemode(Gamemode gamemode) {
        ClearGamemode();

        CurrentGamemode = Instantiate<Gamemode>(gamemode);
        NetworkServer.Spawn(CurrentGamemode.gameObject);
    }
    [Server] public Gamemode SelectRandomGamemode() {
        return GamemodePrefabs[Random.Range(0, GamemodePrefabs.Count)];
    }
    public void SetAllPlayersLocked(bool locked) {
        _LockPlayers = locked;
        foreach(KeyValuePair<int, NetworkConnectionToClient> otherConnection in NetworkServer.connections) {
            HealthManager character = GetPlayerCharacter(otherConnection.Value);
            if(character) {
                PlatformerCharControl charControl = character.GetComponent<PlatformerCharControl>();
                charControl.SetLocked(locked);
            }
        }
    }

    public HealthManager GetPlayerCharacter(NetworkConnection conn) {
        NetworkIdentity id = conn.identity;
        if(id == null) return null;
        return id.GetComponent<HealthManager>();
    }
}
