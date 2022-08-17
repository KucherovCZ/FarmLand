using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/*   LIST OF ALL ACHIEVEMENTS
 *   NUMBER     NAME                       TASK
 *   0    XX      Land owner                 Unlock all islands
 *   1    XX      Biologist                  Unlock all crops
 *   2    XX      Architect                  Unlock all buildings
 *   
 *   3    XX      Kolchoz                    Have 100 fields
 *   
 *   4    XX      Casual gamer               Play for 1 hour
 *   5    XX      Chad gamer                 Play for 24 hours
 *   6    XX      Get a life                 Play for 168 hours
 *   
 *   7    X      Supporter                  Watch X ads (provides permanent % boost on crops)
 *   
 *   8    XX      Michael Geese              Make 1M research / s                                                       <-  ??? might change values later (so its not impossible)
 *   9    XX      Elon Duck                  Make 1T research / s                                                       <-  ??? might change values later (so its not impossible)
 *   
 *   10   XX      Local Farmer               Reach 100K money
 *   11   XX      State Farmer               Reach 1B money
 *   12   XX      Global Farmer              Reach 1QT money
 *   
 *   13   XX      Is that all?               Finish the game
 */

public class Achievements : MonoBehaviour {

    Main main;

    // UI
    public Transform achievementUI;
    private Transform content;
    private Text totalAchivLabel;

    int totalAchievementsUnlocked = 0;

    private static string[] achievements = {
        "Land owner",
        "Biologist",
        "Architect",
        "Kolchoz",
        "Casual gamer",
        "Chad gamer",
        "Get a life",
        "Supporter",
        "Michael Geese",
        "Elon Duck",
        "Local Farmer",
        "State Farmer",
        "Global Farmer",
        "Is that all?" };

    private static bool[] achievementsUnlocked = new bool[achievements.Length];

    public void Start() {
        main = GetComponent<Main>();

        content = achievementUI.Find("Content");
        totalAchivLabel = achievementUI.Find("Total").GetComponent<Text>();

        LoadAchievements();

        // add all event connections
        if(!achievementsUnlocked[12]) Manager.moneyChanged += MoneyChanged;
        if(!achievementsUnlocked[9]) TileInfo.ResRateChanged += ResRateChanged;
        if (!achievementsUnlocked[3]) {
            Build.fieldPlaced += FieldPlaced;
            Build.fieldRemoved += FieldRemoved;
        }
        if(!achievementsUnlocked[1] || !achievementsUnlocked[2]) Build.itemResearched += ItemResearched;
        // if (!achievementsUnlocked[7])  TODO - Add AdViewed method to event (create the event in AdController.cs
    }

    public void OnApplicationQuit() {
        SaveAchievements();
    }

    private int counter = 0;
    public void FixedUpdate() {
        if (!achievementsUnlocked[6]) {
            counter++;
            if (counter > 49) {
                counter = 0;
                CheckTime(Time.unscaledTime);
            }
        }
    }

    #region AchievementHandling

    // draw achievement UI

    // take care of saving/loading achievements from PlayerPrefs

    public void SaveAchievements() {

        for (int i = 0; i < achievements.Length; i++) {
            PlayerPrefs.SetInt(i.ToString(), achievementsUnlocked[i] ? 1 : 0);
        }

        // save all custom needs
        PlayerPrefs.SetInt("FieldCount", fieldCount);

        PlayerPrefs.SetFloat("TotalTime", totalTime + (Time.unscaledTime / 3600));

        PlayerPrefs.SetInt("AdViews", adCount);
    }

    public void LoadAchievements() {
        for (int i = 0; i < achievements.Length; i++) {
            if (PlayerPrefs.HasKey(i.ToString())) {
                if (PlayerPrefs.GetInt(i.ToString()) == 1 && !achievementsUnlocked[i]) UnlockAchievement(i);
            }
        }

        // load all custom needs
        fieldCount = PlayerPrefs.HasKey("FieldCount") ? PlayerPrefs.GetInt("FieldCount") : 0;

        totalTime = PlayerPrefs.HasKey("TotalTime") ? PlayerPrefs.GetFloat("TotalTime") : 0f;

        adCount = PlayerPrefs.HasKey("AdViews") ? PlayerPrefs.GetInt("AdViews") : 0;
    }
    
    public void UnlockAchievement(int achievNumber) {
        achievementsUnlocked[achievNumber] = true;

        //find gameObject "Achievement" + achievNumber.ToString()
        Transform achievement = content.Find("Achievement" + achievNumber.ToString());
        achievement.Find("Cover").gameObject.SetActive(false);
        achievement.GetComponent<Image>().color = new Color(0.38f, 1f, 0.2f);

        totalAchievementsUnlocked++;
        totalAchivLabel.text = totalAchievementsUnlocked.ToString() + "/" + achievements.Length.ToString();
    }

    // DEBUG PURPOSE ONLY
    public void ResetAchievements() {
        for (int i = 0; i < achievements.Length; i++) {
            PlayerPrefs.SetInt(i.ToString(), 0);
            achievementsUnlocked[i] = false;
        }

        SaveAchievements();
    }
    #endregion

    #region PlaytimeAchievements

    private float totalTime = 0f;

    // platime in hours - 1, 25, 100
    public void CheckTime(float currentTime) {
        // change currentTime to hours
        currentTime /= 3600f;

        if (totalTime + currentTime > 168f && !achievementsUnlocked[6]) {
            UnlockAchievement(6);
        }
        else if (totalTime + currentTime > 24f && !achievementsUnlocked[5]) {
            UnlockAchievement(5);
        }
        else if (totalTime + currentTime > 1f && !achievementsUnlocked[4]) {
            UnlockAchievement(4);
        }
    }

    #endregion

    #region PlacingAchievements

    // have 100 fields
    public int fieldCount = 0;
    private void FieldPlaced() {
        fieldCount++;

        if (fieldCount >= 100) {
            UnlockAchievement(3);
            Build.fieldPlaced -= FieldPlaced;
            Build.fieldRemoved -= FieldRemoved;
        }
    }

    private void FieldRemoved() {
        fieldCount--;
    }

    #endregion

    #region UnlockAchievements

    // unlock all islands
    public void IslandUnlocked() {
        if (!achievementsUnlocked[0]) {
            UnlockAchievement(0);
        }
    }

    // unlock all crops
    // unlock all buildings
    public void ItemResearched() {
        // projet cely Build.blocklist pro typ "C" (crop) a pro cokoli ostatni (building - ma moc typu)

        // check if any crop is locked, else achievment
        if (!achievementsUnlocked[1]) {
            bool cropsUnlocked = true;
            foreach (List<string> block in main.build.blockList.Values) {
                if (block[2] == "C" && block[9] == "f") {
                    cropsUnlocked = false;
                    break;
                }
            }
            if (cropsUnlocked) UnlockAchievement(1);
        }

        // check if any building is locked, else achievment
        if (!achievementsUnlocked[2]) {
            bool buildingsUnlocked = true;
            foreach (List<string> block in main.build.blockList.Values) {
                if (block[2] != "C" && block[9] == "f") { 
                    buildingsUnlocked = false;
                    break;
                }
            }
            if (buildingsUnlocked) UnlockAchievement(2);
        }

        if (achievementsUnlocked[1] && achievementsUnlocked[2]) Build.itemResearched -= ItemResearched;
    }

    #endregion

    #region ResearchAchievements

    private void ResRateChanged(float resRate, int power) {

        // 1T research/sec
        if (!achievementsUnlocked[9]) {
            if ((resRate >= 1f && power >= 4) || (resRate > 999f && power >= 3)) {
                UnlockAchievement(9);
                TileInfo.ResRateChanged -= ResRateChanged;
            }
        }

        // 1M research/sec
        if (!achievementsUnlocked[8]) {
            if ((resRate >= 1f && power >= 2) || (resRate > 999f && power >= 1)) {
                UnlockAchievement(8);
            }
        }
    }

    #endregion

    #region MoneyAchievements

    private void MoneyChanged(float money, int power) {
        // check for achievemnts (100k, 1B, 1Qt)

        if (money >= 1 && power >= 5 && !achievementsUnlocked[12]) {
            UnlockAchievement(12);
            Manager.moneyChanged -= MoneyChanged;
        }
        else if (money >= 1 && power >= 3 && !achievementsUnlocked[11]) {
            UnlockAchievement(11);
            // unlock 1B achievemnt
        }
        // 100k money
        else if (money >= 100 && power >= 1 && !achievementsUnlocked[10]) {
            UnlockAchievement(10);
        }
    }

    #endregion

    #region OtherAchievements
    private int adCount = 0;
    // 50 Ads
    private void AdViewed() {
        adCount++;

        if (adCount >= 50) {
            UnlockAchievement(7);
            // TODO - Remove AdViewed method from event
        }
    }

    // Finish the game
    private void AllAchievementsCheck() {
        if (totalAchievementsUnlocked == achievements.Length) {
            UnlockAchievement(13);
        }
    }
    #endregion
}