using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeyInventory : MonoBehaviour
{
    private int keys;
    public Text keyText;

    //Used to gain key
    public void gainKey() {
        keys++;
        keyText.text = "Keys: " + keys;
    }

    //Method to use up keys. If numKeys > keys, just use up all keys available
    //  Pre: numKeysReq > 0
    //  Post: Returns the amount of keys used
    public int useKeys(int numKeysReq) {
        int keysUsed;

        if(numKeysReq > keys) {
            keysUsed = keys;
            keys = 0;
        }else{
            keysUsed = numKeysReq;
            keys -= numKeysReq;
        }
        
        keyText.text = "Keys: " + keys;
        return keysUsed;
    }

    //Accessor method for keys
    public int getNumKeysObtained() {
        return keys;
    }

}
