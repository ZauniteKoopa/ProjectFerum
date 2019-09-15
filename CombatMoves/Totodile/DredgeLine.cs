using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DredgeLine : ISecMove
{
    //Innate variables on damage
    private const int PWR = 100;

    //Innate variables on the projectile
    private const float CHARGE_UP = 0.2f;
    private const float PROJ_SPEED = 0.145f;
    private const int PROJ_PRIORITY = 9;
    private const float TIMEOUT = 1f;

    //Innate variables on the Knockback
    private const float KNOCKBACK_VAL = 400f;
    private const float KNOCKBACK_DURATION = 0.25f;
    private const float STUN_ADDITION = 0.1f;

    //Innate variables on the slowdown / speed debuff afterwards
    private const int STAT_TYPE = BaseStat.SPEED;
    private const float DEBUFF_FACTOR = 0.25f;
    private const float DEBUFF_DURATION = 3f;

    //Cooldown variables
    private bool offCD;
    private const float MAX_COOLDOWN = 18f;
    private float cTimer;

    //Reference variables
    private Animator anim;
    private PKMNEntity basis;
    private Transform hitbox;

    //Public constructor
    public DredgeLine(Animator anim, PKMNEntity basis) {
        this.anim = anim;
        this.basis = basis;
        hitbox = Resources.Load<Transform>("MoveHitboxes/Anchor");

        offCD = true;
    }

    //Regeneration / Update method
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

    //Accessor method that checks if move can be run
    public bool canRun() {
        return offCD && !basis.isStunned();
    }

    //Accessor method to fighter attached to this move
    public PKMNEntity getBasis() {
        return basis;
    }

    //Method that checks the progress concerning how much cooldown is left
    public float getCDProgress() {
        return (!offCD) ? (MAX_COOLDOWN - cTimer) / MAX_COOLDOWN : 0;
    }

    private const float ANIM_DURATION_FACTOR = 0.8f;

    //Main execute method for the player
    public IEnumerator execute() {
        offCD = false;
        float timer = 0f;
        basis.getController().canMove = false;
        anim.SetFloat("speed", 0f);
        Vector2 direction = Battle.dirKnockbackCalc(basis.transform.position, 1);

        Battle.updatePlayerAOrientation(basis.transform);
        anim.SetBool("SpAttacking", true);
        float animLength = ANIM_DURATION_FACTOR * anim.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animLength);
        anim.SetBool("SpAttacking", false);

        //If basis is not stunned
        if(!basis.isStunned()) {
            Transform curHitbox = Object.Instantiate(hitbox, basis.transform);
            ProjectileBehavior projProperties = curHitbox.GetComponent<ProjectileBehavior>();
            projProperties.offensiveSetup(PROJ_SPEED, 0, PROJ_PRIORITY, this);
            projProperties.setDirection(direction);

            //Wait for timeout
            timer = 0f;

            //wait for animation frame to finish before continuing script
            while(curHitbox != null && timer < TIMEOUT) {
                yield return new WaitForFixedUpdate();
                timer += Time.deltaTime;
            }

            if(curHitbox != null)
                Object.Destroy(curHitbox.gameObject);

            
            if(!basis.isStunned())
                basis.getController().canMove = true;
        }
    }

    //Main execute method for the enemy
    public IEnumerator enemyExecute(Transform tgt) {
        offCD = false;
        float timer = 0f;
        basis.getController().canMove = false;
        Vector2 direction = Battle.dirKnockbackCalc(basis.transform.position, tgt.position, 1);
        anim.SetFloat("speed", 0f);

        Battle.updatePlayerAOrientation(basis.transform);
        anim.SetBool("SpAttacking", true);
        float animLength = ANIM_DURATION_FACTOR * anim.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animLength);
        anim.SetBool("SpAttacking", false);

        //If basis is not stunned
        if(!basis.isStunned()) {
            Transform curHitbox = Object.Instantiate(hitbox, basis.transform);
            ProjectileBehavior projProperties = curHitbox.GetComponent<ProjectileBehavior>();
            projProperties.offensiveSetup(PROJ_SPEED, 0, PROJ_PRIORITY, this);
            projProperties.setDirection(direction);
            timer = 0f;

            //wait for animation frame to finish before continuing script
            while(curHitbox != null && timer < TIMEOUT) {
                yield return new WaitForFixedUpdate();
                timer += Time.deltaTime;
            }

            if(curHitbox != null)
                Object.Destroy(curHitbox.gameObject);
            
            if(!basis.isStunned())
                basis.getController().canMove = true;
        }
    }

    //Execute method for assists
    public IEnumerator assistExecute() {
        yield return basis.StartCoroutine(execute());
        basis.getController().SendMessage("assistExecuted");
    }

    public void enactEffects(Collider2D tgt) {
        PKMNEntity enemy = tgt.GetComponent<PKMNEntity>();
        int damage = Battle.damageCalc(basis.level, PWR, basis.accessStat(BaseStat.ATTACK), basis.accessStat(BaseStat.DEFENSE));
        enemy.StartCoroutine(enemy.receiveDamage(damage, basis));
        enemy.StartCoroutine(grabKnockback(enemy.transform));
    }

    private const float STUN_DELAY = 0.16f;

    //Responsible for grab knockback
    IEnumerator grabKnockback(Transform enemy) {
        //Sets up knockback variable and needed reference variables
        Vector2 kVect = Battle.dirKnockbackCalc(enemy.position, basis.transform.position, KNOCKBACK_VAL);
        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        Rigidbody2D basisRB = basis.GetComponent<Rigidbody2D>();
        PKMNEntity enemyFighter = enemy.GetComponent<PKMNEntity>();
        Controller control = enemyFighter.getController();

        //Disable movement until grab movement done
        if(!enemyFighter.getAssist())
            control.canMove = false;

        yield return new WaitForSeconds(STUN_DELAY);        //Overrides default knockback canceller (receiveDamage)
        rb.AddForce(kVect);
        float kbTimer = 0;

        while(kbTimer < KNOCKBACK_DURATION) {
            yield return new WaitForFixedUpdate();
            kbTimer += Time.deltaTime;
        }

        rb.velocity = Vector2.zero;
        basisRB.velocity = Vector2.zero;
        
        if(basis.tag == "Player" || basis.tag == "PlayerAttack" || basis.tag == "PlayerRecovery") {
            PlayerMovement mainControl = (PlayerMovement)(basis.getController());
            mainControl.selectedFighter.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        }

        if(!enemyFighter.isStunned() && !enemy.GetComponent<DashChargeHitBox>().isDashing() && !enemy.GetComponent<Animator>().GetBool("Dashing"))
            control.canMove = true;

        //Add debuff
        StatEffect debuff = new StatEffect(DEBUFF_DURATION, DEBUFF_FACTOR, STAT_TYPE);
        debuff.applyEffect(enemy.GetComponent<PKMNEntity>());
    }
}
