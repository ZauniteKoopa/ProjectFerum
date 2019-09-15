using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionBehavior : MonoBehaviour {

    //Info on explosion
    private IMove currentMove;
    private string enemyTag;
    //private string enemyAttackTag;
    private float knockbackVal;
    private int priority;

    //Explosion variables
    private const float EXPLOSION_DURATION = 0.15f;
    private float timer;
    private bool active;

    public AudioSource boomSound;       //Sound effect

    // Start is called before the first frame update
    public void setUp(float knockback) {
        AttackBox bombInfo = transform.parent.GetComponent<AttackBox>();
        currentMove = bombInfo.currentMove;
        enemyTag = bombInfo.enemyTag;
        //enemyAttackTag = bombInfo.enemyAttackTag;
        priority = bombInfo.priority + 1;

        knockbackVal = 2 * knockback;
        timer = 0.0f;
        active = false;
    }

    // Update is called once per frame
    void FixedUpdate() {
        if(active) {
            timer += Time.deltaTime;

            if (timer > EXPLOSION_DURATION)
                Object.Destroy(gameObject);
        }
    }

    //Checks if something is caught in the explosion
    void OnTriggerEnter2D(Collider2D collider) {
        if (collider.tag == enemyTag) {
            Vector2 knockbackDir = Battle.dirKnockbackCalc(transform.position, collider.transform.position, knockbackVal);
            collider.GetComponent<Rigidbody2D>().AddForce(knockbackDir);
            currentMove.enactEffects(collider);
        }
    }

    //Activates explosion
    public void activate() {
        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<Collider2D>().enabled = true;
        active = true;
        transform.parent = null;

        //Explosion Sound effect
        boomSound.Stop();
        boomSound.clip = Resources.Load<AudioClip>("Audio/AttackSounds/Explode");
        boomSound.Play();
    }
}
