using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchState : MonoBehaviour
{
    private AbilityUI UIState;  //Access to ability UI

    //Switch cooldown variables
    private float curCooldown;
    private const float ASSIST_MOVE_COOLDOWN = 3.5f;
    private const float SWITCH_COOLDOWN = 8.5f;
    private float cTimer;
    private bool offCD;

    // Start is called before the first frame update
    void Awake() {
        offCD = true;
        cTimer = 0f;
        curCooldown = SWITCH_COOLDOWN;
    }

    //Set up method to access UIState
    public void setUpState(AbilityUI state) {
        UIState = state;
    }

    // Update is called once per frame
    void FixedUpdate() {
        if (!offCD) {
            cTimer += Time.deltaTime;

            //Reset timer
            if (cTimer >= curCooldown) {
                offCD = true;
                cTimer = 0f;
                UIState.setToDefault();
            }
        }
    }

    //Accessor method to check if you can switch with a character
    public bool canSwitch() {
        return offCD;
    }

    //Mutator method that changes offCD to false upon switching character
    public void disableSwitch(bool fullSwitch) {
        curCooldown = (fullSwitch) ? SWITCH_COOLDOWN : ASSIST_MOVE_COOLDOWN;
        offCD = false;
    }
}
