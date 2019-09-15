using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitbox : AttackBox {

    void Awake() {
        string entityTag = transform.root.tag;
        enemyTag = (entityTag == "Player") ? "Enemy" : "Player";
        enemyAttackTag = (entityTag == "Player") ? "EnemyAttack" : "PlayerAttack";
    }

    //Checks if collision is an AttackHitbox
    void OnTriggerEnter2D(Collider2D collider) {
        if (collider.tag == enemyTag && currentMove != null) {
            currentMove.enactEffects(collider);           //Calculate damage
        }

        //Check priority
        if (collider.tag == enemyAttackTag)
            transform.parent.GetComponent<PKMNEntity>().StartCoroutine(recoil());
    }

    //Recoil knockback Algorithm / IEnumerator
    //  Pre: run when melee attack hits another attack that has equal or higher priority than this attack
    //  Post: Player is pushed back in the opposite direction of the melee attack
    private const float RECOIL_KNOCKBACK = 100f;
    private const float RECOIL_DURATION = 0.2f;

    IEnumerator recoil() {
        Animator anim = transform.parent.GetComponent<Animator>();
        Rigidbody2D rb = transform.parent.GetComponent<Rigidbody2D>();

        Vector2 recoilDir = new Vector2((float)anim.GetInteger("aHorizontalDirection"), (float)(anim.GetInteger("aVerticalDirection")));
        recoilDir.Normalize();
        recoilDir *= RECOIL_KNOCKBACK * -1;

        rb.AddForce(recoilDir);
        yield return new WaitForSeconds(RECOIL_DURATION);

        rb.velocity = Vector3.zero;
    }

    //Change currentMove
    //Do this before every melee attack
    public void setMove(IMove newMove, int movePriority) {
        currentMove = newMove;
        priority = movePriority;
    }
}
