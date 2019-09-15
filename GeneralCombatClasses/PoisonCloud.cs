using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoisonCloud : MonoBehaviour
{
    //Poison rate and damage
    private const float POISON_RATE = 0.6f;
    private const int POISON_PWR = 12;
    public PKMNEntity source;
    public float offenseStat;
    private float dTimer;

    //Timeout
    private const float TIMEOUT = 5f;
    private float timer;

    //Hashset data structure to keep track of who's in the area
    private HashSet<PKMNEntity> hit;
    public string enemyTag;
    private string enemyAttackTag;

    void Awake() {
        hit = new HashSet<PKMNEntity>();
    }

    //Offensive setup
    public void offensiveSetup(PKMNEntity source) {
        this.source = source;
        offenseStat = source.accessStat(BaseStat.SPECIAL_ATTACK);
        enemyTag = (source.tag == "Player" || source.tag == "PlayerRecovery") ? "Enemy" : "Player";
        enemyAttackTag = (source.tag == "Player" || source.tag == "PlayerRecovery") ? "EnemyAttack" : "PlayerAttack";
        
        GetComponent<SpriteRenderer>().color = (source.tag == "Player" || source.tag == "PlayerRecovery") ? new Color(0.65f, 0.31f, 0.95f, 0.5f) : new Color(0.39f, 0.26f, 0.51f, 0.5f);
    }

    //Fixed Update method to calculate timeout
    void FixedUpdate() {
        float delta = Time.deltaTime;
        timer += delta;
        dTimer += delta;

        if(dTimer >= POISON_RATE) {
            foreach(PKMNEntity enemy in hit) {
                int damage = Battle.damageCalc(source.level, POISON_PWR, offenseStat, enemy.accessStat(BaseStat.SPECIAL_DEFENSE));
                enemy.StartCoroutine(enemy.receiveDoT(damage, source));    
            }

            dTimer = 0f;
        }

        if(timer >= TIMEOUT)
            Object.Destroy(gameObject);
    }

    //If someone enters, add them to data structure
    void OnTriggerEnter2D(Collider2D collider) {
        PKMNEntity enemy = collider.GetComponent<PKMNEntity>();

        if(collider.tag == enemyTag || (collider.tag == enemyAttackTag && enemy != null)) {
            int damage = Battle.damageCalc(source.level, POISON_PWR, offenseStat, enemy.accessStat(BaseStat.SPECIAL_DEFENSE));
            enemy.StartCoroutine(enemy.receiveDoT(damage, source));
            hit.Add(enemy);

            if(enemyTag == "Player")
                enemy.getController().SendMessage("setIsolation", true);
        }
    }

    //If someone exits the zone
    void OnTriggerExit2D(Collider2D collider) {
        PKMNEntity enemy = collider.GetComponent<PKMNEntity>();

        if(hit.Contains(enemy)) {
            hit.Remove(enemy);

            if(enemyTag == "Player" && enemy.isAlive())
                enemy.getController().SendMessage("setIsolation", false);
        }
    }

    //Activates poison cloud
    public void activate() {
        gameObject.SetActive(true);
        transform.parent = null;
    }
}
