using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BubbleBehavior : AttackBox {
    //Movement variables
    private const float SPEED = 0.03f;
    private const float OFFSET = 0.1f;
    public Vector2 movement;
    private bool hitWall;

    //Knockback Variables
    private float kTimer;
    public float knockbackVal;

    //Timeout variables
    private const float MAX_TIME = 9f;
    private float curTime;

    //Health Variables
    private const int MAX_HEALTH = 8;
    public int health;

    //Explosion
    public ExplosionBehavior explosion;

    //Health bar
    public Image healthBar;

    //Basis set up
    void Start() {

        string entityTag = transform.parent.tag;
        enemyTag = (entityTag == "Player" || entityTag == "PlayerRecovery") ? "Enemy" : "Player";
        enemyAttackTag = (entityTag == "Player" || entityTag == "PlayerRecovery") ? "EnemyAttack" : "PlayerAttack";
        transform.tag = (entityTag == "Player" || entityTag == "PlayerRecovery") ? "PlayerAttack" : "EnemyAttack";
        healthBar.color = (entityTag == "Player" || entityTag == "PlayerRecovery") ? Color.green : Color.red;

        Vector3 posOffset = movement;
        posOffset.Normalize();
        transform.localPosition = posOffset * OFFSET;

        health = MAX_HEALTH;
        explosion.setUp(knockbackVal);

        transform.parent = null;        //Detach
    }

    //Sets up projectile before firing / cloning (Player)
    public void setUp(Vector2 entityPos) {
        movement = Battle.dirKnockbackCalc(entityPos, SPEED);
    }

    //Set up projectile before firing / cloning (enemy)
    public void setUp(Vector2 entityPos, Vector2 enemyPos) {
        movement = Battle.dirKnockbackCalc(entityPos, enemyPos, SPEED);
    }

    //Allows movement each frame
    void FixedUpdate(){
        //Translates projectile if not recoiled
        if(!hitWall)
            transform.Translate(movement);

        //Calculates timeout
        curTime += Time.deltaTime;
        if (curTime > MAX_TIME)
            Object.Destroy(gameObject);
    }

    private const float STUNNED_KNOCKBACK = 30f;
    private const float BOUNCE_KNOCKBACK = 200f;
    private const float BOUNCE_DURATION = 0.25f;

    //Collider method. If collided with an object assigned enemyTag, enactEffects of move upon object
    void OnTriggerEnter2D(Collider2D collider) {
        bool dashing = isDashing(collider);

        if (collider.tag == "Player" || collider.tag == "Enemy" || dashing) {

            if (collider.tag == enemyTag || (collider.tag == enemyAttackTag && collider.GetComponent<DashChargeHitBox>() != null)){
                Vector2 knockbackDir = Battle.dirKnockbackCalc(transform.position, collider.transform.position, knockbackVal);
                collider.GetComponent<Rigidbody2D>().AddForce(knockbackDir);
                currentMove.enactEffects(collider);

                //Add final knockback if someone is stunned
                if (collider.GetComponent<PKMNEntity>().isStunned()){
                    Vector2 temp = new Vector2(movement.x, movement.y);
                    temp.Normalize();
                    temp *= STUNNED_KNOCKBACK;
                    collider.GetComponent<Rigidbody2D>().AddForce(temp);
                }

                StartCoroutine(destroyProj());
            }else{
                StartCoroutine(playerBounce(collider));
                if (dashing) {
                    health -= 2;
                    healthBar.fillAmount = (float)health / (float)MAX_HEALTH;
                }
            }

        }else if (collider.tag == "EnemyAttack" || collider.tag == "PlayerAttack"){     //Checking priority
            int otherPrior = collider.GetComponent<AttackBox>().priority;

            if(collider.GetComponent<BubbleBehavior>() != null)
                movement *= -1;
            else if(otherPrior < priority && collider.tag == enemyAttackTag)
                health--;
            else if(otherPrior > priority)
                health = 0;
            else
                health -= 2;

            healthBar.fillAmount = (float)health / (float)MAX_HEALTH;

            if (health <= 0)                    //Destroy projectile and allow explosion
                StartCoroutine(destroyProj());

        }
    }

    //Collider ON Stay Method: exerts force every 0.2 seconds if the collider is a player or an enemy
    private const float KNOCKBACK_RATE = 0.15f;

    void OnTriggerStay2D(Collider2D collider) {
        if (collider.tag == "Player" || collider.tag == "Enemy") {
            kTimer += Time.deltaTime;

            if (kTimer >= KNOCKBACK_RATE) {
                StartCoroutine(playerBounce(collider));
                kTimer = 0;
            }
        }

        //Wall detection
        if(!hitWall && collider.tag == "Platform")
            hitWall = true;
    }

    private bool isDashing(Collider2D collider) {
        return (collider.tag == "PlayerAttack" || collider.tag == "EnemyAttack") && collider.GetComponent<DashChargeHitBox>() != null;
    }

    //IEnumerator for bubble bouncing
    IEnumerator playerBounce(Collider2D collider) {
        Vector2 knockbackDir = Battle.dirKnockbackCalc(transform.position, collider.transform.position, BOUNCE_KNOCKBACK);
        Rigidbody2D rb = collider.GetComponent<Rigidbody2D>();
        Controller control = collider.GetComponent<PKMNEntity>().getController();

        //control.canMove = false;
        rb.velocity = Vector2.zero;
        rb.AddForce(knockbackDir);
        yield return new WaitForSeconds(BOUNCE_DURATION);
        rb.velocity = Vector3.zero;
    }

    //Method to call when destroying projectile. Initiates explosion
    IEnumerator destroyProj() {
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;
        explosion.activate();
        yield return new WaitForSeconds(0.25f);
        Object.Destroy(gameObject);
    }

    //IEnumerator to allow knockback
    private const float KNOCKBACK_DURATION = 0.3f;

    IEnumerator allowKnockback(Collider2D collider) {
        yield return new WaitForSeconds(KNOCKBACK_DURATION);
        Rigidbody2D rb = collider.GetComponent<Rigidbody2D>();
        rb.velocity = Vector2.zero;
    }
}
