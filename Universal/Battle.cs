using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Battle{

    private const int DEGREE_DIV = 45;
    private const float OFFSET = 22.5f;

    /*OFFICIAL MAPPING FOR AHOR AND AVERT:
        0, 4 --> 0  (No Direction)
        1-3 ---> 1  (Right / Up Direction)
        5-7 ---> -1 (Left / Down Direction)
    */
  
    //Updates the player's attack orientation based on the mouse's position relative to the player
    //  Pre: player == a non-null transform that represents the player / user
    //  Post: Attack Orientation is updated so that the player is facing towards the direction of the mouse
    public static void updatePlayerAOrientation(Transform player) {

        //Calculate the vector that points from the player position to the mouse (vDiff)
        Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);
        Vector2 vDiff = new Vector2(mousePos.x - player.position.x, mousePos.y - player.position.y);

        updateAttackOrientation(vDiff, player);
    }

    //Updates the player attack orientation based on target's position relative to the enemy
    //  Pre: enemy is a non-null fighter that represents the enemy unit while target represents target unit
    //  Post: enemy either looks towards the enemy or away
    public static void updateEnemyAOrientation(Transform enemy, Transform target, bool goTowards) {
        Vector2 vDiff = new Vector2(target.position.x - enemy.position.x, target.position.y - enemy.position.y);
        vDiff *= (goTowards) ? 1 : -1;

        updateAttackOrientation(vDiff, enemy);
    }

    //Calculates attack orientation from a vector
    //  Pre: vDiff != null and entity != null and contains a fighter animator & a sprite renderer
    //  Post: the entity's animator attack orientation is updated accordingly
    private static void updateAttackOrientation(Vector2 vDiff, Transform entity) {
        //Calculate the angle of the vector 
        float deg = Mathf.Atan2(vDiff.y, vDiff.x) * Mathf.Rad2Deg;
        deg = (deg < 0) ? deg + 360 + OFFSET : deg + OFFSET;
        deg = (deg >= 360) ? deg - 360 : deg;

        //Calculate the key value
        int keyValue = (int)(deg / DEGREE_DIV);
        int adjKeyValue = (keyValue + 2) % 8;   //Adjust key value so that aHorizontal has the same mapping as aVertical

        //Calculate the new attack vertical and attack horizontal orientation
        int newAVert = (keyValue % 4 == 0) ? 0 : -1 * (int)Mathf.Sign(keyValue - 4f);
        int newAHor = (adjKeyValue % 4 == 0) ? 0 : -1 * (int)Mathf.Sign(adjKeyValue - 4f);

        //Set new animation parameters and check to see if flipping necessary
        Animator anim = entity.GetComponent<Animator>();
        entity.GetComponent<SpriteRenderer>().flipX = (newAHor < 0) ? false : true;
        anim.SetInteger("aHorizontalDirection", newAHor);
        anim.SetInteger("aVerticalDirection", newAVert);
    }

    //Calculates basic movement orientation for enemy from a vector (from entity to location)
    //  Pre: vDiff != zero vector and entity contains an animator corresponding to a fighter AND a sprite renderer
    //  Post: entity's basic orientation is updated accordingly
    public static void updateBasicOrientation(Vector2 movement, Transform entity) {
        //Calculate the angle of the vector 
        float deg = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
        deg = (deg < 0) ? deg + 360 + OFFSET : deg + OFFSET;
        deg = (deg >= 360) ? deg - 360 : deg;

        //Calculate the key value
        int keyValue = (int)(deg / DEGREE_DIV);
        int adjKeyValue = (keyValue + 2) % 8;   //Adjust key value so that aHorizontal has the same mapping as aVertical

        //Calculate the new attack vertical and attack horizontal orientation
        int newAVert = (keyValue % 4 == 0) ? 0 : -1 * (int)Mathf.Sign(keyValue - 4f);
        int newAHor = (adjKeyValue % 4 == 0) ? 0 : -1 * (int)Mathf.Sign(adjKeyValue - 4f);

        //Set new animation parameters and check to see if flipping necessary
        Animator anim = entity.GetComponent<Animator>();
        entity.GetComponent<SpriteRenderer>().flipX = (newAHor < 0) ? false : true;
        anim.SetInteger("horizontalDirection", newAHor);
        anim.SetInteger("verticalDirection", newAVert);
    }

    //Calculates knockback based on informati components "horizontalDirection" and "verticalDirection". knockbackVal > 0
    //  Post: Returns a vector2 representon given by the attacker animator (source of attack)
    //  Pre: animator MUST haveing the direction and amplitude of knockback
    public static Vector2 sourceKnockbackCalc(Animator animator, float knockbackVal) {
        Battle.updatePlayerAOrientation(animator.GetComponent<Transform>());
        int hMove = animator.GetInteger("aHorizontalDirection");
        int vMove = animator.GetInteger("aVerticalDirection");

        Vector2 result;

        //Calculates knockback based on orientation of player at time of attack
        if (hMove != 0 && vMove != 0) {                             //Condition where source is diagonal
            float diagFactor = Mathf.Sqrt(2) / 2f;
            result = new Vector2(diagFactor * knockbackVal * hMove, diagFactor * knockbackVal * vMove);
        } else if (hMove != 0) {                                     //Condition where source is horizontal
            result = new Vector2(knockbackVal * hMove, 0f);
        } else {                                                      //Condition where source is vertical
            result = new Vector2(0f, knockbackVal * vMove);
        }

        return result;
    }

    //Calculates knockback based on direction from entity to mouse
    //  Pre: entityPos represents the position of a living player/enemy in game. KnockbackVal > 0
    //  Post: Returns a Vector2 with the direction from entity to mouse with magnitude knockbackVal
    public static Vector2 dirKnockbackCalc(Vector2 entityPos, float knockbackVal) {
        //Calculate mouse vector in world space
        Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);

        //Calculate new result vector and normalize
        Vector2 result = new Vector2(mousePos.x - entityPos.x, mousePos.y - entityPos.y);
        result.Normalize();

        return result *= knockbackVal;
    }

    //Returns a vector of magnitude knockbackVal that goes from the entity to its enemy
    //  Pre: entityPos and enemyPos are 2D transform positions, kncobackVal > 0
    //  Post: Returns a vector of magnitude knockbackVal that points from entity to enemy
    public static Vector2 dirKnockbackCalc(Vector2 entityPos, Vector2 enemyPos, float knockbackVal) {
        Vector2 result = new Vector2(enemyPos.x - entityPos.x, enemyPos.y - entityPos.y);
        result.Normalize();
        return result *= knockbackVal;
    }

    //Calculates damage for a given move based on the official pokemon damage formula
    //  Pre: 0 < lvl <= 100, 0 < power, attDefRatio = attacker attack / victim defense
    //  Post: Returns an int representing the amount of damage applied to enemy
    public static int damageCalc(int level, int power, float entityAttack, float enemyDef) {
        float attDefRatio = entityAttack / enemyDef;
        //float baseDamage = 5 * entityAttack * 0.02f * power;             //Base damage is half of your attack

        float damage = (0.5f * level + 2) * power * attDefRatio * 0.06f;
        damage += 1;
        return (int)damage;
    }

    //Checks if the given PKMNEntity is a player
    public static bool isPlayer(Collider2D collider) {
        PKMNEntity fighter = collider.GetComponent<PKMNEntity>();
        return fighter != null && (collider.tag == "Player" || collider.tag == "PlayerAttack" || collider.tag == "PlayerRecovery");
    }

}
