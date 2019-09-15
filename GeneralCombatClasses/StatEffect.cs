using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//StatEffect: An object that resembles a stat buff / debuff that lasts for a specified duration
public class StatEffect {
    //Private Instance Variables
    private float duration;             // Amount of time the stat is supposed to last
    private float[] statFactor;         //Factor a stat is buffed or reduced (0.5 means you reduce the stat by half), corresponds with stat effected
    private int[] statsEffected;        //List of stats effected

    //Constructor 1: Given an array of stats
    public StatEffect(float duration, float[] statFactor, int[] statsEffected) {
        if(statFactor.Length != statsEffected.Length)
            throw new System.ArgumentException("Error: factors and stats effected don't correspond");

        this.duration = duration;
        this.statFactor = statFactor;
        this.statsEffected = statsEffected;
    }

    //Constructor 2: Given only 1 stat
    public StatEffect(float duration, float statFactor, int stat) {
        this.duration = duration;

        statsEffected = new int[1];
        this.statFactor = new float[1];
        this.statFactor[0] = statFactor;
        statsEffected[0] = stat;
    }

    //Constructor 3: Given 2 stats
    public StatEffect(float duration, float statFactor, int stat1, int stat2) {
        this.duration = duration;
        this.statFactor = new float[2];
        for(int i = 0; i < 2; i++)
            this.statFactor[i] = statFactor;

        statsEffected = new int[2];
        statsEffected[0] = stat1;
        statsEffected[1] = stat2;
    }

    //Adjusts duration according to the SE_PriorityQueue so that it sychronizes with the main timer for stat Effects
    //  Pre: qTimer >= 0
    //  Post: duration is adjusted to fit with the PKMNEntity's SE_PriorityQueue
    public void adjustDuration(float qTimer) {
        duration += qTimer;
    }

    //Accessor method for duration
    public float getDuration() {
        return duration;
    }

    //Method to check if this is a total debuff (sidebuffs are not considered debuffs)
    public bool isDebuff() {
        for(int i = 0; i < statFactor.Length; i++)
            if(statFactor[i] >= 1.0f && statsEffected[i] < BaseStat.BASE_STATS_LENGTH)
                return false;

        return true;
    }

    //Applies stat effect to effected entity
    //  Pre: PKMNEntity Effected is not null
    //  Post: PKMNEntity stats are changed in accordance with statsEffected
    public void applyEffect(PKMNEntity effected) {
        effected.addStatQ(this);

        for(int i = 0; i < statsEffected.Length; i++) {
            int curStat = statsEffected[i];

            if(curStat < BaseStat.BASE_STATS_LENGTH)
                effected.changeStat(statFactor[i], statsEffected[i]);
            else
                effected.poisonUnit(statFactor[i]);
        }
    }

    //Reverse the effect of this StatEffect 
    //  Pre: PKMNEntity effected is not null and is active in the game
    //  Post: Reverses all of the effects using the reciprocal of the factor
    public void reverseEffect(PKMNEntity effected) {

        for(int i = 0; i < statsEffected.Length; i++) {
            int curStat = statsEffected[i];

            if(curStat < BaseStat.BASE_STATS_LENGTH)
                effected.changeStat(1 / statFactor[i], statsEffected[i]);
            else
                effected.poisonUnit(-1 * statFactor[i]);
        }
    }
}
