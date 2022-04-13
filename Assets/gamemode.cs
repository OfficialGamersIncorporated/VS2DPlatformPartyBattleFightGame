using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class gamemode : NetworkBehaviour {

    public TextMeshProUGUI DisplayTextObj;
    [SyncVar(hook = nameof(DisplayTextChanged))]public string DisplayText;
    [SyncVar(hook = nameof(DisplayColorChanged))]public Color DisplayColor;

    public Color DefaultTextColor;

    NetworkManager_Headgame manager;

    private void Start() {
        //DontDestroyOnLoad(this);
        manager = GameObject.FindGameObjectWithTag("GameController").GetComponent<NetworkManager_Headgame>();
        //manager = GetComponentInParent<NetworkManager_Headgame>();
        DisplayTextChanged(DisplayText, DisplayText);
        DisplayColorChanged(DisplayColor, DisplayColor);


        //manager.ServerStart.AddListener(StartGame);
        if(isServer) Invoke("StartGame", 1);
    }
    [Server] void StartGame() {
        StartCoroutine(RunGame());
    }
    [Server] public IEnumerator RunGame() {
        while(true) {
            print("Waiting for players...");
            DisplayColor = DefaultTextColor;
            DisplayText = "Waiting for players...";
            yield return new WaitUntil(() => GetReadyPlayers() >= 2 && AreAllPlayersReady());
            RespawnAllPlayers(true);
            //manager.SetAllPlayersLocked(true);
            print("Enough players have joined. Counting down...");

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


            GameCompletionState completionState = null;
            while(completionState == null || completionState.Complete == false) {
                completionState = GetGameCompletionState();
                yield return new WaitForEndOfFrame();
            }
            if (completionState.Winners.Count <= 0) { // tie
                DisplayColor = DefaultTextColor;
                DisplayText = "Nobody wins :(";
                print("tie/nobody wins.");
            } else { // one winner
                DisplayColor = completionState.Winners[0].DisplayColor;
                DisplayText = "WINNER";
                print(completionState.Winners[0].ToString() + " is the winner");
            }
            manager.SetAllPlayersLocked(true);
            yield return new WaitForSeconds(3);
        }
    }
    
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
    [Server] public void RespawnAllPlayers(bool locked = false) {
        manager.SetAllPlayersLocked(locked);
        NetworkManager.startPositionIndex = Random.Range(0, NetworkManager.startPositions.Count);
        foreach(KeyValuePair<int, NetworkConnectionToClient> otherConnection in NetworkServer.connections) {
            HealthManager character = manager.GetPlayerCharacter(otherConnection.Value);
            if(character) {
                Transform startPosition = manager.GetStartPosition();
                //character.Heads = 1;
                //character.HeadOwners.Clear();
                //character.HeadOwners.Add(character);
                character.HeadOwners = new List<HealthManager> { character };
                character.SetHeads(character.HeadOwners);
                character.transform.position = startPosition.position;
                character.GetComponent<PlatformerCharControl>().Teleport(startPosition.position);
            }
        }
    }
    void DisplayTextChanged(string oldText, string newText) {
        DisplayTextObj.text = newText;
    }
    void DisplayColorChanged(Color oldColor, Color newColor) {
        DisplayTextObj.color = newColor;
    }
    [Server] GameCompletionState GetGameCompletionState() {
        if(GameObject.FindGameObjectsWithTag("Finish").Length > 0) return new GameCompletionState();

        int playersAlive = 0;
        HealthManager winner = null;
        foreach(KeyValuePair<int, NetworkConnectionToClient> otherConnection in NetworkServer.connections) {
            HealthManager character = manager.GetPlayerCharacter(otherConnection.Value);
            if(character) {
                if(character.HeadOwners.Count > 0) {
                    playersAlive++;
                    winner = character;
                }
            }
        }
        if(playersAlive > 1) {
            return new GameCompletionState(); // not over
        } else if (playersAlive == 1) {
            return new GameCompletionState(new List<HealthManager> {winner}, true); // winner
        } else {
            return new GameCompletionState(new List<HealthManager>(), true); // tie
        }
    }
}

public class GameCompletionState {
    public List<HealthManager> Winners = new List<HealthManager>();
    public bool Complete = false;

    public GameCompletionState (List<HealthManager> winners, bool complete) {
        Winners = winners;
        Complete = complete;
    }
    public GameCompletionState () {
        Winners = new List<HealthManager>();
        Complete = false;
    }
}