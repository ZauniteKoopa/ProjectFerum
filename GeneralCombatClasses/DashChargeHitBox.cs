using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashChargeHitBox : AttackBox
{
    //Reference variables
    private PKMNEntity entity;

    //Boolean locking variable
    private bool dashing;

    //Vector variables for recoil
    private Vector2 dashVector;
    private const float RECOIL_VAL = 110f;
    private const float RECOIL_DURATION = 0.2f;

    //Set reference variable to entity at the start of the game
    void Awake() {
        entity = GetComponent<PKMNEntity>();
    }

    //Accessor method to dashing
    public bool isDashing() {
        return dashing;
    }

    //Set properties of given hitbox and execute a dash towards mouse (Player)
    //  Pre: move == a dashing move, dashStrength > 0, time > 0, newPriority is between 1 and 10
    public IEnumerator executeDash(IMove move, float dashStrength, float time, int newPriority) {
        //Calculate vector
        Battle.updatePlayerAOrientation(transform);
        dashVector = Battle.dirKnockbackCalc(transform.position, dashStrength);

        yield return StartCoroutine(executeDash(move, dashVector, time, newPriority));  //Actually execute dash
    }

    //Executes dash away from mouse
    public IEnumerator executeDashAwayMouse(IMove move, float dashStrength, float time, int newPriority) {
        //Calculate vector
        Battle.updatePlayerAOrientation(transform);
        dashVector = Battle.dirKnockbackCalc(transform.position, dashStrength);
        dashVector *= -1;

        yield return StartCoroutine(executeDash(move, dashVector, time, newPriority));  //Actually execute dash
    }

    //Set properties of given hitbox and execute a dash towards the target (Enemy)
    public IEnumerator executeDashTowards(IMove move, float dashStrength, float time, int newPriority, Transform target) {
        Battle.updateEnemyAOrientation(transform, target, true);
        dashVector = Battle.dirKnockbackCalc(transform.position, target.position, dashStrength);
        yield return StartCoroutine(executeDash(move, dashVector, time, newPriority));
    }

    //Set properties of given hibox and execute a dash away from target (Enemy)
    public IEnumerator executeDashAway(IMove move, float dashStrength, float time, int newPriority, Transform target) {
        Battle.updateEnemyAOrientation(transform, target, false);
        dashVector = Battle.dirKnockbackCalc(target.position, transform.position, dashStrength);
        yield return StartCoroutine(executeDash(move, dashVector, time, newPriority));
    }

    //Execute dash with a set vector already in mind
    //  Pre: move == dashing move, dashVector != zero vector, time > 0, new Priority is between 1 & 10
    public IEnumerator executeDash(IMove move, Vector2 dashVector, float time, int newPriority) {
        //Set general hitbox properties
        currentMove = move;
        priority = newPriority;
        Animator anim = entity.GetComponent<Animator>();

        //Alter properties of character
        enemyTag = (transform.tag == "Player" || transform.tag == "PlayerRecovery") ? "Enemy" : "Player";
        enemyAttackTag = (transform.tag == "Player" || transform.tag == "PlayerRecovery") ? "EnemyAttack" : "PlayerAttack";
        transform.tag = (transform.tag == "Player" || transform.tag == "PlayerRecovery") ? "PlayerAttack" : "EnemyAttack";

        if(!entity.getAssist())
            entity.getController().canMove = false;

        //Set dashing variables
        dashing = true;
        float curTime = 0.0f;

        GetComponent<Rigidbody2D>().AddForce(dashVector);
        Battle.updatePlayerAOrientation(entity.transform);
        anim.SetBool("Dashing", true);

        //Dashing
        while (dashing && curTime < time && !entity.isStunned()) {
            yield return new WaitForFixedUpdate();
            curTime += Time.deltaTime;
        }

        //Check if player is recoiling (dash was interuppted)
        if (dashing) {
            dashing = false;
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        }

        //Reset properties of character
        transform.tag = (transform.tag == "PlayerAttack") ? "Player" : "Enemy";

        if (!entity.isStunned() && entity.isAlive())
            entity.getController().canMove = true;

        anim.SetBool("Dashing", false);
    }

    //Checks in the case of enemy collisions
    void OnCollisionEnter2D(Collision2D collision) {
        Collider2D collider = collision.collider;

        if(collider.tag == enemyTag && dashing) {                            //If hit an enemy, do damage to enemy and recoil
            currentMove.enactEffects(collider);
            StartCoroutine(recoil());
        }else if(collider.tag == enemyAttackTag && dashing) {                //If hit an enemy attack, gain damage
            IMove enemyMove = collider.GetComponent<AttackBox>().currentMove;
            enemyMove.enactEffects(GetComponent<Collider2D>());

            //If player is stunned after damage or priority of enemy attack is higher, recoil
            if (entity.isStunned() || GetComponent<AttackBox>().priority >= priority)
                StartCoroutine(recoil());
        }else if(collider.tag == "Platform"){
            StartCoroutine(recoil());
        }
    }

    //For attack moves
    void OnTriggerEnter2D(Collider2D collider){

        if (collider.tag == enemyAttackTag && dashing) {
            IMove enemyMove = collider.GetComponent<AttackBox>().currentMove;
            enemyMove.enactEffects(GetComponent<Collider2D>());

            //If player is stunned after damage or priority of enemy attack is higher, recoil
            if (entity.isStunned() || collider.GetComponent<AttackBox>().priority >= priority)
                StartCoroutine(recoil());
        }

    }

    //Checks in the case of enemy collisions
    void OnCollisionStay2D(Collision2D collision) {
        Collider2D collider = collision.collider;

        if (collider.tag == enemyTag && dashing) {                               //If hit an enemy, do damage to enemy and recoil
            currentMove.enactEffects(collider);
            StartCoroutine(recoil());
        }else if (collider.tag == enemyAttackTag && dashing){                    //If hit an enemy attack, gain damage
            IMove enemyMove = collider.GetComponent<AttackBox>().currentMove;
            enemyMove.enactEffects(GetComponent<Collider2D>());

            //If player is stunned after damage or priority of enemy attack is higher, recoil
            if (entity.isStunned() || collider.GetComponent<AttackBox>().priority >= priority) {
                StartCoroutine(recoil());
            }
        }
    }

    //Recoil method
    //  Pre: Player collides with either an enemy or an enemyAttack with higher priority
    //  Post: Player will recoil back 
    IEnumerator recoil() {
        dashing = false;

        //Create vector variable
        Vector2 recoilVector = new Vector2(dashVector.x, dashVector.y);
        recoilVector.Normalize();
        recoilVector *= -1 * RECOIL_VAL;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.velocity = Vector2.zero;
        rb.AddForce(recoilVector);
        yield return new WaitForSeconds(RECOIL_DURATION);
        rb.velocity = Vector2.zero;
    }

}
