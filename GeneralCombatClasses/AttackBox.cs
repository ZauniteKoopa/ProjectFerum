using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackBox : MonoBehaviour
{
    public int priority;
    public string enemyTag;
    public string enemyAttackTag;
    public IMove currentMove;
}
