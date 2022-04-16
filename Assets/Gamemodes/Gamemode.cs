using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class Gamemode : NetworkBehaviour {

    // config
    public int MinimumPlayers = 2;
    public string GamemodeName = "Blank";
    [Tooltip("If a player loses a head and then immediately tries to pick it up again will it be destroyed?")]
    public bool DestroyHeadsOnRecollection = true;
    [SyncVar(hook = nameof(DisplayTextChanged))] public string DisplayText;
    [SyncVar(hook = nameof(DisplayColorChanged))] public Color DisplayColor;
    public Color DefaultTextColor;

    // references
    public TextMeshProUGUI DisplayTextObj;
    protected NetworkManager_Custom manager;

    private void Start() {
        manager = NetworkManager_Custom.Singleton;
        DisplayTextChanged(DisplayText, DisplayText);
        DisplayColorChanged(DisplayColor, DisplayColor);

        Head_Decap.DestroyOnRecollection = DestroyHeadsOnRecollection;
        if(isServer) StartCoroutine(StartGamemodeLoop());
    }

    // gamestate
    [Server] public void EndGame(GameCompletionState completionState) {
        manager.SetAllPlayersLocked(true);
        if(completionState.Winners.Count <= 0) { // tie
            DisplayColor = DefaultTextColor;
            DisplayText = "Nobody wins :(";
            print("tie/nobody wins.");
        } else { // one winner
            DisplayColor = completionState.Winners[0].DisplayColor;
            DisplayText = "WINNER";
            print(completionState.Winners[0].ToString() + " is the winner");
        }
    }
    [Server] public virtual IEnumerator StartGamemodeLoop() {
        return null;
    }
    [Server] public virtual GameCompletionState GetGameCompletionState() {
        return null;
    }
    [Server] void SetRandomSpawnSeed(bool avoidCenter = false) {
        // don't always spawn player1 at the first spawnpoint in the list. Start at a random point in the list and start itterating from there.

        int startIndex = avoidCenter ? 0 : 1;
        NetworkManager.startPositionIndex = Random.Range(0, NetworkManager.startPositions.Count);

        if(NetworkServer.connections.Count >= NetworkManager.startPositions.Count) return;
        if(avoidCenter && NetworkManager.startPositionIndex > NetworkManager.startPositions.Count - NetworkServer.connections.Count)
            SetRandomSpawnSeed(avoidCenter);
    }
    [Server] public virtual void RespawnAllPlayers(bool hasOwnHead = false, bool locked = true, bool avoidCenter = false) {
        manager.SetAllPlayersLocked(locked);

        SetRandomSpawnSeed(avoidCenter);

        foreach(KeyValuePair<int, NetworkConnectionToClient> otherConnection in NetworkServer.connections) {
            HealthManager character = manager.GetPlayerCharacter(otherConnection.Value);
            if(character) {
                // set position
                Transform startPosition = manager.GetStartPosition();
                character.transform.position = startPosition.position;
                character.GetComponent<PlatformerCharControl>().Teleport(startPosition.position);

                // give/reset head(s)
                if(hasOwnHead) {
                    character.HeadOwners = new List<HealthManager> { character };
                    character.SetHeads(character.HeadOwners);
                } else {
                    character.HeadOwners = new List<HealthManager>();
                    character.SetHeads(character.HeadOwners);
                }
            }
        }
    }
    [Server] public List<HealthManager> GetPlayersWithAHead() {
        List<HealthManager> playersAlive = new List<HealthManager>();
        foreach(KeyValuePair<int, NetworkConnectionToClient> otherConnection in NetworkServer.connections) {
            HealthManager character = manager.GetPlayerCharacter(otherConnection.Value);
            if(character) {
                if(character.HeadOwners.Count > 0) {
                    playersAlive.Add(character);
                }
            }
        }
        return playersAlive;
    }

    // display updates
    public virtual void DisplayTextChanged(string oldText, string newText) {
        DisplayTextObj.text = newText;
    }
    public virtual void DisplayColorChanged(Color oldColor, Color newColor) {
        DisplayTextObj.color = newColor;
    }

    // standard functions
    [Server] public IEnumerator StandardYieldForPlayers() {
        print("Waiting for players...");
        DisplayColor = DefaultTextColor;
        DisplayText = "Waiting for players...";
        yield return new WaitUntil(() => GetReadyPlayers() >= MinimumPlayers && AreAllPlayersReady());
        print("Enough players have joined. Spawning and counting down...");
    }
    [Server] public IEnumerator StandardCountdown() {
        DisplayColor = DefaultTextColor;
        DisplayText = "3";
        yield return new WaitForSecondsRealtime(1);
        DisplayText = "2";
        yield return new WaitForSecondsRealtime(1);
        DisplayText = "1";
        yield return new WaitForSecondsRealtime(1);
        DisplayText = "GO";
        manager.SetAllPlayersLocked(false);
        print("Starting gamemode... ");
        yield return new WaitForSecondsRealtime(1);
        DisplayText = "";
    }

    // player connection
    [Server] public int GetReadyPlayers() {
        int readyConns = 0;
        foreach(KeyValuePair<int, NetworkConnectionToClient> conn in NetworkServer.connections) {
            if(conn.Value.identity) readyConns++;
        }
        return readyConns;
    }
    [Server] public bool AreAllPlayersReady() {
        foreach(KeyValuePair<int, NetworkConnectionToClient> conn in NetworkServer.connections) {
            if(conn.Value.identity == null) return false;
        }
        return true;
    } 

}

public class GameCompletionState {
    public List<HealthManager> Winners = new List<HealthManager>();
    public bool Complete = false;

    public GameCompletionState(List<HealthManager> winners, bool complete) {
        Winners = winners;
        Complete = complete;
    }
    public GameCompletionState() {
        Winners = new List<HealthManager>();
        Complete = false;
    }
}