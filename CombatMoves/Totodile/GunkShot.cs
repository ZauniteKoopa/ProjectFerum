using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunkShot : ISecMove
{
    //Innate move variables
    private const int INITIAL_PWR = 30;
    private const float PROJ_SPEED = 0.145f;
    private int PROJ_PRIORITY = 4;

    //Stat effect
    private const int DEBUFF_TYPE = BaseStat.SPEED;
    private const float DEBUFF_FACTOR = 0.4f;
    private const float DEBUFF_DURATION = 6f;

    //Cooldown variables
    private const float MAX_COOLDOWN = 14f;
    private float cTimer;
    private bool offCD;

    //Reference variables
    private Animator anim;
    private PKMNEntity basis;
    private Transform hitbox;

    //Public constructor
    public GunkShot(Animator anim, PKMNEntity source) {
        this.anim = anim;
        this.basis = source;
        this.hitbox = Resources.Load<Transform>("MoveHitboxes/GunkHitbox");

        offCD = true;
    }

    //Cooldown regeneration method
    public void regen() {
        if(!offCD) {
            cTimer += Time.deltaTime;

            //update move cooldown for UI player UI
            if (basis.tag == "Player" || basis.tag == "PlayerAttack")          
                basis.getController().SendMessage("updateCooldownDisplay", this);

            if(cTimer >= MAX_COOLDOWN) {
                cTimer = 0f;
                offCD = true;
            }
        }
    }

    //Method to check if this move can be run or not
    public bool canRun() {
        return offCD && !basis.isStunned();
    }

    //Accessor method to get fighter attached to move
    public PKMNEntity getBasis() {
        return basis;
    }

    //Method to get cooldown progress
    public float getCDProgress() {
        return (!offCD) ? (MAX_COOLDOWN - cTimer) / MAX_COOLDOWN : 0;
    }

    //Main execute method for player
    public IEnumerator execute() {
        //Set up move
        offCD = false;
        basis.moveAttack();
        Vector2 direction = Battle.dirKnockbackCalc(basis.transform.position, 1);

        //Shoot out projectile
        Transform curHitbox = Object.Instantiate(hitbox, basis.transform);
        MineProjectile projProps = curHitbox.GetComponent<MineProjectile>();
        projProps.offensiveSetup(PROJ_SPEED, 0f, PROJ_PRIORITY, this);
        projProps.setDirection(direction);

        //Allow animation
        Battle.updatePlayerAOrientation(basis.transform);
        anim.SetBool("SpAttacking", true);
        float animLength = anim.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animLength);

        //Stop animation
        anim.SetBool("SpAttacking", false);
        basis.moveAttackLeave();
    }

    //Main enemy execute
    public IEnumerator enemyExecute(Transform tgt) {
        //Set up move
        offCD = false;
        basis.moveAttack();
        Vector2 direction = Battle.dirKnockbackCalc(basis.transform.position, tgt.position, 1);

        //Shoot out projectile
        Transform curHitbox = Object.Instantiate(hitbox, basis.transform);
        MineProjectile projProps = curHitbox.GetComponent<MineProjectile>();
        projProps.offensiveSetup(PROJ_SPEED, 0f, PROJ_PRIORITY, this);
        projProps.setDirection(direction);

        //Allow animation
        Battle.updatePlayerAOrientation(basis.transform);
        anim.SetBool("SpAttacking", true);
        float animLength = anim.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animLength);

        //Stop animation
        anim.SetBool("SpAttacking", false);
        basis.moveAttackLeave();
    }

    //Main assist execute
    public IEnumerator assistExecute() {
        yield return basis.StartCoroutine(execute());
        basis.getController().SendMessage("assistExecuted");
    }

    //Applies effects to enemy hit
    public void enactEffects(Collider2D tgt) {
        PKMNEntity enemy = tgt.GetComponent<PKMNEntity>();
        int damage = Battle.damageCalc(basis.level, INITIAL_PWR, basis.accessStat(BaseStat.SPECIAL_ATTACK), basis.accessStat(BaseStat.SPECIAL_DEFENSE));
        enemy.StartCoroutine(enemy.receiveDamage(damage, basis));

        //Add debuff
        StatEffect debuff = new StatEffect(DEBUFF_DURATION, DEBUFF_FACTOR, DEBUFF_TYPE);
        debuff.applyEffect(enemy.GetComponent<PKMNEntity>());
    }
}
