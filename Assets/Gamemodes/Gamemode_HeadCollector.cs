using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class Gamemode_HeadCollector : Gamemode {


    [Server] public override IEnumerator StartGamemodeLoop() {
        while(true) {
            yield return StandardYieldForPlayers();
            RespawnAllPlayers(true, true);
            yield return new WaitForSecondsRealtime(.5f);
            DisplayText = GamemodeName;
            yield return new WaitForSecondsRealtime(2);
            yield return StandardCountdown();

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

        if(playersAlive.Count > 1) {
            return new GameCompletionState(); // not over
        } else if (playersAlive.Count == 1) {
            return new GameCompletionState(new List<HealthManager> {playersAlive[0]}, true); // winner
        } else {
            return new GameCompletionState(new List<HealthManager>(), true); // tie
        }
    }
}