using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneControl : ISecMove {
    //Reference variables
    private Animator anim;
    private PKMNEntity basis;
    private Transform hitbox;

    //Stat modification variables
    private float statFactor;
    private int statType;
    private const float ZONE_DURATION = 8f;

    //Cooldown variables
    private const float MAX_CD = 22f;
    private float cTimer;
    private bool offCD;

    //Public constructor
    public ZoneControl(Animator anim, PKMNEntity basis, int statType, float statFactor){
        if (statFactor <= 0f || statFactor == 1f || statType < 0 || statType >= BaseStat.BASE_STATS_LENGTH)
            throw new System.Exception("Error: Invalid parameters for stat zone modification");

        this.anim = anim;
        this.basis = basis;
        this.hitbox = Resources.Load<Transform>("MoveHitboxes/StatZone");

        this.statType = statType;
        this.statFactor = statFactor;

        string targetTag;
        if (statFactor > 1.0f)
            targetTag = basis.tag;
        else
            targetTag = (basis.tag == "Player") ? "Enemy" : "Player";

        hitbox.GetComponent<StatZoneSensor>().initialSetup(this, targetTag, ZONE_DURATION);

        offCD = true;
    }

    //Regeneration method used upon update for cooldowns
    public void regen() {
        if(!offCD) {
            cTimer += Time.deltaTime;

            //update move cooldown for UI player UI
            if (basis.tag == "Player" || basis.tag == "PlayerAttack")          
                basis.getController().SendMessage("updateCooldownDisplay", this);

            if(cTimer >= MAX_CD) {
                cTimer = 0f;
                offCD = true;
            }
        }
    }

    //method to checks if move can be run
    public bool canRun() {
        return offCD;
    }

    //Accessor method to basis
    public PKMNEntity getBasis() {
        return basis;
    }

    //Accessor method to CD Progress
    public float getCDProgress() {
        return (!offCD) ? (MAX_CD - cTimer) / MAX_CD : 0;
    }

    //Execute method for the player
    private const float SETUP_TIME = 0.3f;

    public IEnumerator execute() {
        basis.getController().canMove = false;
        offCD = false;
        anim.SetBool("Charging", true);
        float animTimer = 0f;

        //Do animation
        while(!basis.isStunned() && animTimer < SETUP_TIME) {
            yield return new WaitForFixedUpdate();
            animTimer += Time.deltaTime;
        }

        anim.SetBool("Charging", false);

        //Instantiate hitzone
        if (animTimer >= SETUP_TIME) {
            hitbox.GetComponent<StatZoneSensor>().aggressiveSetup();

            anim.SetBool("FinishedCharge", true);
            float animLength = anim.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(animLength);
            anim.SetBool("FinishedCharge", false);

            if (basis.isAlive())
                basis.getController().canMove = true;
        }
    }

    //Assist execute method for player
    public IEnumerator assistExecute() {
        yield return basis.StartCoroutine(execute());
        basis.getController().SendMessage("assistExecuted");
    }

    //Execute method for enemy
    public IEnumerator enemyExecute(Transform target) {
        yield return basis.StartCoroutine(execute());
    }

    //Enact effects method: applies stat effect on enemies with the target tag
    public void enactEffects(Collider2D effected) {
        PKMNEntity effectedEntity = effected.GetComponent<PKMNEntity>();
        effectedEntity.changeStat(statFactor, statType);
    }

    //Revert effects method: reverses stat effect on enemies with target tag
    public void revertEffects(Collider2D effected) {
        PKMNEntity effectedEntity = effected.GetComponent<PKMNEntity>();
        effectedEntity.changeStat(1 / statFactor, statType);
    }
}
