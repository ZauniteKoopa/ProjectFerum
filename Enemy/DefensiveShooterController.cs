using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefensiveShooterController : Controller {

    private PKMNEntity enemyFighter;        //Reference variable to the fighter
    private Animator anim;                  //Reference variable to animator

    //Movement variables
    private bool ccw;                       //Variable indicating if going in counter clockwise rotation
    private Vector2 distVector;             //Vector from enemy to target
    private const float MIN_DISTANCE = 4.5f; //The optimal min distance
    private const float MAX_DISTANCE = 5.5f; //The optimal max distance for being with 
    private const float MOVEMENT_REDUCTION = 0.85f;

    //Sensor variables that activate enemy Unit
    private Transform target;

    //Moveset variables that will remain constant
    private const int DEF_MOVE = 0;
    private const int MAIN_SHOOTER = 1;

    //Timer variables for main shooter method and melee attack
    private bool canMelee;
    private float meleeTimer;
    private float aTimer;
    private const float MAX_ATTACK_INTERVAL = 12.5f;
    private const float MELEE_ATTACK_INTERVAL = 1.15f;

    //Defensive Move Timer
    private bool canDash;
    private float dashTimer;
    private const float DASH_INTERVAL = 0.45f;

    //Stunned move
    private bool stunMoveExecuted;

    // Start is called before the first frame update
    void Start() {
        canMove = true;
        enemyFighter = GetComponent<PKMNEntity>();
        anim = GetComponent<Animator>();
        aTimer = Random.Range(9f, 11.5f);
        ccw = false;
        canMelee = true;
    }

    // Update is called once per frame
    void FixedUpdate() {
        if(target != null && canMove) {
            enemyMovement();

            //Behavior upon player stun
            PKMNEntity tgtFighter = target.GetComponent<PlayerMovement>().getCurFighter();

            if (!stunMoveExecuted && tgtFighter.isStunned()) {
                stunMoveExecuted = enemyFighter.executeSecMove(MAIN_SHOOTER, target);
                aTimer = (stunMoveExecuted) ? 0f : aTimer;
            }

            //Updating timers foir shooting intervals
            float delta = Time.deltaTime;
            aTimer += delta;

            if(aTimer >= MAX_ATTACK_INTERVAL) {
                enemyFighter.executeSecMove(MAIN_SHOOTER, target);
                stunMoveExecuted = false;
                aTimer = 0f;
            }

            //Updating timers for melee attacks
            if(!canMelee) {
                meleeTimer += delta;

                if(meleeTimer >= MELEE_ATTACK_INTERVAL) {
                    canMelee = true;
                    meleeTimer = 0f;
                }
            }

            //Updating dash timers
            if(!canDash) {
                dashTimer += delta;

                if(dashTimer >= DASH_INTERVAL){
                    dashTimer = 0f;
                    canDash = true;
                }
            }
        }
    }

    //Private helper method for movement
    private void enemyMovement() {
        //Calculate 2 vectors
        Vector2 distVector = new Vector2(transform.position.x - target.position.x, transform.position.y - target.position.y);
        Vector2 circVector = calculateCircVector(distVector);
        float distance = distVector.magnitude;

        //If distance between enemy and player too far, reverse direction. or if equal, have no movement on that axis
        if (distance > MAX_DISTANCE)
            distVector *= -1;
        else if (distance >= MIN_DISTANCE && distance <= MAX_DISTANCE)
            distVector = Vector2.zero;

        //Calculate vector
        distVector.Normalize();
        Vector2 movement = distVector + circVector;
        movement.Normalize();

        //Update animations
        Battle.updateBasicOrientation(movement, transform);
        anim.SetFloat("speed", enemyFighter.getMoveSpeed());
        transform.Translate(movement * enemyFighter.getMoveSpeed() * MOVEMENT_REDUCTION);
    }

    private Vector2 calculateCircVector(Vector2 distVector) {
        //Decide which value to negate by looking at the angle of the vector
        distVector *= -1;

        //Create vector from new information
        float circX = distVector.y;
        float circY = distVector.x;

        circY *= -1;
        Vector2 circVector = new Vector2(circX, circY);
        circVector *= (ccw) ? -1 : 1;

        circVector.Normalize();

        return circVector;
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
            bool moveRan = false;

            if(canDash) {
                canDash = false;
                moveRan = enemyFighter.executeSecMove(DEF_MOVE, threat);
            }

            if (!moveRan && canMelee) {
                enemyFighter.executePrimaryMove(threat);
                canMelee = false;
            }
        }
    }

    //Collision method, everytime hits a platform, reverse direction
    void OnCollisionEnter2D(Collision2D collision) {
        Collider2D collider = collision.collider;

        if (collider.tag == "Platform" || collider.tag == "Player")
            ccw = (ccw) ? false : true;
    }

    //Collision method, if stay on a wall for too long, change direction
    private const float WALL_STAY = 0.5f;
    private float stayTimer;

    void OnCollision2DStay(Collision2D collision) {
        Collider2D wall = collision.collider;

        if(GetComponent<Collider>().tag == "Platform") {
            stayTimer += Time.deltaTime;

            if(stayTimer >= WALL_STAY) {
                ccw = (ccw) ? false : true;
                stayTimer = 0f;
            }
        }
    }
}
