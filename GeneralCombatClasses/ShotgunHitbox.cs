using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShotgunHitbox : AttackBox {

    private const float DURATION = 0.15f;   //Duration of attack
    private const float OFFSET = 0.3f;      //Positional offset of hitbox
    private float timer;                    //Timer to keep track of when to delete hitbox
    private bool dead;

    private float knockbackVal;
    public Vector2 knockback;

    public HashSet<Collider2D> hit;        //A hashset that contains all entities hit

    //Initial setup. Made when character starts loading up moves
    private void initialSetup(int priority, IMove curMove, float knockback) {
        PKMNEntity basis = curMove.getBasis();

        this.priority = priority;
        this.currentMove = curMove;
        string entityTag = basis.tag;
        bool isPlayer = entityTag == "Player" || entityTag == "PlayerRecovery";

        this.enemyTag = (isPlayer) ? "Enemy" : "Player";
        this.enemyAttackTag = (isPlayer) ? "EnemyAttack" : "PlayerAttack";
        tag = (isPlayer) ? "PlayerAttack" : "EnemyAttack";
        knockbackVal = knockback;
    }

    //Offensive setup. Called when player wants to use a move
    public Transform offensiveSetup(int priority, IMove curMove, float kbVal) {
        initialSetup(priority, curMove, kbVal);

        //load variables
        Transform hitbox = GetComponent<Transform>();
        Animator anim = currentMove.getBasis().GetComponent<Animator>();

        int xDir = anim.GetInteger("aHorizontalDirection");
        int yDir = anim.GetInteger("aVerticalDirection");

        float rotation = 90f * xDir;

        if (rotation == 0)
            rotation = 90f + 90f * yDir;
        else
            rotation += Mathf.Sign(rotation) * yDir * 45f;

        Vector2 dirVector = new Vector2(xDir, yDir);
        dirVector.Normalize();

        Vector2 pos = dirVector * OFFSET;
        knockback = dirVector * knockbackVal;

        //Enact variables and instantiate
        hitbox.localPosition = pos;
        hitbox.eulerAngles = new Vector3(0, 0, rotation);

        Transform curBlast = Object.Instantiate(hitbox, anim.transform);
        curBlast.GetComponent<ShotgunHitbox>().currentMove = this.currentMove;
        curBlast.parent = null;
        return curBlast;
    }


    // Update is called once per frame
    void FixedUpdate() {
        timer += Time.deltaTime;

        if (timer >= DURATION)
            StartCoroutine(destroyProj());
    }

    //On trigger enter. If move hits an enemy, enactEffects of move
    void OnTriggerEnter2D(Collider2D target) {
        if(hit == null)
            hit = new HashSet<Collider2D>();

        if (target.tag == enemyTag && !hit.Contains(target) && !dead) {
            hit.Add(target);
            currentMove.enactEffects(target);
            target.GetComponent<Rigidbody2D>().AddForce(knockback);
        }
    }

    //Destroys projectile without disrupting other coroutines
    private IEnumerator destroyProj() {
        dead = true;
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;
        yield return new WaitForSeconds(0.4f);
        Object.Destroy(gameObject);
    }
}
