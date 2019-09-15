using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletSeed : ISecMove
{
    //Innate attack variables associated with BulletSeed
    private const int PWR = 17;
    private const float KNOCKBACK_VAL = 65f;
    private const float PROJ_SPEED = 0.165f;
    private const int PROJ_PRIORITY = 1;

    //Reference variables based on source
    private Animator anim;
    private PKMNEntity basis;
    private Transform hitbox;
    private string enemyTag;

    //Regeneration variables
    private const float REGEN_RATE = 2f; //X seconds per bullet
    private const int MAX_AMMO = 12;
    private int curAmmo;
    private float rTimer;

    //Cooldown Timer
    private const float MAX_COOLDOWN = 10f;
    private float curCooldown;
    private float cTimer;
    private bool offCD;

    //BulletSeed constructor
    //  Pre: anim MUST have hDirection, vDirection, and special attacking vars. sourceTag is either Player or Enemy
    public BulletSeed(Animator anim, PKMNEntity basis) {
        //Establish variables concerning source
        this.anim = anim;
        this.basis = basis;
        this.enemyTag = (anim.tag == "Player") ? "Enemy" : "Player";

        //Set variables for hitbox
        this.hitbox = Resources.Load<Transform>("MoveHitboxes/BulletSeedObj");
        ProjectileBehavior projProperties = hitbox.GetComponent<ProjectileBehavior>();
        projProperties.speed = PROJ_SPEED;
        projProperties.knockbackVal = 0;
        projProperties.priority = PROJ_PRIORITY;

        //Regen variables
        curAmmo = MAX_AMMO;
        rTimer = 0.0f;

        //Set cooldown variables
        cTimer = 0.0f;
        curCooldown = MAX_COOLDOWN;
        offCD = true;
    }

    //Regen algorithm: Regenerates ammo every REGEN_RATE seconds unless curAmmo == MAX_AMMO
    //  Called upon update in PKMNEntity
    public void regen() {
        float delta = Time.deltaTime;

        //Ammo regeneration
        if(curAmmo != MAX_AMMO) {
            //Update timer
            rTimer += delta;

            //If rTimer reaches REGEN_RATE, update curAmmo and reset timer
            if(rTimer >= REGEN_RATE){
                curAmmo = (curAmmo < MAX_AMMO) ? curAmmo + 1 : curAmmo;
                rTimer = 0.0f;
            }
        }

        //Cooldown
        if(!offCD) {
            cTimer += delta;
            
            //update move cooldown for UI player UI
            if (basis.tag == "Player" || basis.tag == "PlayerAttack")          
                basis.getController().SendMessage("updateCooldownDisplay", this);

            //If cooldown is over, set offCD to true and reset timer
            if (cTimer >= curCooldown) {
                offCD = true;
                cTimer = 0.0f;
            }
        }
    }

    //Accessor method concerning the status of the move: whether it can be run or not
    //  Post: Returns true if curAmmo > 0
    public bool canRun() {
        return curAmmo > 0 && offCD && !basis.isStunned();
    }

    //Accessor method to basis / fighter
    public PKMNEntity getBasis() {
        return basis;
    }

    //Accessor method to cooldown progress
    public float getCDProgress() {
        return (!offCD) ? (curCooldown - cTimer) / curCooldown : 0;
    }

    //Constants for the Move Executor
    private const float FIRE_RATE = 0.3f;
    private const int MAX_SHOTS = 5;

    //Move executor for PLAYER
    //  Pre: Player has pressed input for this move's execution 
    //  Post: Move is executed and transform hitbox instantiated with source as parent
    public IEnumerator execute() {
        //Set up move
        offCD = false;
        basis.moveAttack();
        anim.SetBool("SpAttacking", true);
        int shotsFired = 0;

        //Bullet system. Player can fire a barrage of 5 bullets by holding button, but must stay still during it (CHANGE THIS KEYCODE)
        while(Input.GetMouseButton(1) && shotsFired < MAX_SHOTS && !basis.isStunned() && curAmmo > 0) {
            curAmmo--;              //Decrement ammo by 1
            shotsFired++;           //Increment shots fired by 1

            Battle.updatePlayerAOrientation(basis.transform);
            hitbox.GetComponent<ProjectileBehavior>().setMouseDirection(basis.transform);

            Transform curBullet = Object.Instantiate(hitbox, basis.transform);     //Instantiates hitbox with basis as the parent
            curBullet.GetComponent<ProjectileBehavior>().currentMove = this;

            //Bullet sound effect
            basis.soundFXs.Stop();
            basis.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/MachineGun");
            basis.soundFXs.Play();

            yield return new WaitForSeconds(FIRE_RATE);
        }

        curCooldown = shotsFired * (MAX_COOLDOWN / MAX_SHOTS);

        //Upon release, enable cooldown timers and movement
        anim.SetBool("SpAttacking", false);
        basis.moveAttackLeave();
        yield return 0;
    }

    //AssistExecute for Player
    public IEnumerator assistExecute() {
        //Set up move
        Battle.updatePlayerAOrientation(basis.transform);
        ProjectileBehavior hitBehavior = hitbox.GetComponent<ProjectileBehavior>();
        hitBehavior.setMouseDirection(basis.transform);

        offCD = false;
        anim.SetBool("SpAttacking", true);
        int shotsFired = 0;
        float curHealth = basis.accessStat(BaseStat.HEALTH);

        //Bullet system. Player can fire a barrage of 5 bullets by holding button, but must stay still during it (CHANGE THIS KEYCODE)
        while (curHealth <= basis.accessStat(BaseStat.HEALTH) && shotsFired < MAX_SHOTS && curAmmo > 0) {
            curAmmo--;              //Decrement ammo by 1
            shotsFired++;           //Increment shots fired by 1

            Transform curBullet = Object.Instantiate(hitbox, basis.transform);     //Instantiates hitbox with basis as the parent
            curBullet.GetComponent<ProjectileBehavior>().currentMove = this;

            //Bullet sound effect
            basis.soundFXs.Stop();
            basis.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/MachineGun");
            basis.soundFXs.Play();

            yield return new WaitForSeconds(FIRE_RATE);
        }

        curCooldown = shotsFired * (MAX_COOLDOWN / MAX_SHOTS); //Reduces cooldown depending on the amount of shots fired

        //Upon release, enable cooldown timers and movement
        anim.SetBool("SpAttacking", false);
        basis.getController().SendMessage("assistExecuted");
        yield return 0;
    }

    //Move executor for enemy CPUs
    //  Pre: 0 <= numShots <= MAX_SHOTS
    //  Post: Enemy has executed bullet seed
    public IEnumerator enemyExecute(Transform target) {
        //Set up move
        offCD = false;
        basis.moveAttack();
        anim.SetBool("SpAttacking", true);
        int shotsFired = 0;

        //Bullet system. Player can fire a barrage of 6 bullets by holding button, but must stay still during it (CHANGE THIS KEYCODE)
        while (shotsFired < MAX_SHOTS && !basis.isStunned() && curAmmo > 0) {
            curAmmo--;              //Decrement ammo by 1
            shotsFired++;           //Increment shots fired by 1

            Battle.updateEnemyAOrientation(basis.transform, target, true);
            hitbox.GetComponent<ProjectileBehavior>().setDirectionToTarget(basis.transform, target.transform);

            Transform curBullet = Object.Instantiate(hitbox, basis.transform);     //Instantiates hitbox with basis as the parent
            curBullet.GetComponent<ProjectileBehavior>().currentMove = this;

            //Bullet sound effect
            basis.soundFXs.Stop();
            basis.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/MachineGun");
            basis.soundFXs.Play();

            yield return new WaitForSeconds(FIRE_RATE);
        }

        curCooldown = shotsFired * (MAX_COOLDOWN / MAX_SHOTS); //Reduces cooldown depending on the amount of shots fired

        //Upon release, enable cooldown timers and movement
        anim.SetBool("SpAttacking", false);
        anim.GetComponent<PKMNEntity>().moveAttackLeave();
        yield return 0;
    }

    //enactEffects method. If hitbox hit, enact the effects of this move upon enemy
    //  Pre: collider is an enemy to the source of this move
    //  Post: enemy is knockbacked in the direction of the bullet
    public void enactEffects(Collider2D enemy) {
        //Calculates damage
        //Calculates damage
        PKMNEntity enemyFighter = enemy.GetComponent<PKMNEntity>();
        int damage = Battle.damageCalc(basis.level, PWR, basis.accessStat(BaseStat.SPECIAL_ATTACK), enemyFighter.accessStat(BaseStat.SPECIAL_DEFENSE));

        enemyFighter.StartCoroutine(enemyFighter.receiveDamage(damage, basis));
    }
}
