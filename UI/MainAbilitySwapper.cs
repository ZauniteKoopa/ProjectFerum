using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainAbilitySwapper : MonoBehaviour {

    public GameObject[] abilitySelected;    //Array of ability borders that illustrates selections
    private int curAbilityIndex;            //Current ability index
    public AudioSource swapSound;

    //Set current ability index to 0 at start
    void Start() {
        curAbilityIndex = 0;
        abilitySelected[0].SetActive(true);
    }

    //switches Abilities in the UI
    //  Pre: newIndex in equal to the controller AND 0 <= newIndex < abilitySelected.Count
    public void switchAbilities(int newIndex) {
        abilitySelected[curAbilityIndex].SetActive(false);
        curAbilityIndex = newIndex;
        abilitySelected[curAbilityIndex].SetActive(true);
        swapSound.Stop();
        swapSound.Play();
    }
}
