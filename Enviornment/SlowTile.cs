using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowTile : MonoBehaviour
{
    //Slow factor: slows all fighters by this amount
    public float slowFactor;

    //Upon entering tile, slow entity
    void OnTriggerEnter2D(Collider2D collider) {
        PKMNEntity fighter = collider.GetComponent<PKMNEntity>();

        if(fighter != null)
            fighter.changeStat(slowFactor, BaseStat.SPEED);
    }

    //Upon exiting tile, reverse slow on entity
    void OnTriggerExit2D(Collider2D collider) {
        PKMNEntity fighter = collider.GetComponent<PKMNEntity>();

        if(fighter != null)
            fighter.changeStat(1 / slowFactor, BaseStat.SPEED);
    }
}
