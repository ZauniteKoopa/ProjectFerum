using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerticalStaticDualGuard : Controller
{

    //Reference variables
    private Animator anim;
    private PKMNEntity fighter;

    //Movement variables
    public bool goesUp;
    public float MAX_DISTANCE;
    private float curDistance;
    private const float MOVEMENT_REDUCTION = 0.85f;

    // Main Ability Variables (Main attacks must be next to each other)
    private int MAIN_ATTACK_1 = 1;
    private int MAIN_ATTACK_2 = 2;
    private int NUM_MAIN_ATTACKS = 2;
    private int curMainAttack;
    private const float ATTACK_INTERVAL = 9f;
    private float aTimer;

    //Defensive ability variables
    private int SEC_ABILITY = 0;
    private const float MELEE_ATTACK_INTERVAL = 1.2f;
    private bool canMelee;
    private float mTimer;

    //Targeting variable
    private Transform target;

    // Start is called before the first frame update
    void Start() {
        canMove = true;
        anim = GetComponent<Animator>();
        fighter = GetComponent<PKMNEntity>();
        canMelee = true;
        curMainAttack = Random.Range(MAIN_ATTACK_1, MAIN_ATTACK_2 + 1);
        aTimer = Random.Range(4f, 8f);
    }

    // Update is called once per frame
    void FixedUpdate() {
        if(canMove) {
            movement();
            float delta = Time.deltaTime;

            //Method that allows attacking
            if(target != null) {
                aTimer += delta;

                if(aTimer >= ATTACK_INTERVAL) {
                    fighter.executeSecMove(curMainAttack, target);
                    curMainAttack++;
                    curMainAttack -= (curMainAttack > MAIN_ATTACK_2) ? NUM_MAIN_ATTACKS : 0;
                    aTimer = 0f;
                }
            }

            //Method that manages melee attack timer
            if(!canMelee) {
                mTimer += delta;

                if(mTimer >= MELEE_ATTACK_INTERVAL) {
                    canMelee = true;
                    mTimer = 0f;
                }
            }
        }
    }

    //Method that allows the enemy to move
    private void movement() {
        Vector2 move = (goesUp) ? Vector2.up : Vector2.down;
        move *= fighter.getMoveSpeed() * MOVEMENT_REDUCTION;
        Battle.updateBasicOrientation(move, transform);
        anim.SetFloat("speed", fighter.getMoveSpeed());
        transform.Translate(move);

        curDistance += fighter.getMoveSpeed() * MOVEMENT_REDUCTION;
        if(curDistance >= MAX_DISTANCE) {
            goesUp = !goesUp;
            curDistance = 0f;
        }
    }

    //Upon collision with a platform or player, change course
    private void OnCollisionEnter2D(Collision2D collision) {
        Collider2D collider = collision.collider;

        if(collider.tag == "Player" || collider.tag == "Platform") {
            goesUp = !goesUp;
            curDistance = MAX_DISTANCE - curDistance;
        }
    }

    //Sensor method: activates enemy upon use with a target in mind
    //  Pre: an entity (the player) has entered the attacking zone
    public void senseActivate(Transform target) {
        if (target.GetComponent<Controller>() != null)
            this.target = target;
        else
            this.target = target.parent;
    }

    //Sensor method: activates a defensive execute upon player entering defensive sensor
    //  Pre: either activates the fighter's primary move or secondary move upon reaction
    public void senseReact(Transform threat) {

        if(canMove) {
            bool moveRan = fighter.executeSecMove(SEC_ABILITY, threat);

            if(!moveRan && canMelee) {
                fighter.executePrimaryMove(threat);
                canMelee = false;
            }
        }
    }
}
