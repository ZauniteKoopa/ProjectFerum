using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PKMNEntity : MonoBehaviour
{
    //The following MUST be filled out in the given inspector
    public string fighterName;  //Gives the pokemon's base stats by classification (First letter capitalized)
    public int level;           //Scales base stat accordingly (Must be greater than 0)
    public string[] moveList;   //Gives a list of moves to be made. Ordered as follows = [Sec, sec, sec, Primary] (# of secondary moves can vary. MAX 3)
    public Interactable[] connectedElements; //Array of connected elements that this fighter has influence on upon death

    public int[] baseStats;    //Stats that serve as a marker. Will only be change upon level up
    private float[] curStats;     //In-Game stats that can be changed in battle by enemy or self-buffs

    private List<IMove> moves;
    private SE_PriorityQueue statChangeQ;

    public AudioSource soundFXs;     

    //Stats derived from baseStats
    private float baseMoveSpeed;
    private float moveSpeed;
    private float baseArmor;
    private float armor;
    private float shieldDamageMult;
    private float fullSDFactor;
    private const float BASE_SHIELD_DAMAGE_MULT = 45f;
    private const float ARMOR_FACTOR = 1.1f;
    private const float REDUCED_ATTK_MOVE_FACTOR = 0.4f;   //Constant for reduced movement speed while attacking certain moves

    //Boolean locking variables
    private bool shieldStunned;     //Boolean locking variable that checks if armor is 0
    public bool attacking;          //Boolean locking variable that checks if player is attacking (while moving)
    private bool assistStatus;      //Boolean locking variable that checks if entity is executing assist move (ONLY PLAYER)
    private bool dead;              //Boolean locking variable dead

    //Reference variables
    private Controller controller;

    //Regen variables: X seconds per unit
    private const float HEALTH_REGEN = 3f;
    private const float ARMOR_REGEN = 5.5f;
    private const float A_REGEN_PERCENT = 0.125f;
    private const float H_REGEN_PERCENT = 0.02f;
    private float aTimer;
    private float hTimer;

    //UI Elements
    public Image healthBar;
    public Image armorBar;
    public ProgressBar progress;

    //EXP and level up Management
    private const float BASE_MAX_EXP = 40f;
    private const float MAX_EXP_GROWTH = 20f;
    private float maxExp;
    private float curExp;
    private int numOpenStatBoosts;
    private int[] statUpgradesUsed;

    //Damage control methods
    private const float INVINCIBILITY_TIME = 0.075f;
    private bool invincibilityFrame;
    private float iTimer;

    // Awake is called when game boots up.
    //  Pre: pkmnSpecies string must be filled out and found within the baseStatInv in the static BaseStats class
    void Awake() {
        if (fighterName == null || level <= 0 || level > 100)
            throw new System.Exception("Error: Parameters for PKMNEntity not filled out appropriately");

        statChangeQ = new SE_PriorityQueue(this);                   //Establish stat priority queue
        curStats = new float[BaseStat.BASE_STATS_LENGTH];           //Establish curStats
        statUpgradesUsed = new int[BaseStat.BASE_STATS_LENGTH];
        controller = (transform.parent != null) ? transform.parent.GetComponent<Controller>() : GetComponent<Controller>();
        soundFXs = GetComponent<AudioSource>();

        shieldDamageMult = BASE_SHIELD_DAMAGE_MULT;
        restoreCurStats();
        armor = baseArmor;
        curStats[BaseStat.HEALTH] = baseStats[BaseStat.HEALTH];
                                                 //Establish stats for character
        moves = new List<IMove>();

        //Adds move to the list
        for (int i = 0; i < moveList.Length; i++) {
            IMove curMove = BaseStat.moveInv(moveList[i], GetComponent<Animator>(), this, progress);

            if (curMove != null)
                moves.Add(curMove);
        }

        maxExp = BASE_MAX_EXP;
        curExp = 0f;
        aTimer = 0.0f;
        hTimer = 0.0f;
        assistStatus = false;
        dead = false;

        //numOpenStatBoosts = 6;
    }

    // Fixed Update used to allow regeneration of health and armor
    void FixedUpdate() {
        //Update Timers
        float delta = Time.deltaTime;
        hTimer += delta;
        aTimer += delta;

        //Health Regen Checker
        if(hTimer >= HEALTH_REGEN) {
            //Check for the case of health overflow (Health is full already)
            if (curStats[BaseStat.HEALTH] < baseStats[BaseStat.HEALTH])
                curStats[BaseStat.HEALTH] += baseStats[BaseStat.HEALTH] * H_REGEN_PERCENT;

            if (curStats[BaseStat.HEALTH] > baseStats[BaseStat.HEALTH])
                curStats[BaseStat.HEALTH] = baseStats[BaseStat.HEALTH];

            updateUIBars();
            hTimer = 0f;    //Reset
        }

        //Armor Regen Checker
        if(aTimer >= ARMOR_REGEN) {
            if(armor < baseArmor)
                armor += baseArmor * A_REGEN_PERCENT;    //Update armor

            //Update in the case of armor overflow
            if (baseArmor < armor)
                armor = baseArmor;

            updateUIBars();
            aTimer = 0f;
        }

        //Does CD / ammo regeneration or status decay on all moves
        foreach (IMove move in moves)
            move.regen();

        //Updates timer in statChangeQ
        statChangeQ.update();

        //Invincibility frame
        if(invincibilityFrame) {
            iTimer += delta;

            if(iTimer >= INVINCIBILITY_TIME) {
                iTimer = 0f;
                invincibilityFrame = false;
            }
        }
    }

    //Helps avoid drift
    void OnCollisionEnter2D(Collision2D collision) {
        PKMNEntity fighter = collision.collider.GetComponent<PKMNEntity>();

        if(fighter != null && fighter.isStunned())
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    }

    //Helps minimize drift
    void OnCollisionStay2D(Collision2D collision) {
        PKMNEntity fighter = collision.collider.GetComponent<PKMNEntity>();

        if(fighter != null && fighter.isStunned())
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
    }

    //Accessor method for controller
    public Controller getController() {
        return controller;
    }

    //Accessor Method for movementSpeed
    public float getMoveSpeed() {
        return moveSpeed;
    }

    //Accessor method to boolean shieldStuned
    public bool isStunned() {
        return shieldStunned || dead;
    }

    //Accessor method for move list
    public List<IMove> getMoves() {
        return moves;
    }

    //Accessor method to experience for fill method
    public float getPercentToLvL() {
        return curExp / maxExp;
    }

    //Accessor method for numOpenStatBoosts
    public int getNumStatBoosts() {
        return numOpenStatBoosts;
    }

    //Accessor method for statsBoosts used for a certain stat
    //  Pre: 0 <= statType < 6
    public int getStatBoostsUsed(int statType) {
        return statUpgradesUsed[statType];
    }

    //Accessor method to string representation of armor
    //  Used only for UI Purposes
    public string armorToString() {
        int armorRep = (armor <= 0) ? 0 : (armor < 1) ? 1 : (int)armor;
        return armorRep + "/" + (int)baseArmor;
    }

    //Accessor method that indicates if character is alive
    public bool isAlive() {
        return !dead;
    }

    //Mutator method of assist status of unit.
    public void setAssist(bool status) {
        assistStatus = status;
    }

    //Accessor method of assist status of unit
    public bool getAssist() {
        return assistStatus;
    }

    //Accessor method for a specified stat in curStat
    //  Pre: 0 <= statType < 6
    //  Post: Returns the specified stat in integer form
    public float accessStat(int statType) {
        return curStats[statType];
    }

    //Accessor method for baseStat
    //  Pre: 0 >= statType < 6
    //  Post: Returns the base stat (a stat that represents an unchanged version of stat)
    public int accessBaseStat(int statType) {
        return baseStats[statType];
    }

    //Level Up Bonus constants
    private const float HEALTH_GAIN_PERCENT = 0.1f;

    //Experience gain and level up method
    //  Pre: an enemy has died and the player gains exp. enemyLvl > 0 and baseExp > 0
    //  Post: exp has been added and enemy levels up if reached the requirement
    public void gainExp(float baseExp, int enemyLevel) {
        curExp += BaseStat.expGainCalc(baseExp, level, enemyLevel);

        //Level up method
        if(curExp >= maxExp) {
            if(level % 2 == 1) {        //Health growth
                int prevHealth = baseStats[BaseStat.HEALTH];
                baseStats[BaseStat.HEALTH] = BaseStat.healthGrowth(statUpgradesUsed[BaseStat.HEALTH], baseStats[BaseStat.HEALTH]);
                curStats[BaseStat.HEALTH] += (baseStats[BaseStat.HEALTH] - prevHealth);
                statUpgradesUsed[BaseStat.HEALTH]++;
            }else{
                curStats[BaseStat.HEALTH] += HEALTH_GAIN_PERCENT * baseStats[BaseStat.HEALTH];
                if(curStats[BaseStat.HEALTH] > baseStats[BaseStat.HEALTH])
                    curStats[BaseStat.HEALTH] = baseStats[BaseStat.HEALTH];
            }

            //Experience management
            numOpenStatBoosts++;
            curExp -= maxExp;
            maxExp += MAX_EXP_GROWTH;
            level++;

        }
    }

    //Decrement method for getNumStatBoosts upon its "usage"
    //  Pre: Only to be used when player wants to level up & 0 <= stat < 6 & numStatBoosts > 0
    public void useStatBoost(int stat, int newStatValue) {
        float prevShieldAvg = (curStats[BaseStat.DEFENSE] + curStats[BaseStat.SPECIAL_DEFENSE]) / 2f;  //Calculating previous shield mult
        float prevBaseArmor = baseArmor;

        baseStats[stat] = newStatValue;
        curStats[stat] = newStatValue;
        statUpgradesUsed[stat]++;

        float newShieldAvg = (curStats[BaseStat.DEFENSE] + curStats[BaseStat.SPECIAL_DEFENSE]) / 2f;    //Calculating new shield mult
        shieldDamageMult += newShieldAvg - prevShieldAvg;

        statChangeQ.clear();
        updateSubStats();
        armor += (armor <= 0) ? 0 : (baseArmor - prevBaseArmor);
        armorBar.fillAmount = armor / baseArmor;
        numOpenStatBoosts--;
        controller.SendMessage("updateUIStatus", this);
    }

    //Updates substats (stats that are derived from the curStats
    private void updateSubStats() {
        //Calculated sub stats from base stats
        baseMoveSpeed = BaseStat.movementSpeedCalc((int)curStats[BaseStat.SPEED]);
        baseArmor = ARMOR_FACTOR * (curStats[BaseStat.DEFENSE] + curStats[BaseStat.SPECIAL_DEFENSE]) / 2f;
        moveSpeed = (attacking) ? baseMoveSpeed * REDUCED_ATTK_MOVE_FACTOR : baseMoveSpeed;

        //Calculating shield damage factor
        fullSDFactor = shieldDamageMult / ((curStats[BaseStat.DEFENSE] + curStats[BaseStat.SPECIAL_DEFENSE]) / 2f);

        if (armor > baseArmor)
            armor = baseArmor;
    }

    //Updates UI Elements according to currentHealth and currentArmor
    public void updateUIBars() {
        healthBar.fillAmount = curStats[BaseStat.HEALTH] / baseStats[BaseStat.HEALTH];
        armorBar.fillAmount = armor / baseArmor;
    }

    //Restore all stats in curStat to be equal in base stat EXCEPT type (Type is constant)
    //  Pre: BaseStat.TYPE = last index in baseStats, baseStat.length == curStat.length
    //  Post: all stats in this entity is restored
    public void restoreCurStats() {
        for (int i = 1; i < baseStats.Length; i++)
            curStats[i] = baseStats[i];

        statChangeQ.clear();
        updateSubStats();

    }
 
    //Allows changing a specified stat by a factor. Meant to be accessed by the PKMNEntitiy's Move
    //  Pre: 0 <= stat < BaseStat.TYPE & factor > 0
    //  Post: new curStat = old curStat * factor (def * 2.0 means defense doubled)
    public void changeStat(float factor, int stat) {
        if (factor <= 0 || stat < 0 || stat >= BaseStat.BASE_STATS_LENGTH)
            throw new System.ArgumentException("Error: Invalid Parameters for changeStat");

        curStats[stat] *= factor;
        updateSubStats();
    }

    //Adds stat effect to the queue
    //  Pre: effect != null and effect.StatFactor != 0
    //  Post: effect is added to stat queue
    public void addStatQ(StatEffect effect) {
        statChangeQ.add(effect);
    }

    //Adds intensity of poison to the stat effect queue (WILL BE GENERALIZED TO MULTIPLE EFFECTS)
    //  If positive, poison unit. If negative, cures unit
    public void poisonUnit(float intensity) {
        statChangeQ.editPoisonIntensity(intensity);
    }

    //Executes primary, melee move
    //  Pre: none
    public void executePrimaryMove() {
        IMove primaryMove = moves[moves.Count - 1];

        if (primaryMove.canRun())
            StartCoroutine(primaryMove.execute());
        else
            Debug.Log("You can't run that move at this moment");
    }

    //Executes enemy primary move
    //  Pre: target != null and has a valid position
    public void executePrimaryMove(Transform target) {
        IMove primaryMove = moves[moves.Count - 1];

        if (primaryMove.canRun())
            StartCoroutine(primaryMove.enemyExecute(target));
    }

    //Executes a secondary move that maps to a specied index
    //  Pre: moveIndex >= 0 && moveIndex < numSecMoves()
    public void executeSecMove(int moveIndex) {
        if (moveIndex < 0 || moveIndex >= moves.Count - 1)
            throw new System.ArgumentException("Error: Invalid index for moves");

        IMove secMove = moves[moveIndex];

        if (secMove.canRun()) {
            StartCoroutine(secMove.execute());
        } else
            Debug.Log("You can't run that move at this moment");
    }

    //Executes enemy secondary moves
    //  Pre: moveIndex >= 0 && moveIndex < numSecMove, target != null
    //  Post: returns a boolean representing whether the move ran or not
    public bool executeSecMove(int moveIndex, Transform target) {
        if (moveIndex < 0 || moveIndex >= moves.Count - 1)
            throw new System.ArgumentException("Error: Invalid index for moves");

        IMove secMove = moves[moveIndex];
        bool moveRan = secMove.canRun();

        if (moveRan)
            StartCoroutine(secMove.enemyExecute(target));

        return moveRan;
    }

    //Executes an assist, automated version of the sec move for the player
    //  Pre: moveIndex >= 0 && moveIndex < numSecMoves()
    //  Post: returns a boolean indicating that the move ran
    public bool executeAssistMove(int moveIndex) {
        if (moveIndex < 0 || moveIndex >= moves.Count - 1)
            throw new System.ArgumentException("Error: Invalid index for moves");

        ISecMove secMove = (ISecMove)moves[moveIndex];
        bool executed = secMove.canRun();

        if (executed) {
            StartCoroutine(secMove.assistExecute());
        }else
            Debug.Log("You can't run that move at this moment");

        return executed;
    }

    //Accessor method on the number of secondary moves available
    public int getNumSecMoves() {
        return moves.Count - 1;
    }

    //Reduces movement speed upon certain attack (preferably a projectile)
    public void moveAttack() {
        moveSpeed *= REDUCED_ATTK_MOVE_FACTOR;
        attacking = true;
    }

    //Increase movement upon leaving attack
    //  Pre: moveAttack() MUST'VE been used earlier
    public void moveAttackLeave() {
        attacking = false;
        moveSpeed = baseMoveSpeed;
    }

    private const float KNOCKBACK_DURATION = 0.15f;       //Const value for how long knockback will last
    private const float BREAK_RECOIL = 0.35f;
    private const float ENEMY_SHIELD_STUN = 2.5f;
    private const float PLAYER_SHIELD_STUN = 1.75f;
    private const float MOVE_REDUCTION = 0.25f;
    private const float STUN_TRANS_VALUE = 0.75f;

    //Send damage for event driven programming. Usually sent by a hitbox
    public IEnumerator receiveDamage(int damage, PKMNEntity lastHitFighter) {
        bool shieldStunAttack = false;  //Boolean lock variable to turn on when receiving attack destroys shield

        if(!invincibilityFrame) {
            invincibilityFrame = true;  //Activate invincibility frame

            //Decrements health and armor
            curStats[BaseStat.HEALTH] -= damage;
            armor -= damage * fullSDFactor;
            updateUIBars();

            //Resets timers
            hTimer = 0.0f;
            aTimer = 0.0f;

            if (curStats[BaseStat.HEALTH] <= 0){
                yield return StartCoroutine(death(lastHitFighter));
            }else if (armor <= 0 && !shieldStunned){
                yield return StartCoroutine(shieldStun());
                shieldStunAttack = true;
            }
        }

        //Cancel knockback in all cases unless it's the attack that shieldStuns unit
        if(!shieldStunAttack && !GetComponent<DashChargeHitBox>().isDashing() && !GetComponent<Animator>().GetBool("Dashing")) {
            //Disables knockback after a period of time
            moveSpeed *= MOVE_REDUCTION;
            yield return new WaitForSeconds(KNOCKBACK_DURATION);
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            moveSpeed = baseMoveSpeed;
        }
    }

    //Does damage over time that does not EFFECT shields
    //  If crippled (less than a certain % of hp), slow movement and do good damage to armor
    private float CRIPPLE_PERCENT = 0.4f;
    private float DOT_ARMOR_PERCENT = 0.25f;
    private float MIN_ARMOR_RESERVED = 0.2f;

    public IEnumerator receiveDoT(int damage, PKMNEntity lastHitFighter) {
        curStats[BaseStat.HEALTH] -= damage;
        hTimer = 0.0f;
        aTimer = 0.0f;

        //Method that checks armor
        float minReq = baseArmor * MIN_ARMOR_RESERVED;

        if(curStats[BaseStat.HEALTH] <= CRIPPLE_PERCENT * baseStats[BaseStat.HEALTH] && !shieldStunned && armor > minReq) {
            //Cripples movement
            moveSpeed *= MOVE_REDUCTION;
            yield return new WaitForSeconds(KNOCKBACK_DURATION);
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            moveSpeed = baseMoveSpeed;

            //Do armor damage, but DO NOT SHIELD STUN once armor damage is too much
            float curArmor = armor;
            armor -= baseArmor * DOT_ARMOR_PERCENT;
            if(armor <= minReq)
                armor = minReq;
        }

        updateUIBars();

        if(curStats[BaseStat.HEALTH] <= 0 && !dead)
            if(lastHitFighter != null)
                yield return StartCoroutine(death(lastHitFighter));
            else
                curStats[BaseStat.HEALTH] = 1f;
    }

    //When armor is broken by an attack, shield stun this unit
    private IEnumerator shieldStun() {
        Animator anim = GetComponent<Animator>();

        shieldStunned = true;                                   //Set boolean locking variable to true
        anim.SetBool("Stunned", true);                          //Change animation state
        anim.SetFloat("speed", 0f);
        anim.SetBool("SpAttacking", false);
        anim.SetBool("PhyAttacking", false);
        anim.SetBool("Dashing", false);

        if(!assistStatus)
            controller.canMove = false;                             //Disable movement if entity hit isn't an assist move

        //Change transparency of sprite
        Color temp = GetComponent<SpriteRenderer>().color;
        temp.a = STUN_TRANS_VALUE;
        GetComponent<SpriteRenderer>().color = temp;

        //Allow sound effect
        soundFXs.Stop();
        soundFXs.clip = Resources.Load<AudioClip>("Audio/ShieldBreak");
        soundFXs.Play();

        //Adds shield break knockback and disables after a period of time
        yield return new WaitForSeconds(BREAK_RECOIL);
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;

        if(tag == "Player") {
            Time.timeScale = 0.1f;
            yield return new WaitForSeconds(0.15f);
            Time.timeScale = 1f;
        }

        //Allow stun
        float shieldStun = (controller.tag == "Player") ? PLAYER_SHIELD_STUN : ENEMY_SHIELD_STUN;
        yield return new WaitForSeconds(shieldStun);
        soundFXs.Stop();
        anim.SetBool("Stunned", false);                         //Reset Animation state

        //Reset
        temp.a = 1;
        GetComponent<SpriteRenderer>().color = temp;

        if(!dead)
            controller.canMove = true;

        armor = baseArmor;
        shieldStunned = false;
        updateUIBars();
    }

    //Death algorithm for PKMN Entity
    private IEnumerator death(PKMNEntity lastHitFighter) {
        dead = true;

        //Interact with each interactable upon death
        foreach(Interactable element in connectedElements)
            element.unlock(1);

        if (transform.tag == "Player" || transform.tag == "PlayerAttack" && GetComponent<DashChargeHitBox>() != null)         //If it's a player, send message to controller to alter UI elements
            controller.gameObject.SendMessage("partnerDeath", assistStatus);
        else {
            lastHitFighter.getController().GetComponent<PlayerMovement>().gainExp(level, lastHitFighter);   //If enemy, transfer ExP
            controller.enabled = false;
        }

        soundFXs.Stop();
        soundFXs.clip = Resources.Load<AudioClip>("Audio/DeathCries/" + fighterName);
        soundFXs.Play();

        GetComponent<SpriteRenderer>().enabled = false;
        enabled = false;
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        yield return new WaitForSeconds(1.2f);
        gameObject.SetActive(false);
    }
}
