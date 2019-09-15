using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AggroSquirtleController : Controller {

    //Reference variables
    private PKMNEntity enemyFighter;
    private Animator anim;

    private Transform target;   //Target that activates enemy

    //Moveset Variables
    private const int BUBBLE_SHIELD = 0;
    private const int MAIN_TACKLE = 1;

    //Timer variables
    private bool canMelee;
    private float meleeTimer;
    private float aTimer;
    private const float MAX_ATTACK_INTERVAL = 14.5f;
    private const float MELEE_ATTACK_INTERVAL = 1.75f;

    //Stunned variables
    private bool stunMoveExecuted;

    // Start is called before the first frame update
    void Start() {
        canMove = true;
        canMelee = true;

        enemyFighter = GetComponent<PKMNEntity>();
        anim = GetComponent<Animator>();
        aTimer = Random.Range(10.5f, 13f);
    }

    // Update is called once per frame
    void FixedUpdate() {
        if(canMove && target != null) {
            //Enemy movement
            Vector2 movement = new Vector2(target.position.x - transform.position.x, target.position.y - transform.position.y);
            movement.Normalize();
            Battle.updateBasicOrientation(movement, transform);
            anim.SetFloat("speed", enemyFighter.getMoveSpeed());
            transform.Translate(movement * enemyFighter.getMoveSpeed() * 0.5f);

            //Behavior upon stun: If enemy stunned, charge dash. If dash couldn't be run, use a water pulse
            PKMNEntity tgtFighter = target.GetComponent<PlayerMovement>().getCurFighter();

            if (!stunMoveExecuted && tgtFighter.isStunned()) {
                stunMoveExecuted = enemyFighter.executeSecMove(MAIN_TACKLE, target);

                if (stunMoveExecuted)
                    aTimer = 0f;
                else
                    stunMoveExecuted = enemyFighter.executeSecMove(BUBBLE_SHIELD, target);
            }

            //Timers
            float delta = Time.deltaTime;
            aTimer += delta;

            //Resetter for attack timer
            if(aTimer >= MAX_ATTACK_INTERVAL) {
                enemyFighter.executeSecMove(MAIN_TACKLE, target);
                stunMoveExecuted = false;
                aTimer = 0f;
            }

            //Resetter for melee timer
            if(!canMelee) {
                meleeTimer += delta;

                if(meleeTimer >= MELEE_ATTACK_INTERVAL) {
                    canMelee = true;
                    meleeTimer = 0.0f;
                }
            }
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
        if(threat.tag == "Player" && canMove && canMelee) {         //If a player is nearby, melee attack
            enemyFighter.executePrimaryMove(threat);
            canMelee = false;
        }else{                                          //If it's a player attack. Do a defensive water pulse. If it doesn't run, primary attack
            bool moveRan = enemyFighter.executeSecMove(BUBBLE_SHIELD, threat);

            if(!moveRan && canMelee) {
                enemyFighter.executePrimaryMove(threat);
                canMelee = false;
            }
        }
    }
}
