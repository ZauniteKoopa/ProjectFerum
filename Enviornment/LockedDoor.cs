using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockedDoor : Interactable{
    
    public GameObject door;
    public bool keyholed;
    public AudioSource unlockSound;
    private const float SOUND_DURATION = 0.85f;

    //Upon collision with a sensor, lock all doors
    void OnTriggerEnter2D(Collider2D collider) {

        bool isPlayer = Battle.isPlayer(collider);

        if(isPlayer && !activated)
            activate();
        else if(isPlayer && keyholed) {
            int keysUsed = collider.transform.parent.GetComponent<KeyInventory>().useKeys(keysReq);
            unlock(keysUsed);
            unlockSound.Play();
        }
    }

    //Overriden activate method: Locks every door upon activating sensor
    public override void activate() {
        activated = true;
        door.SetActive(true);
    }

    //Overidden deactivation method: Destoys every unlock door and then destroys sensor (destroying doors to be changed to a method for doors)
    public override void deactivate() {
        StartCoroutine(destroyDoor());
    }

    //Unlock door IEnumerator
    IEnumerator destroyDoor() {
        unlockSound.Play();
        yield return new WaitForSeconds(SOUND_DURATION);
        Object.Destroy(door);
        Object.Destroy(gameObject);
    }
}
