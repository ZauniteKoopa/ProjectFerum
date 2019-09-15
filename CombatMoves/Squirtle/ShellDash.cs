using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShellDash : ISecMove
{
    //Channel variables
    private const float MIN_PWR = 60f;
    private const float MAX_PWR = 110f;
    private const float MAX_DASH_STRENGTH = 800f;
    private const float MIN_DASH_STRENGTH = 500f;
    private const float FULL_CHANNEL = 2.5f;
    private const float MAX_CHANNEL = 3f;
    private float curPwr;
    private float curDashStrength;

    //Innate Move variables
    private const float KNOCKBACK_VAL = 10f;
    private const float DASH_DURATION = 0.35f;
    private const int PRIORITY = 7;

    //Reference variables
    private Animator anim;
    private PKMNEntity basis;
    private DashChargeHitBox dashBox;
    private string enemyTag;
    private ProgressBar progress;

    //Cooldown variables
    private bool offCD;
    private float cTimer;
    private float MAX_COOLDOWN = 10f;

    //Constructor
    public ShellDash(Animator anim, PKMNEntity basis, ProgressBar progress) {
        //Set reference variables and any other variables concerning entity
        this.anim = anim;
        this.basis = basis;
        this.dashBox = basis.GetComponent<DashChargeHitBox>();
        this.enemyTag = (basis.tag == "Player") ? "Enemy" : "Player";
        this.progress = progress;

        //Establish any other variables
        cTimer = 0.0f;
        offCD = true;
    }

    //Cooldown regeneration method
    public void regen() {
        if(!offCD) {
            cTimer += Time.deltaTime;   //Update timer

            //update move cooldown for UI player UI
            if (basis.tag == "Player" || basis.tag == "PlayerAttack")          
                basis.getController().SendMessage("updateCooldownDisplay", this);

            if(cTimer >= MAX_COOLDOWN) {    //If cooldown reached, reset
                cTimer = 0f;
                offCD = true;
            }
        }
    }

    //Accessor method to that checks if the move can be run or not
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

    //Sound effect interval
    private const float CHARGE_SOUND_INTERVAL = 0.25f;

    //Execute Method
    public IEnumerator execute() {
        offCD = false;
        bool alreadyFull = false;
        float channelTimer = 0f;
        float totalTimer = 0f;
        float soundTimer = 0f;

        basis.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/Dash");     //Establish sound clip
        anim.SetBool("Dashing", true);
        anim.SetFloat("speed", 0f);
        basis.getController().canMove = false;

        progress.gameObject.SetActive(true);

        //Channeling
        while(Input.GetMouseButton(1) && !basis.isStunned() && totalTimer < MAX_CHANNEL) {
            channelTimer = (channelTimer < FULL_CHANNEL) ? channelTimer + Time.deltaTime : FULL_CHANNEL;
            progress.updateProgress(channelTimer / FULL_CHANNEL);

            totalTimer += Time.deltaTime;
            soundTimer += Time.deltaTime;

            //Sound effects
            if(channelTimer >= FULL_CHANNEL && !alreadyFull) {
                basis.soundFXs.Stop();
                basis.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/Beep");
                basis.soundFXs.Play();
                alreadyFull = true;
            }else if(soundTimer >= CHARGE_SOUND_INTERVAL && !alreadyFull){
                soundTimer = 0.0f;
                basis.soundFXs.Stop();
                basis.soundFXs.Play();
            }

            basis.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            Battle.updatePlayerAOrientation(basis.transform);
            yield return new WaitForFixedUpdate();
        }

        progress.gameObject.SetActive(false);

        if (!basis.isStunned() && basis.isAlive()) {                    //If the fighter isn't stunned, execute dash
            //Calculate stats
            curPwr = getChannelStat(MIN_PWR, MAX_PWR, channelTimer);
            curDashStrength = getChannelStat(MIN_DASH_STRENGTH, MAX_DASH_STRENGTH, channelTimer);

            basis.soundFXs.Stop();
            basis.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/Dash");
            basis.soundFXs.Play();
            yield return basis.StartCoroutine(dashBox.executeDash(this, curDashStrength, DASH_DURATION, PRIORITY));
        }
    }

    //Assist execute method
    public IEnumerator assistExecute() {
        offCD = false;
        Vector2 dashVector = Battle.dirKnockbackCalc(basis.transform.position, 1);  //Calculate normalized vector

        //Set sequence up
        float channelTimer = 0f;
        float soundTimer = 0f;
        basis.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/Dash");     //Establish sound clip

        anim.SetBool("Dashing", true);
        anim.SetFloat("speed", 0f);
        basis.getController().canMove = false;
        progress.gameObject.SetActive(true);

        //Channeling
        while (!(Input.GetKeyDown("c") || Input.GetKeyDown("v")) && !basis.isStunned() && channelTimer < FULL_CHANNEL) {
            channelTimer += Time.deltaTime;
            progress.updateProgress(channelTimer / FULL_CHANNEL);
            soundTimer += Time.deltaTime;

            if(soundTimer >= CHARGE_SOUND_INTERVAL) {
                soundTimer = 0.0f;
                basis.soundFXs.Stop();
                basis.soundFXs.Play();
            }

            Battle.updatePlayerAOrientation(basis.transform);
            basis.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            yield return new WaitForFixedUpdate();
        }

        progress.gameObject.SetActive(false);

        if (!basis.isStunned()) {                    //If the fighter isn't stunned, execute dash
            //Calculate stats
            curPwr = getChannelStat(MIN_PWR, MAX_PWR, channelTimer);
            curDashStrength = getChannelStat(MIN_DASH_STRENGTH, MAX_DASH_STRENGTH, channelTimer);
            dashVector *= curDashStrength;

            basis.soundFXs.Stop();
            basis.soundFXs.Play();

            yield return basis.StartCoroutine(dashBox.executeDash(this, dashVector, DASH_DURATION, PRIORITY));
        }

        basis.getController().SendMessage("assistExecuted");
    }

    //Enemy execute method
    public IEnumerator enemyExecute(Transform target) {
        offCD = false;

        //Set sequence up
        float channelTimer = 0f;
        float soundTimer = 0f;
        basis.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/Dash");     //Establish sound clip

        anim.SetBool("Dashing", true);
        anim.SetFloat("speed", 0f);
        basis.getController().canMove = false;
        progress.gameObject.SetActive(true);

        //Channeling
        while (!basis.isStunned() && channelTimer < FULL_CHANNEL) {
            channelTimer += Time.deltaTime;
            progress.updateProgress(channelTimer / FULL_CHANNEL);
            soundTimer += Time.deltaTime;

            if (soundTimer >= CHARGE_SOUND_INTERVAL) {
                soundTimer = 0.0f;
                basis.soundFXs.Stop();
                basis.soundFXs.Play();
            }

            Battle.updatePlayerAOrientation(basis.transform);
            basis.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            yield return new WaitForFixedUpdate();
        }

        progress.gameObject.SetActive(false);

        if (!basis.isStunned()) {                    //If the fighter isn't stunned, execute dash
            //Calculate stats
            curPwr = getChannelStat(MIN_PWR, MAX_PWR, channelTimer);
            curDashStrength = getChannelStat(MIN_DASH_STRENGTH, MAX_DASH_STRENGTH, channelTimer);

            basis.soundFXs.Stop();
            basis.soundFXs.Play();

            yield return basis.StartCoroutine(dashBox.executeDashTowards(this, curDashStrength, DASH_DURATION, PRIORITY, target));
        }
    }

    //Private helper method for execute method to calculate current power and current dash strength
    //  Pre: min < max and 0 < channelTime < FULL_CHANNEL
    //  Post: Returns "min + (max - min) * (channelTime / FULL_CHANNEL)"
    private float getChannelStat(float min, float max, float channelTime) {
        float diff = max - min;
        float percentChannel = channelTime / FULL_CHANNEL;
        return min + (diff * percentChannel);
    }

    //Enact effects method
    public void enactEffects(Collider2D enemy) {
        Vector2 knockbackVector = Battle.dirKnockbackCalc(basis.transform.position, enemy.transform.position, KNOCKBACK_VAL);
        enemy.GetComponent<Rigidbody2D>().AddForce(knockbackVector);

        //Calculates damage
        PKMNEntity enemyFighter = enemy.GetComponent<PKMNEntity>();
        int damage = Battle.damageCalc(basis.level, (int)curPwr, basis.accessStat(BaseStat.ATTACK), enemyFighter.accessStat(BaseStat.DEFENSE));

        enemyFighter.StartCoroutine(enemyFighter.receiveDamage(damage, basis));
    }
}
