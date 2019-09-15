using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuickAttack : ISecMove
{
    //Innate attack variables
    private const int PWR = 40;
    private const int TYPE = BaseStat.NORMAL;
    private const float KNOCKBACK_VAL = 0f;
    private const float DASH_STRENGTH = 500f;
    private const float DASH_DURATION = 0.11f;
    private const int PRIORITY = 2;

    //Reference variables
    private Animator anim;
    private PKMNEntity basis;
    private DashChargeHitBox dashBox;
    private string enemyTag;

    //Dash charges regeneration
    private const int MAX_CHARGES = 3;
    private const float REGEN_RATE = 3.5f;
    private int numCharges;

    //Cooldown variables
    private bool offCD;
    private float cTimer;
    private float MAX_COOLDOWN = 10.25f;

    //Constructor
    public QuickAttack(Animator anim, PKMNEntity basis) {
        //Set reference variables and any other variables concerning entity
        this.anim = anim;
        this.basis = basis;
        this.dashBox = basis.GetComponent<DashChargeHitBox>();
        this.enemyTag = (basis.tag == "Player") ? "Enemy" : "Player";

        //Establish any other variables
        numCharges = MAX_CHARGES;
        offCD = true;
        cTimer = 0f;
    }

    //Regeneration for cooldowns and charges
    public void regen() {
        //Only regens if charges are less than MAX_CHARGES
        if(numCharges < MAX_CHARGES) {
            cTimer += Time.deltaTime;    //Update time

            if(!offCD && (basis.tag == "Player" || basis.tag == "PlayerAttack"))  //Update cooldown for player UI
                basis.getController().SendMessage("updateCooldownDisplay", this);

            if(offCD && cTimer >= REGEN_RATE) {                 //When offCD, charges can regenerate
                numCharges++;
                cTimer = 0.0f;
            }else if(!offCD && cTimer >= MAX_COOLDOWN) {        //When onCD, must wait for a longer time to use move. Will gain all 3 charges afterwards
                offCD = true;
                numCharges = MAX_CHARGES;
                cTimer = 0.0f;
            }
        }
    }

    //Returns whether the move can be run in this state or not
    public bool canRun() {
        return offCD;
    }

    //Accessor method to basis / fighter
    public PKMNEntity getBasis() {
        return basis;
    }

    //Accessor method to CD Cooldown progress
    public float getCDProgress() {
        return (!offCD) ? (MAX_COOLDOWN - cTimer) / MAX_COOLDOWN : 0;
    }

    //Execute quick attack move
    public IEnumerator execute() {
        numCharges--;

        //If run out of charges, move is on cooldown
        if (numCharges <= 0) {
            offCD = false;
            cTimer = 0.0f;
        }

        basis.soundFXs.Stop();
        basis.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/Dash");
        basis.soundFXs.Play();
        yield return basis.StartCoroutine(dashBox.executeDash(this, DASH_STRENGTH, DASH_DURATION, PRIORITY));
    }

    //Assist execute attack move
    public IEnumerator assistExecute() {
        yield return basis.StartCoroutine(execute());
        basis.getController().SendMessage("assistExecuted");
    }

    //Enemy execute quick attack
    public IEnumerator enemyExecute(Transform target) {
        numCharges--;

        //If run out of charges, move is on cooldown
        if (numCharges <= 0) {
            offCD = false;
            cTimer = 0.0f;
        }

        basis.soundFXs.Stop();
        basis.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/Dash");
        basis.soundFXs.Play();

        //If it's an enemy attack, try to dodge it. If it's an enemy, try to dash towards
        if (target.tag == "PlayerAttack")
            yield return basis.StartCoroutine(dashBox.executeDashAway(this, DASH_STRENGTH, DASH_DURATION, PRIORITY, target));
        else if (target.tag == "Player")
            yield return basis.StartCoroutine(dashBox.executeDashTowards(this, DASH_STRENGTH, DASH_DURATION, PRIORITY, target));

        yield return 0;
    }

    //Upon collision on enemy, enact effects
    public void enactEffects(Collider2D enemy) {
        Vector2 knockbackVector = Battle.dirKnockbackCalc(basis.transform.position, enemy.transform.position, KNOCKBACK_VAL);
        enemy.GetComponent<Rigidbody2D>().AddForce(knockbackVector);

        //Calculates damage
        PKMNEntity enemyFighter = enemy.GetComponent<PKMNEntity>();
        int damage = Battle.damageCalc(basis.level, PWR, basis.accessStat(BaseStat.ATTACK), enemyFighter.accessStat(BaseStat.DEFENSE));

        enemyFighter.StartCoroutine(enemyFighter.receiveDamage(damage, basis));
    }
}
