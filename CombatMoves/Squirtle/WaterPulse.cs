using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterPulse : ISecMove
{
    //Innate attack variabes associated with this move
    private const int PWR = 90;
    private const float KNOCKBACK_VAL = 150f;
    private const int PROJ_PRIORITY = 6;

    //Reference variables based on source
    private Animator anim;
    private PKMNEntity basis;
    private Transform hitbox;
    private string enemyTag;

    //Cooldown Timer
    private const float MAX_COOLDOWN = 10f;
    private float cTimer;
    private bool offCD;

    //Constructor
    public WaterPulse(Animator anim, PKMNEntity basis) {
        this.anim = anim;
        this.basis = basis;
        this.enemyTag = (anim.tag == "Player") ? "Enemy" : "Player";

        //Set variables for hitbox
        this.hitbox = Resources.Load<Transform>("MoveHitboxes/BubbleHitbox");
        BubbleBehavior projProperties = hitbox.GetComponent<BubbleBehavior>();
        projProperties.knockbackVal = KNOCKBACK_VAL;
        projProperties.priority = PROJ_PRIORITY;

        //set cooldown variables
        cTimer = 0.0f;
        offCD = true;
    }

    //Regen Method for cooldown
    public void regen() {

        if(!offCD) {
            cTimer += Time.deltaTime;

            //update move cooldown for UI player UI
            if (basis.tag == "Player" || basis.tag == "PlayerAttack")          
                basis.getController().SendMessage("updateCooldownDisplay", this);

            //If cooldown is over, set offCD to true and reset timer
            if (cTimer >= MAX_COOLDOWN) {
                offCD = true;
                cTimer = 0.0f;
            }
        }
    }

    //Method that returns whether or not the move can be run
    public bool canRun() {
        return offCD;
    }

    //Accessor method for basis
    public PKMNEntity getBasis() {
        return basis;
    }

    //Accessor method to CD Progress
    public float getCDProgress() {
        return (!offCD) ? (MAX_COOLDOWN - cTimer) / MAX_COOLDOWN : 0;
    }

    private const float ATTACK_DELAY = 0.15f;

    //Execute method
    public IEnumerator execute() {
        //Reduce movement for a small second
        offCD = false;
        anim.GetComponent<PKMNEntity>().moveAttack();
        Battle.updatePlayerAOrientation(basis.transform);
        anim.SetBool("SpAttacking", true);

        yield return new WaitForSeconds(ATTACK_DELAY);

        //Enable attack if entity isn't stunned
        if(!basis.isStunned() && basis.transform != null) {
            if(hitbox == null)      //ducktape fix
                this.hitbox = Resources.Load<Transform>("MoveHitboxes/BubbleHitbox");

            hitbox.GetComponent<BubbleBehavior>().setUp(basis.transform.position);
            Transform curBubble = Object.Instantiate(hitbox, basis.transform);
            curBubble.GetComponent<BubbleBehavior>().currentMove = this;
        }

        //Disable animation
        anim.SetBool("SpAttacking", false);
        anim.GetComponent<PKMNEntity>().moveAttackLeave();
    }

    //Assist Execute method
    public IEnumerator assistExecute() {
        //Reduce movement for a small second
        offCD = false;
        Battle.updatePlayerAOrientation(basis.transform);
        anim.SetBool("SpAttacking", true);

        yield return new WaitForSeconds(ATTACK_DELAY);

        //Enable attack if entity isn't stunned
        if (!basis.isStunned() && basis.transform != null) {
            if (hitbox == null)      //ducktape fix
                this.hitbox = Resources.Load<Transform>("MoveHitboxes/BubbleHitbox");

            BubbleBehavior projBehavior = hitbox.GetComponent<BubbleBehavior>();
            projBehavior.setUp(basis.transform.position);
            Transform curBubble = Object.Instantiate(hitbox, basis.transform);
            curBubble.GetComponent<BubbleBehavior>().currentMove = this;
        }

        //Disable animation
        anim.SetBool("SpAttacking", false);
        basis.getController().SendMessage("assistExecuted");
    }

    //Enemy execute method
    public IEnumerator enemyExecute(Transform target) {
        //Reduce movement for a small second
        offCD = false;
        anim.GetComponent<PKMNEntity>().moveAttack();
        Battle.updateEnemyAOrientation(basis.transform, target.transform, true);
        anim.SetBool("SpAttacking", true);

        yield return new WaitForSeconds(ATTACK_DELAY);

        //Enable attack if entity isn't stunned
        if (!basis.isStunned() && target != null && basis.transform != null) {
            hitbox.GetComponent<BubbleBehavior>().setUp(basis.transform.position, target.position);
            Transform curBubble = Object.Instantiate(hitbox, basis.transform);
            curBubble.GetComponent<BubbleBehavior>().currentMove = this;
        }

        //Disable animation
        anim.SetBool("SpAttacking", false);
        anim.GetComponent<PKMNEntity>().moveAttackLeave();
    }

    //enableEffects method
    public void enactEffects(Collider2D enemy) {
        //Calculates damage
        PKMNEntity enemyFighter = enemy.GetComponent<PKMNEntity>();
        int damage = Battle.damageCalc(basis.level, PWR, basis.accessStat(BaseStat.SPECIAL_ATTACK), enemyFighter.accessStat(BaseStat.SPECIAL_DEFENSE));

        enemyFighter.StartCoroutine(enemyFighter.receiveDamage(damage, basis));
    }
}
