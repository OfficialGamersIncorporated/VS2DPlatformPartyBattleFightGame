using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Gamemode_KeepAway : Gamemode {

    public float GamemodeTime = 120;
    public Head_Decap HeadPrefab;

    [Server] public void SpawnHead() {
        Head_Decap head = Instantiate<Head_Decap>(HeadPrefab, NetworkManager.startPositions[0].position, new Quaternion());
        NetworkServer.Spawn(head.gameObject);
    }
    [Server] public override IEnumerator StartGamemodeLoop() {
        while(true) {
            yield return StandardYieldForPlayers();
            RespawnAllPlayers(false, true, true);
            yield return new WaitForSecondsRealtime(.5f);
            DisplayText = GamemodeName;

            SpawnHead();

            yield return new WaitForSecondsRealtime(2);
            yield return StandardCountdown();
            
            // timer
            for(int i = 0; i < GamemodeTime; i++) {
                DisplayText = (GamemodeTime - i).ToString();
                yield return new WaitForSeconds(1);

                if(GetPlayersWithAHead().Count <= 0 && GameObject.FindGameObjectsWithTag("Finish").Length <= 0)
                    SpawnHead();
            }

            // overtime if no-one has a head.
            DisplayText = "Overtime";
            GameCompletionState completionState = null;
            while(completionState == null || completionState.Complete == false) {
                completionState = GetGameCompletionState();
                yield return new WaitForEndOfFrame();
            }

            EndGame(completionState);
            yield return new WaitForSeconds(3);
        }
    }

    [Server] public override GameCompletionState GetGameCompletionState() {
        if(GameObject.FindGameObjectsWithTag("Finish").Length > 0) return new GameCompletionState();

        List<HealthManager> playersAlive = GetPlayersWithAHead();

        if(playersAlive.Count == 1) {
            return new GameCompletionState(new List<HealthManager> { playersAlive[0] }, true); // winner
        } else {
            return new GameCompletionState(new List<HealthManager>(), true); // tie
        }
    }

}
