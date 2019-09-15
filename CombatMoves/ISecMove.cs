using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISecMove : IMove {

    //AssistMove execution: run upon assist
    //  Pre: player wanted to execute an assist move from a partner. (Held C/D and confirmed move)
    //  Post: move is executed 
    IEnumerator assistExecute();

    //Method that reads cooldown progress for UI
    //  Post: returns a decimal representing how much time left the cooldown has (0 = 0% of CD, 1 = 100% of CD)
    float getCDProgress();
}
