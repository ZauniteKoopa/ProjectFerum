using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Interactable : MonoBehaviour {
    public bool activated;      //Boolean locking variable for activation
    public int keysReq;

    //Activation method: activates the method (if any activation)
    public abstract void activate();

    //Unlock method: upon bringing a key or killing an enemy, remove a lock.
    public void unlock(int numKeys) {
        keysReq -= numKeys;

        if(keysReq == 0)
            deactivate();
    }

    //Deactivate method: when all keys unlocked (keys == 0), have the interactable deactivate
    public abstract void deactivate();
}
