using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Key : MonoBehaviour {

    public AudioSource keyAudio;
    private const float AUDIO_DURATION = 0.75f;

    //When player touches key, he gains a key in his inventory
    void OnTriggerEnter2D(Collider2D collider) {
        if(Battle.isPlayer(collider)) {
            collider.transform.parent.GetComponent<KeyInventory>().gainKey();
            StartCoroutine(keyGained());
        }
    }

    //Plays when key is gained
    IEnumerator keyGained() {
        GetComponent<Collider2D>().enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;
        keyAudio.Play();
        yield return new WaitForSeconds(AUDIO_DURATION);
        Object.Destroy(gameObject);
    }
}
