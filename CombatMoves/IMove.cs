using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMove {

    //Executes move with the use of a move's transform and associated behaviors upon player input
    IEnumerator execute();

    //Executes move for the enemy with a selected target (a player or the player's attack)
    IEnumerator enemyExecute(Transform target);

    //Method used to calculate and send damage and knockback to enemy upon hitbox contact. Could also add effects upon hit
    void enactEffects(Collider2D enemy);

    //Method used for cooldown regeneration and status decay. This method can be empty if it's a basic melee move
    void regen();

    //Accessor method concerning status of a move. If returns true, the move can execute. Otherwise, it won't
    bool canRun();

    //Accessor method concerning basis
    PKMNEntity getBasis();
}
