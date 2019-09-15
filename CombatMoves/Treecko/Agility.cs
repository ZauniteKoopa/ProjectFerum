using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Agility : ISecMove
{
    //Reference Variables to PKMNEntity Attached
    private Animator anim;
    private PKMNEntity unit;
    private ProgressBar progress;

    //Constant variables for the StatEffect object
    private const float CHANGE_FACTOR = 1.5f;
    private const float DURATION = 14.0f;

    //Charge Management Variables (In seconds)
    private float chargeProgress;
    private bool charging;
    private float dTimer;
    private const float CHARGE_REQ = 2.5f;
    private const float DECAY_RATE = 0.09f;
    private const float DECAY_INTERVAL = 0.5f;
   
    //Cooldown Management Variables
    private const float MAX_COOLDOWN = 20f;
    private float cTimer;
    private bool offCD;

    //Sound effect interval
    private const float CHARGE_SOUND_INTERVAL = 0.85f;

    //General Constructor
    public Agility(Animator anim, PKMNEntity unit, ProgressBar progress) {
        this.anim = anim;
        this.unit = unit;
        this.progress = progress;

        //Charge Management variables
        chargeProgress = 0.0f;
        charging = false;
        dTimer = 0.0f;

        //Cooldown Management Variables
        offCD = true;
        cTimer = 0.0f;
    }

    //Execute method upon user input FOR PLAYER for charging a self-buff
    public IEnumerator execute() {
        Controller entityController = anim.GetComponent<PKMNEntity>().getController();

        charging = true;                                    //Set boolean locking variable to true to avoid decay while charging
        anim.SetFloat("speed", 0);                          //Set Animation
        anim.SetBool("Charging", true);                     
        entityController.canMove = false;                   //Disable movement
        float curHealth = unit.accessStat(BaseStat.HEALTH); //Get current health for checking
        progress.gameObject.SetActive(true);                //Set progressbar to true

        unit.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/Charging");  //Establish sound clip
        float soundTimer = CHARGE_SOUND_INTERVAL;           //Set sound to play immediately

        //While player holds key, update chargeProgress
        while (Input.GetMouseButton(1) && chargeProgress < CHARGE_REQ && unit.accessStat(BaseStat.HEALTH) >= curHealth) {
            chargeProgress += Time.deltaTime;
            soundTimer += Time.deltaTime;

            curHealth = unit.accessStat(BaseStat.HEALTH);
            progress.updateProgress(chargeProgress / CHARGE_REQ);

            if(soundTimer >= CHARGE_SOUND_INTERVAL) {
                soundTimer = 0.0f;
                unit.soundFXs.Stop();
                unit.soundFXs.Play();
            }

            yield return new WaitForFixedUpdate();
        }

        anim.SetBool("Charging", false);

        //If charge is successful, update animator and apply StatEffect to self
        if (chargeProgress >= CHARGE_REQ) {
            progress.gameObject.SetActive(false);
            chargeProgress = 0.0f;
            offCD = false;
            enactEffects(unit.GetComponent<Collider2D>());

            //Sound effects
            unit.soundFXs.Stop();
            unit.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/Beep");
            unit.soundFXs.Play();

            anim.SetBool("FinishedCharge", true);
            float animLength = anim.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(animLength);
            anim.SetBool("FinishedCharge", false);
        }

        charging = false;

        if(!unit.isStunned() && unit.isAlive())
            entityController.canMove = true;
    }

    //Assist execute method for player
    public IEnumerator assistExecute() {

        charging = true;                                    //Set boolean locking variable to true to avoid decay while charging
        anim.SetFloat("speed", 0);                          //Set Animation
        anim.SetBool("Charging", true);
        float curHealth = unit.accessStat(BaseStat.HEALTH); //Get current health for checking
        progress.gameObject.SetActive(true);                //Set progressbar to true

        //While player holds key, update chargeProgress
        while (chargeProgress < CHARGE_REQ && unit.accessStat(BaseStat.HEALTH) >= curHealth){
            chargeProgress += Time.deltaTime;
            progress.updateProgress(chargeProgress / CHARGE_REQ);
            curHealth = unit.accessStat(BaseStat.HEALTH);
            yield return new WaitForFixedUpdate();
        }

        anim.SetBool("Charging", false);

        //If charge is successful, update animator and apply StatEffect to self
        if (chargeProgress >= CHARGE_REQ) {
            progress.gameObject.SetActive(false);
            chargeProgress = 0.0f;
            offCD = false;
            enactEffects(unit.GetComponent<Collider2D>());

            //Finish sound effects
            unit.soundFXs.Stop();
            unit.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/Beep");
            unit.soundFXs.Play();

            anim.SetBool("FinishedCharge", true);
            float animLength = anim.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(animLength);
            anim.SetBool("FinishedCharge", false);
        }

        charging = false;
        unit.getController().SendMessage("assistExecuted");
    }

    //Execute method for enemy
    public IEnumerator enemyExecute(Transform target) {

        charging = true;                                    //Set boolean locking variable to true to avoid decay while charging
        anim.SetFloat("speed", 0);                          //Set Animation
        anim.SetBool("Charging", true);
        float curHealth = unit.accessStat(BaseStat.HEALTH); //Get current health for checking
        progress.gameObject.SetActive(true);                //Set progressbar to true

        //While player holds key, update chargeProgress
        while (chargeProgress < CHARGE_REQ && unit.accessStat(BaseStat.HEALTH) >= curHealth) {
            chargeProgress += Time.deltaTime;
            progress.updateProgress(chargeProgress / CHARGE_REQ);
            curHealth = unit.accessStat(BaseStat.HEALTH);
            yield return new WaitForFixedUpdate();
        }

        anim.SetBool("Charging", false);

        //If charge is successful, update animator and apply StatEffect to self
        if (chargeProgress >= CHARGE_REQ) {
            progress.gameObject.SetActive(false);
            chargeProgress = 0.0f;
            offCD = false;
            enactEffects(unit.GetComponent<Collider2D>());

            //Finish sound effects
            unit.soundFXs.Stop();
            unit.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/Beep");
            unit.soundFXs.Play();

            anim.SetBool("FinishedCharge", true);
            float animLength = anim.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(animLength);
            anim.SetBool("FinishedCharge", false);
        }

        charging = false;
    }

    //EnactEffects method. Apply self-buff on unit and add buff to the PriorityQueue
    public void enactEffects(Collider2D self) {
        StatEffect effects = new StatEffect(DURATION, CHANGE_FACTOR, BaseStat.SPEED);
        effects.applyEffect(unit);
    }

    //Regeneration method in charge of cooldown mangement and decay
    public void regen() {
        float delta = Time.deltaTime;

        //Decay Management
        if(chargeProgress > 0 && !charging) {
            dTimer += delta;

            //If 1 second passed, decay chargeProgress. 0 is the minimum
            if(dTimer >= DECAY_INTERVAL) {
                chargeProgress -= DECAY_RATE;
                chargeProgress = (chargeProgress < 0) ? 0 : chargeProgress;
                progress.updateProgress(chargeProgress / CHARGE_REQ);

                if(chargeProgress == 0)
                    progress.gameObject.SetActive(false);

                dTimer = 0.0f;
            }
        }

        //Cooldown Management
        if(!offCD) {
            cTimer += delta;

            //update move cooldown for UI player UI
            if (unit.tag == "Player" || unit.tag == "PlayerAttack")          
                unit.getController().SendMessage("updateCooldownDisplay", this);

            //If cooldown reached its finish, reset timer and make this move off cooldown
            if (cTimer >= MAX_COOLDOWN) {
                offCD = true;
                cTimer = 0.0f;
            }
        }
    }

    //Method to check if move is usable by returning whether it's off cooldown
    public bool canRun() {
        return offCD;
    }

    //Accessor method to basis / fighter
    public PKMNEntity getBasis() {
        return unit;
    }

    //Accessor method to CD progress
    public float getCDProgress() {
        return (!offCD) ? (MAX_COOLDOWN - cTimer) / MAX_COOLDOWN : 0;
    }
}
