using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeepMeFloating : MonoBehaviour
{
    public GameObject endGameScreen;

    void Update() {
        // -15 looks like a fair position to say you have sunk,
        // but we wait some frames before showing the game over screen
        if (transform.position.y < -15 && TimeCounter.GetInstance().IsCounting) {
            TimeCounter.GetInstance().StopCounting();
            StartCoroutine(ShowGameOver());
        }
    }

    IEnumerator ShowGameOver() {
        yield return new WaitForSeconds(1);
        Time.timeScale = 0;
        endGameScreen.SetActive(true);
    }
}