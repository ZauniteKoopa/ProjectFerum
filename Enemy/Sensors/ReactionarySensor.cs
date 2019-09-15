using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReactionarySensor : MonoBehaviour
{
    public Controller enemyControl;         //Reference variable to the controller
    private float reactTimer;
    private const float REACTION_TIMING = 0.75f;

    //If player enters reaction zone, have enemy react immediately
    void OnTriggerEnter2D(Collider2D threat) {
        if ((threat.tag == "PlayerAttack" || threat.tag == "Player") && enemyControl.canMove && enemyControl.GetComponent<PKMNEntity>().isAlive() && threat.transform != null)
            enemyControl.SendMessage("senseReact", threat.transform);
    }

    //If player stays in reaction zone for a period of time, activate reaction again
    void OnTriggerStay2D(Collider2D threat) {
        if(threat.tag == "PlayerAttack" || threat.tag == "Player") {
            reactTimer += Time.deltaTime;

            if (reactTimer >= REACTION_TIMING) {
                if (enemyControl.canMove && enemyControl.GetComponent<PKMNEntity>().isAlive() && threat.transform != null)
                    enemyControl.SendMessage("senseReact", threat.transform);

                reactTimer = 0f;
            }
        }
    }

    //If player exit reaction zone, reset reaction timer
    void OnTriggerExit2D(Collider2D threat) {
        reactTimer = 0.0f;
    }
}
