using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SE_PriorityQueue {
    //Private instance variables
    private float timer;                        //Timer to keep track of everything
    private PKMNEntity entity;                  //Reference pointer to entity that queue belongs to
    private LinkedList<StatEffect> qStruct;     //Data Structure that holds the elements in the priority queue

    //Poison variables
    private float poisonIntensity;
    private const float BADLY_POISONED_REQ = 1f;
    private float pTimer;
    private const float POISON_TICK = 1.2f;
    private const int BASIS_POISON_PERCENT = 16; //(1/16)
    private const int BASIS_BADLY_POISON_PERCENT = 8; //(1/8)
    private int curPoisonDamage;


    //Constructor
    public SE_PriorityQueue(PKMNEntity entity) {
        this.entity = entity;
        timer = 0.0f;
        pTimer = 0f;
        qStruct = new LinkedList<StatEffect>();
        curPoisonDamage = BASIS_POISON_PERCENT;
    }

    //Method that updates timer.
    //  Pre: Called within a looping function (PKMNEntity's Update)
    //  Post: Updates timer IF there are any elements present in the queue and checks it with
    //        the front most element
    public void update() {
        if(qStruct.Count > 0) {
            float delta = Time.deltaTime;
            timer += delta;       //Update Timer

            //Poison method
            if(poisonIntensity > 0f) {
                pTimer += delta;

                if(pTimer > POISON_TICK) {
                    pTimer = 0f;
                    int damage = entity.accessBaseStat(BaseStat.HEALTH) / curPoisonDamage;
                    entity.StartCoroutine(entity.receiveDoT(damage, null));
                }
            }

            //If timer exceeds the current duration, remove and reverse the effect
            if(timer >= qStruct.First.Value.getDuration()) {
                StatEffect finishedSE = this.remove();
                finishedSE.reverseEffect(this.entity);
            }
        }
    }

    //Method that adds elements to the proirity queue while maintaining order of smallest duration to biggest duration
    //  Pre: effect.getDuration > 0
    //  Post: A new effect has been added to the priority queue
    public void add(StatEffect effect) {
        Debug.Log("StatEffect Added!");
        effect.adjustDuration(timer);       //Adjust the duration of the stat effect to align with the current timer

        if(qStruct.Count == 0) {
            qStruct.AddFirst(effect);       //If it's empty, just add the element
        }else{
            //Iterate through the linked list until you find an effect whose duration is greater than or equal to the one being inserted OR reach end of list
            LinkedListNode<StatEffect> curNode = qStruct.Last;
            while (curNode != null && curNode.Value.getDuration() > effect.getDuration())
                curNode = curNode.Previous;

            //2 Cases: If at end of list, just add last. Else, add before curNode
            if (curNode != null)
                qStruct.AddAfter(curNode, effect);
            else
                qStruct.AddFirst(effect);
        }
    }

    //Method that removes the first element in the queue
    //  Pre: qStruct.count > 0
    //  Post: First element in the queue has been removed
    private StatEffect remove() {
        StatEffect result = qStruct.First.Value;
        qStruct.RemoveFirst();

        //If there are no elements in the list anymore, reset timer.
        if (qStruct.Count == 0)
            timer = 0.0f;

        Debug.Log("StatEffect Timed Out!");
        return result;
    }

    //Reset Priority Queue and reverses all stat effects
    public void clear() {
        //clears and reverses everything
        while(qStruct.Count != 0) {
            StatEffect finishedSE = this.remove();
            finishedSE.reverseEffect(this.entity);
        }

        timer = 0.0f;
    }

    //Allows user to edit poison intensity
    public void editPoisonIntensity(float intensity) {
        this.poisonIntensity += intensity;
        curPoisonDamage = (poisonIntensity >= BADLY_POISONED_REQ) ? BASIS_BADLY_POISON_PERCENT : BASIS_POISON_PERCENT;
    }
}
