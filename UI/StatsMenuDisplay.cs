using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatsMenuDisplay : MonoBehaviour {
    //Fighter information
    private List<PKMNEntity> fighters;      //List of fighter to choose from to display
    public Button[] fighterButtons;         //List of fighter buttons that switch fighters
    private int fighterIndex;
    private int availableUpgrades;

    //Stat UI information
    public Image health;
    public Text healthText;
    public Image armor;
    public Text armorText;
    public Text levelDisplay;
    public Image exp;
    public Image numOpenBoosts;
    public Text openBoostsText;
    public SingleBaseStat[] baseStatDisplays;

    //Constant colors for stat boosts
    private Color upgradeReady;
    private Color noUpgrade;

    //Set color variables
    void Awake() {
        upgradeReady = new Color(0f, 0.9175f, 0.9607f);
        noUpgrade = new Color(0.4622f, 0.4622f, 0.4622f);
        fighterIndex = 0;
    }

    private const float SWITCH_DELAY = 0.2f;

    //Called to allow leaving menu through button shortcut
    private IEnumerator pauseGame() {
        yield return new WaitForSecondsRealtime(SWITCH_DELAY);
        Time.timeScale = 0f;

        while (!Input.GetKeyDown("left shift")) {
            yield return 0;

            int leftIndex = (fighterIndex - 1 + fighters.Count) % fighters.Count;
            int rightIndex = (fighterIndex + 1) % fighters.Count;

            if (Input.GetKey("q") && leftIndex != fighterIndex) {
                displayFighterInfo(leftIndex);
                yield return new WaitForSecondsRealtime(SWITCH_DELAY);
            }else if (Input.GetKey("e") && rightIndex != fighterIndex) {
                displayFighterInfo(rightIndex);
                yield return new WaitForSecondsRealtime(SWITCH_DELAY);
            }
        }

        close();
    }

    //Adds fighters to the menu display
    //  ONLY used in the beginning when loading information
    //  ALL BUTTONS MUST BE DISABLED BEFORE THIS RUNS
    public void addFighter(PKMNEntity fighter) {
        if (fighters == null)
            fighters = new List<PKMNEntity>();

        fighters.Add(fighter);
        Button curButton = fighterButtons[fighters.Count - 1];
        curButton.gameObject.SetActive(true);
        curButton.GetComponentInChildren<Text>().text = fighter.fighterName;

    }

    //Method for opening menu and pausing game
    public void open() {
        fighterIndex = 0;
        gameObject.SetActive(true);
        AudioListener.volume = 0f;
        fighters[0].getController().canMove = false;
        displayFighterInfo(0);
        fighterButtons[0].interactable = false;
        StartCoroutine(pauseGame());

        for(int i = 1; i < fighterButtons.Length; i++)
            fighterButtons[i].interactable = true;
    }

    //Method for closing menu and resuming game
    public void close() {
        fighters[0].getController().canMove = true;
        gameObject.SetActive(false);
        AudioListener.volume = 1f;
        Time.timeScale = 1f;
    }

    //Accessor method for the current fighter whose information is displayed
    public PKMNEntity getFighter() {
        return fighters[fighterIndex];
    }

    //Displays ALL fighter information and sets menu up
    public void displayFighterInfo(int newIndex) {
        //Get Fighter
        int prevIndex = fighterIndex;
        fighterIndex = newIndex;
        PKMNEntity fighter = fighters[fighterIndex];

        //Update ALL Info
        health.fillAmount = fighter.healthBar.fillAmount;
        float curHealth = fighter.accessStat(BaseStat.HEALTH);
        curHealth = (curHealth <= 0) ? 0 : (curHealth < 1) ? 1 : curHealth;
        healthText.text = (int)curHealth + "/" + fighter.accessBaseStat(BaseStat.HEALTH);

        armor.fillAmount = fighter.armorBar.fillAmount;
        armorText.text = fighter.armorToString();

        levelDisplay.text = "Level: " + fighter.level;
        exp.fillAmount = fighter.getPercentToLvL();
        availableUpgrades = fighter.getNumStatBoosts();
        openBoostsText.text = "" + availableUpgrades;

        numOpenBoosts.color = (availableUpgrades > 0) ? upgradeReady : noUpgrade;

        //Update base stats
        foreach(SingleBaseStat baseStat in baseStatDisplays) {
            baseStat.displayFighterInfo(fighter);

            if (availableUpgrades > 0)
                baseStat.displayPotentialUpgrade();
            else
                baseStat.disablePotentialUpgrade();
        }

        //Update buttons
        fighterButtons[fighterIndex].interactable = false;
        fighterButtons[prevIndex].interactable = true;
    }

    //Updates information upon upgrade
    //  Pre: upgraded stat is the stat that has been upgraded previously
    public void uponUpgrade(SingleBaseStat upgradedStat) {
        availableUpgrades--;
        openBoostsText.text = "" + availableUpgrades;
        armor.fillAmount = fighters[fighterIndex].armorBar.fillAmount;
        armorText.text = fighters[fighterIndex].armorToString();

        if (availableUpgrades > 0) {
            upgradedStat.displayPotentialUpgrade();                     //Updates the potential upgrade if there are still upgrades available
        } else {
            numOpenBoosts.color = noUpgrade;

            foreach (SingleBaseStat baseStat in baseStatDisplays)       //Disable all potential upgrades when no upgrades available
                baseStat.disablePotentialUpgrade();
        }
    }
}
