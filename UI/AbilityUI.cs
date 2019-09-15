using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbilityUI : MonoBehaviour
{
    public Image[] abilityIcons;    //References to ability icons

    //Fighter variables
    private PKMNEntity fighter;
    private Dictionary<IMove, Image> iconMapping;
    private const int MAX_SEC_ABILITIES = 3;

    //Color UI Variables
    private Color defaultColor;
    private Color cdSwitchColor;
    private Color deathColor;
    private Color assistColor;

    //Experience management
    public Image expBar;
    public GameObject levelUp;

    //Icon variables
    public Image fighterIcon;

    //Sets up dictionary and cooldown variables
    void Awake() {
        iconMapping = new Dictionary<IMove, Image>();

        //Sets colors
        defaultColor = new Color(0.2547f, 0.2451f, 0.2451f);
        cdSwitchColor = new Color(0.0518f, 0.1448f, 0.5f);
        deathColor = new Color(0.6132f, 0.3042f, 0.2545f, 0.66f);
    }

    //Update method that checks the status of fighter and make changes to correspond with status (Particularly with switch cooldown)
    public void updateStatus() {
        SwitchState thisState = fighter.GetComponent<SwitchState>();
        GetComponent<Image>().color = (thisState.canSwitch()) ? defaultColor : cdSwitchColor;

        if (!fighter.isAlive()) {
            GetComponent<Image>().color = deathColor;
            fighterIcon.sprite = Resources.Load<Sprite>("Portraits/Death");
            fighterIcon.color = deathColor;
        }

        updateExp();
    }

    //Update method for switch status
    public void setIsolation(bool isolated) {
        GetComponent<Image>().color = (isolated) ? cdSwitchColor : defaultColor;
    }

    //Update method for experience
    public void updateExp() {
        if (fighter.getNumStatBoosts() > 0) {
            levelUp.SetActive(true);
            expBar.fillAmount = 1f;
        } else {
            expBar.fillAmount = fighter.getPercentToLvL();
            levelUp.SetActive(false);
        }
    }

    //Sets up ability UI for a fighter
    public void setUp(PKMNEntity newFighter) {
        iconMapping.Clear();
        fighter = newFighter;
        List<IMove> fighterMoves = fighter.getMoves();

        //Setting fighter icon
        if(fighterIcon != null)
            fighterIcon.sprite = Resources.Load<Sprite>("Portraits/" + fighter.fighterName);

        int curIconIndex = 0;
        int numSecMoves = newFighter.getNumSecMoves();

        //Enabling ability icons
        while (curIconIndex < numSecMoves) {
            ISecMove curMove = (ISecMove)fighterMoves[curIconIndex];
            Image curIcon = abilityIcons[curIconIndex];

            curIcon.gameObject.SetActive(true);
            iconMapping[curMove] = curIcon;
            cooldownUpdate(curMove);

            curIconIndex++;
        }

        //Disabling redundant ability icons
        while (curIconIndex < MAX_SEC_ABILITIES) {
            abilityIcons[curIconIndex].gameObject.SetActive(false);
            curIconIndex++;
        }

        //Settings for exp bar
        if (fighter.getNumStatBoosts() > 0){
            levelUp.SetActive(true);
            expBar.fillAmount = 1f;
        }else{
            expBar.fillAmount = fighter.getPercentToLvL();
            levelUp.SetActive(false);
        }

    }

    //Accessor method for fighter
    public PKMNEntity getFighter() {
        return fighter;
    }

    //Mutator methods for color status
    public void setToDefault() {
        GetComponent<Image>().color = defaultColor;
    }

    //Swapping Method for UI Ability elements
    public void swapIcons(AbilityUI other) {
        PKMNEntity temp = other.getFighter();
        other.setUp(fighter);
        this.setUp(temp);
    }

    //Event based function - Disabling move
    //  Changes color of icon purple
    public void cooldownUpdate(ISecMove move) {
        Image curIcon = iconMapping[move].transform.GetChild(0).GetComponent<Image>();
        float cooldown = move.getCDProgress();
        curIcon.fillAmount = cooldown;
        iconMapping[move].color = (cooldown <= 0) ? Color.yellow : new Color(0.4f, 0.4f, 0f);
    }
}
