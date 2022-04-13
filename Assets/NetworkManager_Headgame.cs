using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;
using TMPro;

public class NetworkManager_Headgame : NetworkManager {

    public List<Color> PlayerDisplayColors = new List<Color>();

    public bool _LockPlayers = false;

    public UnityEvent ServerStart = new UnityEvent();

    public gamemode GamemodePrefab;

    public TextMeshProUGUI DisplayTextObj;

    public override void OnServerAddPlayer(NetworkConnection conn) {
        base.OnServerAddPlayer(conn);

        HealthManager playerCharacter = GetPlayerCharacter(conn);
        if(playerCharacter) {
            playerCharacter.HeadOwners = new List<HealthManager> { playerCharacter };
            playerCharacter.SetHeads(playerCharacter.HeadOwners);
            PlatformerCharControl charControl = playerCharacter.GetComponent<PlatformerCharControl>();
            charControl.SetLocked(_LockPlayers);
        }

        // refresh every players color when a new one joins. This isn't ideal but prevents duplicate colors.
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

        gamemode mode = Instantiate<gamemode>(GamemodePrefab);
        NetworkServer.Spawn(mode.gameObject);
        ServerStart.Invoke();
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
        //foreach(NetworkIdentity ownedObj in conn.clientOwnedObjects) {
        //    HealthManager healthMng = ownedObj.GetComponent<HealthManager>();
        //    if(healthMng)
        //        return healthMng;
        //}
        //return null;
        NetworkIdentity id = conn.identity;
        if(id == null) return null;
        return id.GetComponent<HealthManager>();
    }
}
