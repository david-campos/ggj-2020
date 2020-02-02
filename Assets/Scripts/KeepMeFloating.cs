using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeepMeFloating : MonoBehaviour
{
    public GameObject endGameScreen;

    void Update() {
        // -15 looks like a fair position to say you have sunk
        if (transform.position.y < -15) {
            endGameScreen.SetActive(true);
            TimeCounter.GetInstance().StopCounting();
            Time.timeScale = 0;
        }
    }
}
