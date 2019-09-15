using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BaseStat {
    //Constants for base stat index in base stats array
    public const int HEALTH = 0;  //MUST BE 0
    public const int ATTACK = 1;
    public const int DEFENSE = 2;
    public const int SPECIAL_ATTACK = 3;
    public const int SPECIAL_DEFENSE = 4;
    public const int SPEED = 5;
    public const int BASE_STATS_LENGTH = 6;

    //Stat effect index
    public const int POISON_INDEX = 6;

    //Constants for type IDs
    public const int NORMAL = 0;
    public const int GRASS = 1;
    public const int PSYCHIC = 2;
    public const int WATER = 3;


    //Consts for movement speed calculations
    private const float MIN_BASE_MOVE = 0.0375f;      //Minimum movement speed a pokemon can go
    private const float MAX_BASE_MOVE = 0.1525f;      //Maximum movement speed a pokemon can go
    private const float BASE_SPEED_CAP = 150f;      //Capped speed

    //Calculates movement speed for species. Movement speed is a linear equation
    //  Pre: name is found within inventory
    //  Post: Returns movement speed for a species (name)
    public static float movementSpeedCalc(int speed) {
        float curSpeed = (float)speed;

        if (curSpeed >= BASE_SPEED_CAP)
            return MAX_BASE_MOVE;

        return MIN_BASE_MOVE + (MAX_BASE_MOVE - MIN_BASE_MOVE) * (curSpeed / BASE_SPEED_CAP);
    }

    //Move Creator Inventory
    //  Pre: String must be found within move inventory. sourceTag is either "Player" or "Enemy". None of the parameters are null
    //  Post: Returns a new IMove for a character
    public static IMove moveInv(string moveName, Animator anim, PKMNEntity source, ProgressBar progress) {
        if (moveName == null || anim == null || source == null)
            throw new System.ArgumentException("Error: Null Parameter found for moveInv");

        switch (moveName) {
            case "Pound":
                return new Pound(anim, source);
            case "BulletSeed":
                return new BulletSeed(anim, source);
            case "Agility":
                return new Agility(anim, source, progress);
            case "QuickAttack":
                return new QuickAttack(anim, source);
            case "WaterPulse":
                return new WaterPulse(anim, source);
            case "ShellDash":
                return new ShellDash(anim, source, progress);
            case "GattlerBlast":
                return new GattlerBlast(anim, source);
            case "StormZone":
                return new ZoneControl(anim, source, BaseStat.SPEED, 0.4f);
            case "AnchorSlash":
                return new AnchorSlash(anim, source);
            case "DredgeLine":
                return new DredgeLine(anim, source);
            case "GunkShot":
                return new GunkShot(anim, source);
            case "null":
                return null;
            default:
                throw new System.ArgumentException("Error: " + moveName + " not found in moveInv");
        }
    }

    //Calculations for stat upgrades
    //  Pre: 0 <= statBoostsUsed < 6 and baseStat > 0
    public static int statUpgradeCalc(int numStatBoosts, int baseStat) {
        float statIncrease = 0.24f - numStatBoosts * 0.04f;
        return (int)(baseStat + baseStat * statIncrease);
    }

    //Calculations for Experience gain
    public static float expGainCalc(float baseExp, int playerLvl, int enemyLvl) {
        float lvlFactor = (float)(enemyLvl + 4);
        lvlFactor /= (float)(playerLvl + 4);
        return baseExp * lvlFactor;
    }

    //Calculations for health gain upon level up
    public static int healthGrowth(int numHealthBoosts, int curBaseHealth) {
        float statIncrease = 0.2f - numHealthBoosts * 0.02f;
        return (int)(curBaseHealth + curBaseHealth * statIncrease);
    }
}