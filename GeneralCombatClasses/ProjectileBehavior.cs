using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileBehavior : AttackBox
{
    //PRECOND: currentMove, speed, and knockbackVal must be filled out before execution

    //Speed and movement variables
    public float speed;
    public Vector2 movement;   //Direction

    //Knockback Variables
    public float knockbackVal;
    public Vector2 knockbackDir;

    //Timeout variables
    private const float MAX_DISTANCE = 15f;
    private float curDist;

    // Start is called before the first frame update. Sets movement
    void Start() {
        string entityTag = transform.parent.tag;
        enemyTag = (entityTag == "Player" || entityTag == "PlayerRecovery") ? "Enemy" : "Player";
        enemyAttackTag = (entityTag == "Player" || entityTag == "PlayerRecovery") ? "EnemyAttack" : "PlayerAttack";
        transform.tag = (entityTag == "Player" || entityTag == "PlayerRecovery") ? "PlayerAttack" : "EnemyAttack";

        transform.parent = null;        //Detach
    }

    // Update is called once per frame
    void FixedUpdate() {
        //Translates projectile
        transform.Translate(movement);

        //Calculates timeout
        curDist += speed;
        if (curDist >= MAX_DISTANCE)
            Object.Destroy(gameObject);
    }

    private const float STUNNED_KNOCKBACK = 50f;

    //Collider method. If collided with an object assigned enemyTag, enactEffects of move upon object
    void OnTriggerEnter2D(Collider2D collider) {

        if (collider.tag == enemyTag) {
            collider.GetComponent<Rigidbody2D>().AddForce(knockbackDir);
            currentMove.enactEffects(collider);

            //Add final knockback if someone is stunned
            if (collider.GetComponent<PKMNEntity>().isStunned()) {
                Vector2 temp = new Vector2(movement.x, movement.y);
                temp.Normalize();
                temp *= STUNNED_KNOCKBACK;
                collider.GetComponent<Rigidbody2D>().AddForce(temp);
            }

            Object.Destroy(gameObject);
        } else if ((collider.tag == "PlayerAttack" || collider.tag == "EnemyAttack") && collider.GetComponent<AttackBox>().priority >= priority) {  //Checking priority
            StartCoroutine(destroyProj());
        }else if(collider.GetComponent<DashChargeHitBox>() != null && collider.tag == enemyAttackTag){    //If the attack box was just a dashing unit, destroy projectile
            StartCoroutine(destroyProj());
        }else if(collider.tag == "Platform"){
            Object.Destroy(gameObject);
        }
    }

    //Offensive setup method
    public void offensiveSetup(float speed, float kb, int priority, IMove move) {
        this.priority = priority;
        this.speed = speed;
        this.knockbackVal = kb;
        currentMove = move;
    }

    //Method to call when destroying projectile
    IEnumerator destroyProj() {
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;
        yield return new WaitForSeconds(0.1f);
        Object.Destroy(gameObject);
    }

    //Method that sets the direction from entity to mouse
    public void setMouseDirection(Transform source) {
        Vector2 fatherPos = source.position;
        Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);

        Vector2 dirVector = new Vector2(mousePos.x - fatherPos.x, mousePos.y - fatherPos.y);
        dirVector.Normalize();

        movement = dirVector * speed;
        knockbackDir = dirVector * knockbackVal;
    }

    //Method that sets direction from entity to its enemy / target
    public void setDirectionToTarget(Transform source, Transform target) {
        Vector2 sourcePos = source.position;
        Vector2 targetPos = target.position;

        //Create direction vector
        Vector2 dirVector = new Vector2(targetPos.x - sourcePos.x, targetPos.y - sourcePos.y);
        dirVector.Normalize();

        movement = dirVector * speed;
        knockbackDir = dirVector * knockbackVal;
    }

    //Manually sets direction to a specified vector
    public void setDirection(Vector2 dir) {
        dir.Normalize();
        movement = dir * speed;
        knockbackDir = knockbackVal * dir;
    }

    //Accessor method to normalized vector
    public Vector2 getNormDir() {
        Vector2 result = new Vector2(movement.x, movement.y);
        result.Normalize();
        return result;
    }
}