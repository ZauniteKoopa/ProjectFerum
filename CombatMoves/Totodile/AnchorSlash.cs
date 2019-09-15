using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnchorSlash : ISecMove {

    //Innate move variables
    private const int PWR = 40;
    private const float KNOCKBACK_VAL = 400f;
    private const int PRIORITY = 5;
    private const float SLASH_DURATION = 0.25f;
    private const float SLASH_SIZE = 0.9f;

    //Innate cooldown variables
    private const float MAX_COOLDOWN = 4f;
    private float cTimer;
    private bool offCD;

    //Reference variables
    private Animator anim;
    private Transform hitbox;
    private PKMNEntity basis;

    //Public constructor (Only used at the start)
    public AnchorSlash(Animator anim, PKMNEntity basis) {
        //Establish reference variables
        this.anim = anim;
        this.basis = basis;
        hitbox = Resources.Load<Transform>("MoveHitboxes/CircleSlash");

        //Set boolean variables
        offCD = true;
    }

    //Regeneration method for cooldown
    public void regen() {
        if(!offCD) {
            cTimer += Time.deltaTime;

            //update move cooldown for UI player UI
            if (basis.tag == "Player" || basis.tag == "PlayerAttack")
                basis.getController().SendMessage("updateCooldownDisplay", this);

            if(cTimer > MAX_COOLDOWN) {
                offCD = true;
                cTimer = 0f;
            }
        }
    }

    //Method to check if the move can be run or not
    public bool canRun() {
        return offCD && !basis.isStunned();
    }

    //Accessor method to the fighter attached to this move instance
    public PKMNEntity getBasis() {
        return basis;
    }

    //Accessor method to % of cooldown left
    //  Post: returns a number between 0 and 1 that represents the amount of cooldown left
    public float getCDProgress() {
        return (!offCD) ? (MAX_COOLDOWN - cTimer) / MAX_COOLDOWN : 0;
    }

    //Primary execute method for all 3 types of execute
    public IEnumerator execute() {
        offCD = false;

        //Set new hitbox
        Transform curHitbox = Object.Instantiate(hitbox, basis.transform);
        curHitbox.GetComponent<SpinBox>().offensiveSetup(this, PRIORITY, SLASH_SIZE);

        //Do animation
        basis.moveAttack();
        anim.SetBool("SpAttacking", true);
        yield return new WaitForSeconds(SLASH_DURATION);
        anim.SetBool("SpAttacking", false);
        basis.moveAttackLeave();

        //Destroy hitbox
        Object.Destroy(curHitbox.gameObject);
    }

    public IEnumerator enemyExecute(Transform tgt) {
        yield return basis.StartCoroutine(execute());
    }

    public IEnumerator assistExecute() {
        yield return basis.StartCoroutine(execute());
        basis.getController().SendMessage("assistExecuted");
    }

    //Method that applies effect to enemies if hit
    public void enactEffects(Collider2D threat) {
        PKMNEntity enemy = threat.GetComponent<PKMNEntity>();
        Vector2 kVect = Battle.dirKnockbackCalc(basis.transform.position, enemy.transform.position, KNOCKBACK_VAL);
        threat.GetComponent<Rigidbody2D>().AddForce(kVect);
        int damage = Battle.damageCalc(basis.level, PWR, basis.accessStat(BaseStat.ATTACK), basis.accessStat(BaseStat.DEFENSE));
        enemy.StartCoroutine(enemy.receiveDamage(damage, basis));
    }
}
