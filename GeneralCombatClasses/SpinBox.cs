using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinBox : AttackBox {

    //Hashset that contains all the enemies hit
    private HashSet<Collider2D> hit;

    //Method that manually sets the hitbox up
    public void offensiveSetup(IMove move, int priority, float spinSize) {
        currentMove = move;
        this.priority = priority;

        string userType = move.getBasis().tag;
        bool isPlayer = userType == "Player" || userType == "PlayerRecovery";
        enemyTag = (isPlayer) ? "Enemy" : "Player";
        enemyAttackTag = (isPlayer) ? "EnemyAttack" : "PlayerAttack";
        tag = (isPlayer) ? "PlayerAttack" : "EnemyAttack";

        transform.localScale = new Vector3(spinSize, spinSize, 1);
    }

    //Collider method that applies effects to enemies not already hit
    void OnTriggerEnter2D(Collider2D target) {
        if(target.tag == enemyTag) {
            if(hit == null)
                hit = new HashSet<Collider2D>();
            
            if(!hit.Contains(target)) {
                hit.Add(target);
                currentMove.enactEffects(target);
            }
        }
    }
}
