using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerMovement : Controller
{
    //Reference variable
    public PKMNEntity selectedFighter;
    public Transform attack;
    private Transform player;
    private Animator animator;
    public StatsMenuDisplay statMenu;

    //Move tracking variables and scrolling
    private int secMoveIndex;
    private bool scrolled;                      //Boolean locking variable for scrolling control
    private const float SCROLL_DELAY_DURATION = 0.07f;

    //Teamwork / Party Member variables
    private int partyIndex;
    private int numPartners;
    private const float TRANSITION_TIME = 0.35f;
    public bool isolated;                   //A stat effect that makes a player unable to switch (MAKE THIS A STAT EFFECT WITH POISONED)

    //Assist move variables
    private PKMNEntity assistFighter;
    private bool assistSeqOn;               //checks if slow-mo sequence is still going
    private bool assistMoveExecuted;        //Checks if the assist move has been executed

    //Death / Recovery variables
    private const float DEATH_WAIT = 2f;

    //UI Elements
    public AbilityUI[] abilityIconSets;
    public Image[] healthBars;
    public Image[] armorBars;
    public MainAbilitySwapper mainSwapper;
    public Dictionary<PKMNEntity, AbilityUI> abilityUIMap;
    public ProgressBar assistTimerUI;

    // Start is called before the first frame update
    void Start() {
        //Set reference variables
        player = selectedFighter.GetComponent<Transform>();
        animator = selectedFighter.GetComponent<Animator>();

        //Set canMove to true
        canMove = true;
        assistMoveExecuted = false;

        //Secondary move index
        secMoveIndex = 0;

        //Teamwork variables
        partyIndex = 0;
        selectedFighter = transform.GetChild(partyIndex).GetComponent<PKMNEntity>();
        numPartners = transform.childCount - 1;

        //Set UI Elements
        if (numPartners >= 2)
            abilityIconSets[1].gameObject.SetActive(true);
        if (numPartners >= 3)
            abilityIconSets[2].gameObject.SetActive(true);

        abilityUIMap = new Dictionary<PKMNEntity, AbilityUI>();

        for (int i = 0; i < numPartners; i++) {
            PKMNEntity curFighter = transform.GetChild(i).GetComponent<PKMNEntity>();
            SwitchState curSwitchState = curFighter.gameObject.AddComponent<SwitchState>();

            abilityUIMap[curFighter] = abilityIconSets[i];  //Abilities
            abilityIconSets[i].setUp(curFighter);
            curSwitchState.setUpState(abilityIconSets[i]);

            curFighter.healthBar = healthBars[i];           //Stat bars
            curFighter.armorBar = armorBars[i];
            statMenu.addFighter(curFighter);
        }
    }

    //Changes isolation state
    public void setIsolation(bool isoState) {
        isolated = isoState;
        abilityUIMap[selectedFighter].setIsolation(isoState);
    }

    //Update method that doesn't scale with scaledTime: selecting secondary moves
    void Update() {
        int numSecMoves = selectedFighter.getNumSecMoves();

        //Scrolling through moves
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0 && !scrolled) {
            secMoveIndex = (secMoveIndex + 1 >= numSecMoves) ? 0 : secMoveIndex + 1;
            StartCoroutine(scrollDelay());
        } else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0 && !scrolled) {
            secMoveIndex = (secMoveIndex == 0) ? numSecMoves - 1 : secMoveIndex - 1;
            StartCoroutine(scrollDelay());
        }

        //Quick access to secondary moves
        if (Input.GetKey(KeyCode.Alpha1) && numSecMoves >= 1) {
            secMoveIndex = 0;
            mainSwapper.switchAbilities(secMoveIndex);
        }

        if (Input.GetKey(KeyCode.Alpha2) && numSecMoves >= 2) {
            secMoveIndex = 1;
            mainSwapper.switchAbilities(secMoveIndex);
        }

        if (Input.GetKey(KeyCode.Alpha3) && numSecMoves >= 3) {
            secMoveIndex = 2;
            mainSwapper.switchAbilities(secMoveIndex);
        }

        if (Input.GetKey(KeyCode.Escape)) {
            SceneManager.LoadScene(0);
            Time.timeScale = 1f;
            AudioListener.volume = 1f;
        }

    }

    // Update is called once per frame
    void FixedUpdate() {
        if (canMove) {
            gridMovement();
            combat();

            if(!assistSeqOn && numPartners > 1 && !isolated)        //If assist sequence is taking place, do not allow partner switching or more assist moves
                teamwork();
        }

        //UI controls that can happen in any time
        if (Input.GetKeyDown(KeyCode.LeftShift)) {              //Stats menu
            statMenu.gameObject.SetActive(true);
            statMenu.open();
        }

        //To maintain consistent position
        if (!assistSeqOn) {
            transform.position = player.position;
            player.localPosition = Vector3.zero;
        }
    }

    //Accessor method for selectedFighter
    public PKMNEntity getCurFighter() {
        return selectedFighter;
    }

    //Updator method that updates the UI status of a given fighter
    public void updateUIStatus(PKMNEntity fighter) {
        abilityUIMap[fighter].updateStatus();
    }

    //Method that allows 2D movement when movement keys pressed (WASD)
    void gridMovement() {
        //Bools to check if idle
        bool horizontalMove = true;
        bool verticalMove = true;

        //Horizontal movement conditionals
        if (Input.GetKey("d"))
            horizontalWalk(Vector3.right, 1);
        else if (Input.GetKey("a"))
            horizontalWalk(Vector3.left, -1);
        else
            horizontalMove = false;

        //Vertical movement conditionals
        if (Input.GetKey("w"))
            verticalWalk(Vector3.up, 1);
        else if (Input.GetKey("s"))
            verticalWalk(Vector3.down, -1);
        else
            verticalMove = false;

        //Checks if idle
        if (!verticalMove && !horizontalMove)
            animator.SetFloat("speed", 0f);

        //Move if diagonal
        if (verticalMove && horizontalMove) {
            //diagVector: rt(2)/2, rt(2)/2, 0
            Vector3 diagVector = new Vector3(animator.GetInteger("horizontalDirection") * (Mathf.Sqrt(2) / 2f), animator.GetInteger("verticalDirection") * (Mathf.Sqrt(2) / 2f), 0);
            player.Translate(diagVector * selectedFighter.getMoveSpeed());
        }
    }

    //Private helper method for grid movement. Small function to change walking animating state in terms of horizontal
    //    left = -1, right = 1, none = 0
    void horizontalWalk(Vector3 direction, int value) {

        //Set animator parameters
        animator.SetInteger("horizontalDirection", value);
        animator.SetFloat("speed", selectedFighter.getMoveSpeed());

        //Flips sprite if going right and player is not in an attack animation
        if(!selectedFighter.attacking)
            selectedFighter.GetComponent<SpriteRenderer>().flipX = (value == 1) ? true : false;

        //Checks if any vertical distance done and Move player full horizontal distance if necessary
        if (!Input.GetKey("w") && !Input.GetKey("s")) {
            animator.SetInteger("verticalDirection", 0);
            player.Translate(direction * selectedFighter.getMoveSpeed());
        }
    }

    //Private helper method for grid movement. Small function to change walking animating state in terms of vertical
    //    down = -1, up = 1, none = 0
    void verticalWalk(Vector3 direction, int value){
        //Set animator parameters
        animator.SetInteger("verticalDirection", value);
        animator.SetFloat("speed", selectedFighter.getMoveSpeed());

        //Check if any horizontal distance done and move player full vertical distance if necessary
        if (!Input.GetKey("a") && !Input.GetKey("d")) {
            animator.SetInteger("horizontalDirection", 0);
            player.Translate(direction * selectedFighter.getMoveSpeed());
        }
    }

    //Controller for combat. Wiil mostly focus around the mouse
    void combat() {
        //Executes primary move upon left click
        if (Input.GetMouseButtonDown(0) && !assistSeqOn) 
            selectedFighter.executePrimaryMove();

        //Executes secondary move upon right click
        if (Input.GetMouseButtonDown(1) && !assistSeqOn)
            selectedFighter.executeSecMove(secMoveIndex);
            
    }

    //Private helper method with scrolling through secondary moves without doubling on scrolling
    //  Pre: an item was scrolled through
    private IEnumerator scrollDelay() {
        scrolled = true;
        mainSwapper.switchAbilities(secMoveIndex);
        yield return new WaitForSecondsRealtime(SCROLL_DELAY_DURATION);
        scrolled = false;
    }

    //Controller actions for teamwork: includes changing characters and assist moves
    void teamwork() {

        int leftIndex = (partyIndex - 1 + numPartners) % numPartners;
        int rightIndex = (partyIndex + 1) % numPartners;

        SwitchState leftState = transform.GetChild(leftIndex).GetComponent<SwitchState>();
        SwitchState rightState = transform.GetChild(rightIndex).GetComponent<SwitchState>();

        //Switching between teammates
        if (Input.GetKeyDown("e") && rightState.canSwitch()) {  //Switch right
            int prevPartyIndex = partyIndex;
            partyIndex = rightIndex;

            if (prevPartyIndex != partyIndex)
                StartCoroutine(rotateCharacters());
        }

        if (Input.GetKeyDown("q") && leftState.canSwitch()) {   //Switch left
            int prevPartyIndex = partyIndex;
            partyIndex = leftIndex;

            if (prevPartyIndex != partyIndex)
                StartCoroutine(rotateCharacters());
        }

        //Assist Moves
        if (Input.GetKeyDown("c") && assistFighter == null && leftState.canSwitch()) //Use left character move
            StartCoroutine(assistMoveExecute(leftIndex, player.position));

        if (Input.GetKeyDown("v") && assistFighter == null && rightState.canSwitch()) //Use right character move
            StartCoroutine(assistMoveExecute(rightIndex, player.position));
    }

    //Switches to the character
    //  Pre: fighterIndex must have been changed to a different value before hand
    IEnumerator rotateCharacters() {
        selectedFighter.GetComponent<SwitchState>().disableSwitch(true);    //Disable switch state on selectedFighter

        //Do rotation
        swapMainCharUI(false);

        if (numPartners > 2)
            swapPartnerUI();
        else if(numPartners == 2 && assistFighter != null) {
            int index = (partyIndex == 0) ? 1 : 0;
            PKMNEntity sidePartner = transform.GetChild(index).GetComponent<PKMNEntity>();

            //Swap UI Elements
            swapUIBars(assistFighter, sidePartner);
            abilityUIMap[assistFighter].swapIcons(abilityUIMap[sidePartner]);

            //Update mappings
            AbilityUI temp = abilityUIMap[assistFighter];
            abilityUIMap[assistFighter] = abilityUIMap[sidePartner];
            abilityUIMap[sidePartner] = temp;

            assistFighter.GetComponent<SwitchState>().setUpState(abilityUIMap[assistFighter]);
            sidePartner.GetComponent<SwitchState>().setUpState(abilityUIMap[sidePartner]);
        }

        //Update UI states
        foreach (KeyValuePair<PKMNEntity, AbilityUI> entry in abilityUIMap)
            entry.Value.updateStatus();

        //Do transition animation
        canMove = false;
        float animTimer = 0.0f;

        animator.SetBool("Charging", true);
        yield return new WaitForSeconds(0.1f);
        animator.SetBool("Charging", false);
        animator.SetBool("FinishedCharge", true);

        while (!selectedFighter.isStunned() && animTimer < TRANSITION_TIME && !attacking()) {
            yield return new WaitForFixedUpdate();
            animTimer += Time.deltaTime;
            combat();
        }

        animator.SetBool("FinishedCharge", false);
        if (!selectedFighter.isStunned() && selectedFighter.isAlive())
            canMove = true;
    }

    //Private helper method that returns whether player wants to attack during transition animation
    //  Pre: transition animation MUST be going on
    private bool attacking() {
        return Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
    }

    //Private helper method that swaps the main character (selectedFighter) with a partner character
    //  Pre: new partyIndex does not correspond with current selectedFighter in child array
    //  Post: The UI for main character and a partner character is swapped
    private void swapMainCharUI(bool assist) {
        //Disables current SelectedFighter and stores prev fighter (if assist is false)
        if(!assist) {
            selectedFighter.GetComponent<SpriteRenderer>().enabled = false;
            selectedFighter.GetComponent<Collider2D>().enabled = false;
        }

        PKMNEntity prevFighter = selectedFighter;

        //Enables new fighter
        selectedFighter = transform.GetChild(partyIndex).GetComponent<PKMNEntity>();
        selectedFighter.GetComponent<SpriteRenderer>().enabled = true;
        selectedFighter.GetComponent<Collider2D>().enabled = true;

        //Switch UI elements
        secMoveIndex = 0;
        mainSwapper.switchAbilities(secMoveIndex);
        swapUIBars(selectedFighter, prevFighter);
        abilityUIMap[selectedFighter].swapIcons(abilityUIMap[prevFighter]);

        //Switches fighter mappings
        AbilityUI temp = abilityUIMap[selectedFighter];
        abilityUIMap[selectedFighter] = abilityUIMap[prevFighter];
        abilityUIMap[prevFighter] = temp;

        selectedFighter.GetComponent<SwitchState>().setUpState(abilityUIMap[selectedFighter]);
        prevFighter.GetComponent<SwitchState>().setUpState(abilityUIMap[prevFighter]);

        //Set Main Character variables to fighter
        animator = selectedFighter.GetComponent<Animator>();
        player = selectedFighter.GetComponent<Transform>();
        player.position = transform.position;
    }

    //Private helper method: swap UI elements between partners
    //  Pre: numPartners > 2. Main Character has already been swapped with a partner
    //  Post: the 2 partners have already been swapped. Statuses do not change
    private void swapPartnerUI() {
        List<PKMNEntity> partners = new List<PKMNEntity>();

        //Get all partners (Will always run 3 times)
        for (int i = 0; i < numPartners; i++)
            if (i != partyIndex)
                partners.Add(transform.GetChild(i).GetComponent<PKMNEntity>());

        //Swap UI Elements
        swapUIBars(partners[0], partners[1]);
        abilityUIMap[partners[0]].swapIcons(abilityUIMap[partners[1]]);

        //Update mappings
        AbilityUI temp = abilityUIMap[partners[0]];
        abilityUIMap[partners[0]] = abilityUIMap[partners[1]];
        abilityUIMap[partners[1]] = temp;

        partners[0].GetComponent<SwitchState>().setUpState(abilityUIMap[partners[0]]);
        partners[1].GetComponent<SwitchState>().setUpState(abilityUIMap[partners[1]]);
    }

    //Swaps UI Bars between 2 characters
    //  both fighters MUST be active in the game and have health and armor > 0
    void swapUIBars(PKMNEntity fighter1, PKMNEntity fighter2) {
        //Create temp variables
        Image tempHP = fighter1.healthBar;
        Image tempArmor = fighter1.armorBar;

        //Set fighter1 UI bars to fighter2's
        fighter1.healthBar = fighter2.healthBar;
        fighter1.armorBar = fighter2.armorBar;

        //Set fighter2 to temp
        fighter2.healthBar = tempHP;
        fighter2.armorBar = tempArmor;

        //Update both
        fighter1.updateUIBars();
        fighter2.updateUIBars();
    }

    //Assist move constants
    private const float MAX_ASSIST_SEQUENCE_DURATION = 0.15f;
    private const float SLOWED_TIME_SCALE = 0.05f;

    //Execute assist move method
    //  Pre: Pressed C or V, fighterIndex < numPartners
    //  Post: Do tactical slow down sequence to select move
    IEnumerator assistMoveExecute(int assistIndex, Vector3 mainPosition) {
        int mainIndex = (partyIndex > assistIndex) ? partyIndex - 1 : partyIndex;        //Store new main index to go back to player after attack
        PKMNEntity mainFighter = selectedFighter;
        assistFighter = transform.GetChild(assistIndex).GetComponent<PKMNEntity>();      //Get new assist fighter
        Transform assist = assistFighter.transform;
        float assistHealth = assistFighter.accessStat(BaseStat.HEALTH);                  //Get health statuses
        float mainHealth = selectedFighter.accessStat(BaseStat.HEALTH);
        numPartners--;

        //Enable and detach
        partyIndex = assistIndex;
        swapMainCharUI(true);
        //assist.position = transform.position;

        //Commence slow down sequence
        assistSeqOn = true;
        assistFighter.transform.parent = null;
        assistTimerUI.gameObject.SetActive(true);
        Time.timeScale = SLOWED_TIME_SCALE;
        float curTime = 0.0f;
        bool moveRan = false;
        bool notHit = assistHealth <= assistFighter.accessStat(BaseStat.HEALTH) && mainHealth <= mainFighter.accessStat(BaseStat.HEALTH);

        //Slow down sequence
        while ((Input.GetKey("c") || Input.GetKey("v")) && !moveRan && curTime < MAX_ASSIST_SEQUENCE_DURATION && notHit) {
            assistTimerUI.updateProgress((MAX_ASSIST_SEQUENCE_DURATION - curTime) / MAX_ASSIST_SEQUENCE_DURATION);

            yield return new WaitForSecondsRealtime(0.01f);
            curTime += Time.deltaTime;

            if (Input.GetMouseButton(1))
                moveRan = moveRan || selectedFighter.executeAssistMove(secMoveIndex);

            float curAssistHealth = assistFighter.accessStat(BaseStat.HEALTH);
            float curMainHealth = mainFighter.accessStat(BaseStat.HEALTH);

            notHit = assistHealth <= curAssistHealth && mainHealth <= curMainHealth;    //Checks if main character OR assist character was hit
            assistHealth = curAssistHealth;
            mainHealth = curMainHealth;
        }

        //Set main character back as the selected fighter while keeping enemy detached
        partyIndex = mainIndex;
        goBackToMain(mainPosition);
        Time.timeScale = 1f;
        assistSeqOn = false;
        bool allowDuration = moveRan || !notHit;

        canMove = false;
        yield return new WaitForSeconds(0.18f);
        if (!selectedFighter.isStunned())
            canMove = true;

        if (allowDuration)
            yield return StartCoroutine(automatedAssistDuration(assistHealth, moveRan));

        assistTimerUI.gameObject.SetActive(false);

        if (assistFighter.isAlive())                     //If assist fighter is alive, re implement him back in. If not alive, don't reimplement
            revertAssist(assistIndex, allowDuration);
        else
            abilityUIMap[assistFighter].updateStatus();

        assistFighter = null;   //Turn assist fighter equal to null to indicate that no assist fighter is present
    }

    //Private IEnumerator that allows period for enemy to do automated attacks and wait for a given period of time
    private const float REST_DURATION = 3.75f;
    private const float HIT_DELAY = 1.5f;

    private IEnumerator automatedAssistDuration(float assistHealth, bool moveRan) {
        assistFighter.setAssist(true);      //Set assist status to true
        assistTimerUI.updateProgress(1f);

        //If a move was running, wait until move is done
        if (moveRan)
            yield return new WaitUntil(checkAssistExecutedStatus);

        //Reset to down idle animation
        Animator anim = assistFighter.GetComponent<Animator>();
        anim.SetFloat("speed", 0.0f);
        assistMoveExecuted = false;

        //Upon move finishing, wait for the designated wait period
        float timer = 0.0f;
        float curHealth = assistHealth;

        //If the enemy hit during wait period, timer automatically resets
        while (timer < REST_DURATION && curHealth > 0) {
            yield return new WaitForEndOfFrame();
            timer += Time.deltaTime;
            curHealth = assistFighter.accessStat(BaseStat.HEALTH);

            if(curHealth < assistHealth || assistFighter.isStunned()) {
                timer -= HIT_DELAY;
                timer = (timer < 0) ? 0 : timer;
                assistHealth = curHealth;
            }

            assistHealth = curHealth;
            assistTimerUI.updateProgress((REST_DURATION - timer) / REST_DURATION);
        }
    }

    //Private event driven boolean method that would turn moveExecuted = true upon the execution of an assistMove
    private void assistExecuted() {
        assistMoveExecuted = true;
    }

    //Private helper method that checks if assist executed
    private bool checkAssistExecutedStatus() {
        return assistMoveExecuted;
    }

    //Private helper method for assistMoves: set selectedCharacter back to previous main character
    //  Pre: assist move is already in execution and partyIndex changed to the previous mainIndex
    //  Post: You are back to main character while a partner character is in execution
    private void goBackToMain(Vector3 originalPos) {
        PKMNEntity prevFighter = selectedFighter;   //Set previous fighter

        //Enables new fighter
        selectedFighter = transform.GetChild(partyIndex).GetComponent<PKMNEntity>();
        selectedFighter.transform.position = originalPos;
        selectedFighter.GetComponent<SpriteRenderer>().enabled = true;
        selectedFighter.GetComponent<Collider2D>().enabled = true;

        //Switch UI elements
        secMoveIndex = 0;
        mainSwapper.switchAbilities(secMoveIndex);
        swapUIBars(selectedFighter, prevFighter);
        abilityUIMap[selectedFighter].swapIcons(abilityUIMap[prevFighter]);

        //Switches fighter mappings
        AbilityUI temp = abilityUIMap[selectedFighter];
        abilityUIMap[selectedFighter] = abilityUIMap[prevFighter];
        abilityUIMap[prevFighter] = temp;

        selectedFighter.GetComponent<SwitchState>().setUpState(abilityUIMap[selectedFighter]);
        prevFighter.GetComponent<SwitchState>().setUpState(abilityUIMap[prevFighter]);

        //Set Main Character variables to fighter
        animator = selectedFighter.GetComponent<Animator>();
        player = selectedFighter.GetComponent<Transform>();
    }

    //Private helper method for assist moves: Revert assist fighter back to normal state
    //  Pre: Assist fighter finished assist duration and control is given back to main fighter
    //  Post: Assist fighter reverted back to being an invisible partner
    private void revertAssist(int assistIndex, bool durationRan) {
        numPartners++;
        while (assistIndex >= numPartners)  //Adjust assistIndex just in case a main character died during assist
            assistIndex--;

        Transform assist = assistFighter.transform;
        assist.parent = transform;                                      //Insert character back in children array
        assistFighter.setAssist(false);

        //Move assist fighter back
        assist.position = player.position;
        assist.localPosition = Vector3.zero;
        assist.SetSiblingIndex(assistIndex);

        assistFighter.GetComponent<SpriteRenderer>().enabled = false;   //Disable character back
        assistFighter.GetComponent<Collider2D>().enabled = false;

        if (numPartners == 1) {                  //If assistFighter was the only fighter left, change selectedFighter to assistFighter
            StartCoroutine(assistToSelected());
        }else{
            if (assistIndex <= partyIndex)      //Realign partyIndex
                partyIndex += 1;

            if(durationRan) {
                assistFighter.GetComponent<SwitchState>().disableSwitch(false);    //Disable switch state on assistFighter
                abilityUIMap[assistFighter].updateStatus();
            }
        }
    }

    //Private helper method that allows a delay before having the assistFighter become the main selectedFighter
    //  Pre: assistFighter is currently the only party member that is alive
    IEnumerator assistToSelected() {
        PKMNEntity prevFighter = selectedFighter;
        selectedFighter = assistFighter;
        animator = selectedFighter.GetComponent<Animator>();
        player = selectedFighter.GetComponent<Transform>();

        yield return new WaitForSeconds(DEATH_WAIT);

        //Switch UI elements
        secMoveIndex = 0;
        mainSwapper.switchAbilities(secMoveIndex);
        swapUIBars(selectedFighter, prevFighter);
        abilityUIMap[selectedFighter].swapIcons(abilityUIMap[prevFighter]);

        //Switches fighter mappings
        AbilityUI temp = abilityUIMap[selectedFighter];
        abilityUIMap[selectedFighter] = abilityUIMap[prevFighter];
        abilityUIMap[prevFighter] = temp;

        //Update UI
        abilityUIMap[prevFighter].updateStatus();

        selectedFighter.GetComponent<SwitchState>().setUpState(abilityUIMap[selectedFighter]);
        prevFighter.GetComponent<SwitchState>().setUpState(abilityUIMap[prevFighter]);

        selectedFighter.GetComponent<SpriteRenderer>().enabled = true;   //Disable character back
        selectedFighter.GetComponent<Collider2D>().enabled = true;
        canMove = true;
    }

    //Experience gaining method for party
    //  Pre: an enemy must have died from a player attack
    //  Post: experience will be given to the party with the last hit fighter getting more experience than the party memebers
    private const float MAIN_BASE_EXP = 15f;
    private const float SIDE_BASE_EXP = 10f;
     
    public void gainExp(int deadEnemyLvl, PKMNEntity lastHitFighter) {
        StartCoroutine(expHelper(deadEnemyLvl, lastHitFighter));
    }

    //Private IEnumerator helper for Exp: Helps kessens the workload per frame
    private IEnumerator expHelper(int deadEnemyLvl, PKMNEntity lastHitFighter) {
        //Goes through the entire party: 1 party member per frame
        for(int i = 0; i < numPartners; i++) {
            PKMNEntity curFighter = transform.GetChild(i).GetComponent<PKMNEntity>();
            float curBaseExp = (curFighter == lastHitFighter) ? MAIN_BASE_EXP : SIDE_BASE_EXP;
            curFighter.gainExp(curBaseExp, deadEnemyLvl);
            abilityUIMap[curFighter].updateExp();
            yield return new WaitForEndOfFrame();
        }

        //Accounts for assist fighters
        if(assistFighter != null) {
            yield return new WaitForEndOfFrame();
            if (assistFighter == lastHitFighter)
                assistFighter.gainExp(MAIN_BASE_EXP, deadEnemyLvl);
            else
                assistFighter.gainExp(SIDE_BASE_EXP, deadEnemyLvl);

            abilityUIMap[assistFighter].updateExp();
        }
    }

    //Upon partner / teammate death, move to next partner or just end the game
    //  Pre: a partner must lose all of his health
    IEnumerator partnerDeath(bool assistStatus) {
        numPartners = (assistStatus) ? numPartners : numPartners - 1;

        if (numPartners <= 0 && (assistFighter == null || assistStatus)) { //CASE 1: Your death algorithm. The character that dies is the last character in the party
            Debug.Log("RIP! You lost");
            canMove = false;
        }else {
            if (numPartners <= 0 && assistFighter != null && !assistStatus)   //CASE 2: main character dies and the only one left is the assistFighter - Disable movement so assistFighter thread can deal with it
                canMove = false;
            else if(!assistStatus)                                            //DEFAULT CASE: main character dies and there's still party members within party
                yield return StartCoroutine(mainMemberDeathFreePartners());
        }
    }

    //Private helper method that switches characters if a main character is dead but partners (within rotation) are alive
    private IEnumerator mainMemberDeathFreePartners() {
        PKMNEntity prevFighter = selectedFighter;
        partyIndex %= numPartners;                  //Change partyIndex
        selectedFighter.transform.parent = null;    //Detaches parent

        //Enable new partner the GameObject. If a GameObject shares a parent with other GameObjects and are on the same level (i.e. they share the same direct parent), these GameObjects are known as siblings. The sibling index shows where each GameObject sits in this sibling hierarchy.

        selectedFighter = transform.GetChild(partyIndex).GetComponent<PKMNEntity>();
        animator = selectedFighter.GetComponent<Animator>();
        player = selectedFighter.GetComponent<Transform>();
        player.position = transform.position;

        canMove = false;
        yield return new WaitForSeconds(DEATH_WAIT);
        canMove = true;

        selectedFighter.GetComponent<SpriteRenderer>().enabled = true;
        selectedFighter.GetComponent<Collider2D>().enabled = true;

        //Switch UI elements
        secMoveIndex = 0;
        mainSwapper.switchAbilities(secMoveIndex);
        swapUIBars(selectedFighter, prevFighter);
        abilityUIMap[selectedFighter].swapIcons(abilityUIMap[prevFighter]);

        //Switches fighter mappings
        AbilityUI temp = abilityUIMap[selectedFighter];
        abilityUIMap[selectedFighter] = abilityUIMap[prevFighter];
        abilityUIMap[prevFighter] = temp;
        abilityUIMap[prevFighter].updateStatus();
        setIsolation(false);

        yield return StartCoroutine(recoveryFrames());
    }

    //Private helper method IENumerator for mainMemberDeath for recovery / invincibility frames
    //  Pre: Main member has died and the selected fighter has been switched to a living member
    //  Post: current member now has recovery frames where he will not be damaged during it
    private const float RECOVERY_DURATION = 1.25f;
    private const float RECOVERY_TRANS = 0.75f;

    private IEnumerator recoveryFrames() {

        SpriteRenderer fighterSprite = selectedFighter.GetComponent<SpriteRenderer>();  //Get reference variable

        //Change transparency of sprite
        Color temp = fighterSprite.color;
        temp.a = RECOVERY_TRANS;
        fighterSprite.color = temp;

        selectedFighter.tag = "PlayerRecovery"; //Change tag to disable damage

        yield return new WaitForSeconds(RECOVERY_DURATION);

        //Reset
        temp.a = 1;
        fighterSprite.color = temp;

        selectedFighter.tag = "Player";
    }

    //Updates cooldown display from the move itself
    private void updateCooldownDisplay(ISecMove move) {
        PKMNEntity fighter = move.getBasis();
        abilityUIMap[fighter].cooldownUpdate(move);
    }
}
