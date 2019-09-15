using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pound : IMove
{
    //Innate attack variables associated with Pound
    private const int PWR = 30;
    private const float KNOCKBACK_VAL = 175f;
    private const int MOVE_PRIORITY = 3;
    private const int TYPE = BaseStat.NORMAL;

    //Variables that would be associated with the source
    private Animator animator;
    private PKMNEntity basis;
    private Transform hitbox;
    private string enemyTag;

    //Cooldown variables
    private const float MAX_COOLDOWN = 0.625f;
    private float cTimer;
    private bool offCD;

    //Hashset variable that looks at what's hit
    private HashSet<Collider2D> hit;

    //Constructor of move. ONLY TO BE USED AT START OF GAME ON AWAKE OR UPON LEARNING MOVE. melee hitbox should be first child of PKMNEntity gameObject
    public Pound(Animator animator, PKMNEntity baseStats)  {
        this.animator = animator;
        this.basis = baseStats;
        this.hitbox = basis.transform.GetChild(0);
        this.enemyTag = (animator.tag == "Player") ? "Enemy" : "Player";

        offCD = true;
        cTimer = 0.0f;
        hit = new HashSet<Collider2D>();
    }

    //Constant variables
    private const float HITBOX_LENGTH = 0.85f;

    //Executes action for Pound (Player)
    //  Creates a small box hitbox in front of user
    public IEnumerator execute() {
        Controller entityController = animator.GetComponent<PKMNEntity>().getController();

        entityController.canMove = false;
        offCD = false;
        hitbox.GetComponent<Hitbox>().setMove(this, MOVE_PRIORITY);

        //Add melee sound effects
        basis.soundFXs.Stop();
        basis.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/MeleeAttack");
        basis.soundFXs.Play();

        //Set position of hitbox centered at player
        Battle.updatePlayerAOrientation(animator.GetComponent<Transform>());
        yield return basis.StartCoroutine(updateHitBoxPos());

        if (!basis.isStunned())
            entityController.canMove = true;
        hit.Clear();
    }

    //Executes action for pound (enemy)
    // Create a small hitbox
    public IEnumerator enemyExecute(Transform target) {
        Controller entityController = animator.GetComponent<PKMNEntity>().getController();

        entityController.canMove = false;
        offCD = false;
        hitbox.GetComponent<Hitbox>().setMove(this, MOVE_PRIORITY);

        //Add melee sound effects
        basis.soundFXs.Stop();
        basis.soundFXs.clip = Resources.Load<AudioClip>("Audio/AttackSounds/MeleeAttack");
        basis.soundFXs.Play();

        //Set position of hitbox centered at player
        Battle.updateEnemyAOrientation(animator.transform, target, true);
        yield return basis.StartCoroutine(updateHitBoxPos());

        if (!basis.isStunned() && basis.isAlive())
            entityController.canMove = true;
    }

    //Private helper method for updating hitbox properties
    private IEnumerator updateHitBoxPos() {
        float hitX = animator.transform.position.x;
        float hitY = animator.transform.position.y;

        //Change offset based on player orientation
        if (animator.GetInteger("aHorizontalDirection") != 0 && animator.GetInteger("aVerticalDirection") != 0) {
            hitX += (Mathf.Sqrt(2f) / 2) * HITBOX_LENGTH * animator.GetInteger("aHorizontalDirection");
            hitY += (Mathf.Sqrt(2f) / 2) * HITBOX_LENGTH * animator.GetInteger("aVerticalDirection");
        }else{
            hitX += HITBOX_LENGTH * animator.GetInteger("aHorizontalDirection");
            hitY += HITBOX_LENGTH * animator.GetInteger("aVerticalDirection");
        }

        hitbox.position = new Vector3(hitX, hitY, 0);

        //Execute animation
        animator.SetFloat("speed", 0);
        animator.SetBool("PhyAttacking", true);

        float animLength = animator.GetCurrentAnimatorStateInfo(0).length;

        //wait for animation frame to finish before continuing script
        yield return new WaitForSeconds(animLength);
        animator.SetBool("PhyAttacking", false);
    }

    //Sends damage if enemy is within hitbox of transform
    //  Does damage to an area in front
    public void enactEffects(Collider2D enemy) {
        if(!hit.Contains(enemy)) {
            hit.Add(enemy); //Add enemy to hashset to avoid double dipping

            //Calculates knockback
            Vector2 knockback = Battle.sourceKnockbackCalc(animator, KNOCKBACK_VAL);

            //Calculates damage
            PKMNEntity enemyFighter = enemy.GetComponent<PKMNEntity>();
            int damage = Battle.damageCalc(basis.level, PWR, basis.accessStat(BaseStat.ATTACK), enemyFighter.accessStat(BaseStat.DEFENSE));

            enemy.GetComponent<Rigidbody2D>().AddForce(knockback);      //Applies knockback
            enemyFighter.StartCoroutine(enemyFighter.receiveDamage(damage, basis));
        }
    }

    //Regen method. Nothing is put in the algorithm since its a basic melee attack
    public void regen() {
        if(!offCD) {
            cTimer += Time.deltaTime;

            if (cTimer >= MAX_COOLDOWN) {
                offCD = true;
                cTimer = 0;
            }

        }
    }

    //Because this move is a basic attack, it can always be run unless controller doesn't allow it to be so.
    public bool canRun() {
        return offCD && !basis.isStunned();
    }

    //Accessor method to basis / fighter
    public PKMNEntity getBasis(){
        return basis;
    }
}
