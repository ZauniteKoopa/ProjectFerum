using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivationSensor : MonoBehaviour
{
    private bool activated;                 //Bool checking variable for activation
    public Controller enemyControl;         //Reference variable to the controller

    void OnTriggerEnter2D(Collider2D player) {
        if (!activated && player.tag == "Player") {
            enemyControl.SendMessage("senseActivate", player.transform);
            activated = true;
        }
    }

    void OnTriggerStay2D(Collider2D player) {
        if (!activated && player.tag == "Player") {
            enemyControl.SendMessage("senseActivate", player.transform);
            activated = true;
        }
    }
}
