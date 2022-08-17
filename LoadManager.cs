using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadManager : MonoBehaviour
{
    public static float autosaveInterval = 300;

    private Main main;
    private int firstLaunch;
    private void Start() {
        main = GetComponent<Main>();

        // play intro animation

        firstLaunch = PlayerPrefs.HasKey("firstLaunch") ? PlayerPrefs.GetInt("firstLaunch") : 0;

        if (firstLaunch == 0) {
            // first launch ever

            // play tutorial
            GetComponent<Tutorial>().StartTutorial(main);
        }
        else if (firstLaunch == 2) {
            // replaying tutorial

            GetComponent<Tutorial>().StartTutorial(main);

            PlayerPrefs.SetInt("firstLaunch", 3);
        }
        else if (firstLaunch == 3) {
            Save.LoadData("tutorialBackup");
            Debug.Log("Tutorial backup loaded");
            PlayerPrefs.SetInt("firstLaunch", 1);
            StartCoroutine(SaveWithDelay(2f));
            StartCoroutine(Autosave());
        }
        else {
            // normal launch

            // load game from autosave
            Save.LoadData("autosave");
        }

        StartCoroutine(Autosave());
    }

    private IEnumerator SaveWithDelay(float seconds) {
        yield return new WaitForSeconds(seconds);
        main.SaveGame();
        Debug.Log("Game saved to autosave");
    }

    private IEnumerator Autosave() {
        while (true) {
            yield return new WaitForSeconds(autosaveInterval);
            main.SaveGame();
            Debug.Log("Autosaving...");
        }
    }

    public void UpdateAutosave(float newInterval) {
        StopAllCoroutines();
        autosaveInterval = newInterval * 60;
        StartCoroutine(Autosave());
    }

    public static void ReloadGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
