using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SingleBaseStat : MonoBehaviour {
    public int baseStat;                    //Indicator of which baseStat
    public StatsMenuDisplay fighterInfo;    //Pointer pointing to the big info menu

    //Variables for filling the bars
    private const float MAX_STAT = 100f;
    public Image statBar;
    public Image updateBar;

    //For the Update button
    private int numStatBoosts;
    public Text statBoostDisplay;
    public Button updateButton;
    private int potentialStat;
    private const int MAX_UPGRADES = 6;

    //At start, set potentialStat to zero to indicate there isn't one set yet
    void Awake() {
        potentialStat = 0;
    }

    //Displays fighter info onto single base stat display
    //  Pre: fighter must be alive and not null with specified baseStat
    //  Post: Displays fighter's boosts used on stat and base stat value
    public void displayFighterInfo(PKMNEntity fighter) {
        numStatBoosts = fighter.getStatBoostsUsed(baseStat);
        statBar.fillAmount = fighter.accessBaseStat(baseStat) / MAX_STAT;
        statBoostDisplay.text = numStatBoosts + "/" + MAX_UPGRADES;
    }

    //Update potential stat upon event that player opens menu and numOpenStatBoosts > 0
    //  ONLY updates potentialStat if potentialStat == 0 (last potential stat was already used)
    public void displayPotentialUpgrade() {
        if (numStatBoosts < 6){
            //Obtain potential stat through a growth formula
            potentialStat = fighterInfo.getFighter().accessBaseStat(baseStat);
            potentialStat = BaseStat.statUpgradeCalc(numStatBoosts, potentialStat);

            //Set update bar to appropriate info
            updateBar.fillAmount = (float)potentialStat / MAX_STAT;

            //Unables upgrade button and update bar
            updateButton.interactable = true;
            updateBar.enabled = true;
        }else
            disablePotentialUpgrade();
    }

    //Disables upgraded stats
    public void disablePotentialUpgrade() {
        updateButton.interactable = false;
        updateBar.enabled = false;
    }

    //Upgrades stat upon button press
    public void upgradeStat() {
        numStatBoosts += 1;
        PKMNEntity linkedFighter = fighterInfo.getFighter();
        linkedFighter.useStatBoost(baseStat, potentialStat);    //Upgrades stat

        //Updates display
        statBar.fillAmount = linkedFighter.accessBaseStat(baseStat) / MAX_STAT;
        statBoostDisplay.text = linkedFighter.getStatBoostsUsed(baseStat) + "/" + MAX_UPGRADES;

        //Indicate to main menu that an upgrade has happened
        fighterInfo.uponUpgrade(this);
    }
}