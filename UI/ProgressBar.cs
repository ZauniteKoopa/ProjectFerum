using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour {
    public Image progress;

    //Updates the progress made on the UI
    //  Pre: percentDone must be between 0 and 1
    public void updateProgress(float percentDone) {
        progress.fillAmount = percentDone;
    }
}
