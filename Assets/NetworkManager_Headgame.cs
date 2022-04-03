using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class NetworkManager_Headgame : NetworkManager {

    public List<Color> PlayerDisplayColors = new List<Color>();

    public override void OnServerAddPlayer(NetworkConnection conn) {
        base.OnServerAddPlayer(conn);

        // refresh every players color when a new one joins. This isn't ideal but prevents duplicate colors.
        int i = 0;
        foreach(KeyValuePair<int, NetworkConnectionToClient> otherConnection in NetworkServer.connections) {
            HealthManager character = GetPlayerCharacter(otherConnection.Value);
            if (character) {
                character.DisplayColor = PlayerDisplayColors[i];
            }
            i++;
        }
    }

    public HealthManager GetPlayerCharacter(NetworkConnection conn) {
        //foreach(NetworkIdentity ownedObj in conn.clientOwnedObjects) {
        //    HealthManager healthMng = ownedObj.GetComponent<HealthManager>();
        //    if(healthMng)
        //        return healthMng;
        //}
        //return null;
        return conn.identity.GetComponent<HealthManager>();
    }
}
