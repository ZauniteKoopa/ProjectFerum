using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatZoneSensor : AttackBox {

    //Timeout variables
    public float zoneDuration;
    private float spawnTimer;
    private bool timeOut;

    //Dataset that keeps track of everyone effected
    public HashSet<Collider2D> effected;

    //Sets move up initially
    public void initialSetup(IMove move, string target, float duration) {
        if (duration <= 0)
            throw new System.Exception("Error: Negative or zero duration");

        effected = new HashSet<Collider2D>();
        zoneDuration = duration;
        currentMove = move;
        enemyTag = target;
        priority = 0;

    }

    //Sets clone up
    public void aggressiveSetup() {
        Transform clone = Object.Instantiate(transform, currentMove.getBasis().transform);
        StatZoneSensor cloneSensor = clone.GetComponent<StatZoneSensor>();
        cloneSensor.currentMove = currentMove;
        cloneSensor.effected = new HashSet<Collider2D>();
        clone.parent = null;
    }

    //Update method that calculates timeout
    void FixedUpdate() {

        spawnTimer += Time.deltaTime;

        if(spawnTimer >= zoneDuration && !timeOut) {
            timeOut = true;

            Object.Destroy(gameObject);
        }
    }

    //Upon Trigger with an entity, apply stat effect to enemy
    void OnTriggerEnter2D(Collider2D entity) {
        if(entity.tag == enemyTag && !effected.Contains(entity)) {
            effected.Add(entity);
            currentMove.enactEffects(entity);
        }
    }

    //Accounts for dashes
    void OnTriggerStay2D(Collider2D entity) {
        if (entity.tag == enemyTag && !effected.Contains(entity)){
            effected.Add(entity);
            currentMove.enactEffects(entity);
        }
    }

    //Upon exit, revert effects of enemy
    void OnTriggerExit2D(Collider2D entity) {
        if(entity.tag == enemyTag) {
            effected.Remove(entity);
            ZoneControl zoneMove = (ZoneControl)currentMove;
            zoneMove.revertEffects(entity);
        }
    }
}
