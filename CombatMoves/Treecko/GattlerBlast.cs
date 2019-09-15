using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GattlerBlast : ISecMove
{
    //Innate move variables
    private const int SHOTGUN_PWR = 40;
    private const float SHOTGUN_KNOCKBACK = 400f;
    private const int SHOTGUN_PRIORITY = 5;
    //private const int DASH_PWR = 25;
    private const int DASH_PRIORITY = 2;

    //Stat effect variables with shotgun
    private const float REDUCTION_FACTOR = 0.8f;
    private const float STAT_DURATION = 8f;

    //Dash variables
    private const float DASH_STRENGTH = 600f;
    private const float DASH_DURATION = 0.1f;

    //Reference variables
    private Animator anim;
    private PKMNEntity basis;
    private string enemyTag;
    private DashChargeHitBox dashBox;
    private Transform shotgunBlast;
    private Transform mainHitbox;

    //Reload variables
    private const int MAX_CHARGES = 3;
    private const float RELOAD_TIME = 2.5f;
    private const float COOLDOWN = 11f;
    private const float ATTACK_CD = 0.3f;
    private float aTimer;
    private float cTimer;
    private float rTimer;
    private int numCharges;
    private bool offCD;
    private bool canAttack;

    //Constructor
    public GattlerBlast(Animator anim, PKMNEntity basis) {
        //Set reference variables and any other variables concerning entity
        this.anim = anim;
        this.basis = basis;
        this.dashBox = basis.GetComponent<DashChargeHitBox>();
        this.enemyTag = (basis.tag == "Player") ? "Enemy" : "Player";
        this.mainHitbox = Resources.Load<Transform>("MoveHitboxes/TriangleHitbox");

        //Set other variables
        canAttack = true;
        offCD = true;
        numCharges = MAX_CHARGES;
    }

    //Regen method
    public void regen() {
        float delta = Time.deltaTime;

        //Cooldown timer
        if(!offCD) {
            cTimer += delta;

            //update move cooldown for UI player UI
            if (basis.tag == "Player" || basis.tag == "PlayerAttack")          
                basis.getController().SendMessage("updateCooldownDisplay", this);

            if(cTimer >= COOLDOWN) {
                cTimer = 0f;
                numCharges = MAX_CHARGES;
                offCD = true;
            }
        }

        //Ammo regeneration timer
        if(numCharges < MAX_CHARGES && offCD) {
            rTimer += delta;

            if(rTimer >= RELOAD_TIME) {
                rTimer = 0f;
                numCharges++;
            }
        }

        //Attack timer
        if(!canAttack) {
            aTimer += delta;

            if(aTimer >= ATTACK_CD) {
                aTimer = 0f;
                canAttack = true;
            }
        }
    }

    //Accessor method that checks if the move is in a state to be executed or not
    public bool canRun() {
        return canAttack && offCD;
    }

    //Accessor method to the basis / fighter attached to this move object
    public PKMNEntity getBasis() {
        return basis;
    }

    //Accessor method to cooldown progress
    public float getCDProgress() {
        return (!offCD) ? (COOLDOWN - cTimer) / COOLDOWN : 0;
    }

    //Basic Execute method for player
    public IEnumerator execute() {
        numCharges--;       

        //If charge is 0, activate big cooldown and reset timers
        if(numCharges <= 0) {
            offCD = false;
            cTimer = 0f;
        }

        Battle.updatePlayerAOrientation(anim.transform);

        //Shotgun sound effect
        basis.soundFXs.Stop();
        basis.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/Shotgun");
        basis.soundFXs.Play();

        shotgunBlast = mainHitbox.GetComponent<ShotgunHitbox>().offensiveSetup(SHOTGUN_PRIORITY, this, SHOTGUN_KNOCKBACK);
        yield return basis.StartCoroutine(dashBox.executeDashAwayMouse(this, DASH_STRENGTH, DASH_DURATION, DASH_PRIORITY));
        canAttack = false;
        rTimer = 0f;
    }

    //Assist execute method for the player
    public IEnumerator assistExecute() {
        yield return basis.StartCoroutine(execute());
        basis.getController().SendMessage("assistExecuted");
    }

    //Enemy execute method
    public IEnumerator enemyExecute(Transform target) {
        numCharges--;

        //If charge is 0, activate big cooldown and reset timers
        if (numCharges <= 0) {
            offCD = false;
            cTimer = 0f;
        }

        Battle.updateEnemyAOrientation(anim.transform, target, true);

        //Shotgun sound effect
        basis.soundFXs.Stop();
        basis.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/Shotgun");
        basis.soundFXs.Play();

        shotgunBlast = mainHitbox.GetComponent<ShotgunHitbox>().offensiveSetup(SHOTGUN_PRIORITY, this, SHOTGUN_KNOCKBACK);
        yield return basis.StartCoroutine(dashBox.executeDashAway(this, DASH_STRENGTH, DASH_DURATION, DASH_PRIORITY, target));
        canAttack = false;
        rTimer = 0f;
    }

    //Enact effects method
    public void enactEffects(Collider2D enemy) {
        PKMNEntity enemyFighter = enemy.GetComponent<PKMNEntity>();

        //Bounds shotgunBounds = shotgunBlast.GetComponent<Collider2D>().bounds;
        int pwr;
        float offense;
        float defense;

        pwr = SHOTGUN_PWR;
        offense = basis.accessStat(BaseStat.SPECIAL_ATTACK);
        defense = enemyFighter.accessStat(BaseStat.SPECIAL_DEFENSE);

        int damage = Battle.damageCalc(basis.level, pwr, offense, defense);
        enemyFighter.StartCoroutine(enemyFighter.receiveDamage(damage, basis));

        StatEffect effects = new StatEffect(STAT_DURATION, REDUCTION_FACTOR, BaseStat.SPECIAL_DEFENSE);
        effects.applyEffect(enemyFighter);

        //THIS SENSING METHOD DOES NOT WORK
        /*if(enemy.bounds.Intersects(shotgunBounds)) {
            pwr = SHOTGUN_PWR;
            offense = basis.accessStat(BaseStat.SPECIAL_ATTACK);
            defense = enemyFighter.accessStat(BaseStat.SPECIAL_DEFENSE);
            StatEffect effects = new StatEffect(STAT_DURATION, REDUCTION_FACTOR, BaseStat.SPECIAL_DEFENSE);
            effects.applyEffect(enemyFighter);
        }else{
            pwr = DASH_PWR;
            offense = basis.accessStat(BaseStat.ATTACK);
            defense = enemyFighter.accessStat(BaseStat.DEFENSE);
        }*/
    }
}
