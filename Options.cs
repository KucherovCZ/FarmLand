using UnityEngine;
using UnityEngine.UI;

public class Options : MonoBehaviour {

    /*
     * 
     *      TODO - save options to PlayerPrefs and load them on next run
     * 
     * 
     */

    Main main;
    LoadManager loadManager;

    InputField autosaveInterval;

    private void Start() {
        main = GetComponent<Main>();
        loadManager = GetComponent<LoadManager>();

        autosaveInterval = main.canvas.Find("OptionsMenu/Autosave/InputField").GetComponent<InputField>();
    }

    public void UpdateNotifications() { 
        // no idea
    }

    public void UpdateSounds() { 
        // change all audio sources with "Sound" tag
    }

    public void UpdateMusic() { 
        // change all audio sources with "Music" tag
    }

    public void ChangeAutosaveInterval() {
        int newInterval = int.Parse(autosaveInterval.text);

        if (newInterval > 60) newInterval = 60;
        if (newInterval < 3) newInterval = 3;
        autosaveInterval.text = newInterval.ToString();

        loadManager.UpdateAutosave((float)newInterval);
    }

    public void ReplayTutorial() {
        PlayerPrefs.SetInt("firstLaunch", 2);
        GetComponent<LoadManager>().StopAllCoroutines();
        main.SaveGame("tutorialBackup");
        Debug.Log("Game saved to tutorial backup");
        main.LoadGame();
    }
}
