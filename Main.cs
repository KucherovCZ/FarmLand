using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class Main : MonoBehaviour {
    #region editor variables
    public Tilemap ground, building, highlight, overlay, lockmap;
    public GridLayout gridLayout;

    public TileInfo[,] gridArray;
    public Vector2Int arrayBoundaries;

    public Text versionLabel;
    public Text errorLabel;

    #endregion

    #region loaded variables
    public Build build;
    public Manager manager;

    #endregion

    #region UI
    public Transform canvas;
    public Transform worldCanvas;

    private Transform unlock1, unlock2, unlock3;
    #endregion

    #region state variables
    public int currentOverlay = 0;
    #endregion

    public void Start() {
        Save.main = this;

        build = GetComponent<Build>();
        manager = GetComponent<Manager>();

        InitGridArray();
        SetRiverBoosts();

        /* testing */
        Testing.manager = manager;
        Testing.Cheat(0);

        versionLabel.text = Application.version;

        unlock1 = worldCanvas.Find("Unlock1");
        unlock2 = worldCanvas.Find("Unlock2");
        unlock3 = worldCanvas.Find("Unlock3");
    }

    #region Save/Load

    private long logoffTime = 0;

    private void OnApplicationPause(bool pauseStatus) {
        if (pauseStatus) {
            SaveGame();
            logoffTime = DateTime.Now.Ticks;
        }
        else {
            if (logoffTime == 0) return;
            manager.ShowIdleUI((DateTime.Now.Ticks - logoffTime) / 10000000, manager.productionRate, manager.prPower, manager.resRate, manager.rrPower);
            logoffTime = 0;
        }
    }

    private void OnApplicationQuit() {
        SaveGame();
    }

    public void SaveGame() {
        Save.SaveData("autosave", new DataPack(this, manager));
    }

    public void SaveGame(string name) {
        Save.SaveData(name, new DataPack(this, manager));
    }

    public void LoadGame() {
        LoadManager.ReloadGame();
    }

    public void RemoveSavedGame() {
        Save.RemoveSavedData("autosave");
        PlayerPrefs.SetInt("firstLaunch", 2);
        GetComponent<Achievements>().ResetAchievements();
        LoadManager.ReloadGame();
    }

    #endregion

    #region GridArray stuff

    private void InitGridArray() {
        gridArray = new TileInfo[arrayBoundaries.x, arrayBoundaries.y];
        Main main = this;

        for (int x = 0; x < gridArray.GetLength(0); x++) {
            for (int y = 0; y < gridArray.GetLength(1); y++) {
                TileInfo newTile = new TileInfo(new Vector3Int(x, y, 0), "", '0', 0, 0, 0, 0, false, ref main);
                gridArray[x, y] = newTile;
            }
        }
    }

    public void SetRiverBoosts() {
        for (int x = 0; x < gridArray.GetLength(0); x++) {
            for (int y = 0; y < gridArray.GetLength(1); y++) {
                if (ground.HasTile(new Vector3Int(x, y, 0))) {
                    string name = ground.GetTile(new Vector3Int(x, y, 0)).name;
                    if (name.Length > 4 && name.Substring(0, 5) == "River") {
                        // set all tiles in radius one to 1.1f
                        try {
                            for (int x2 = -1; x2 < 2; x2++) {
                                for (int y2 = -1; y2 < 2; y2++) {
                                    gridArray[x + x2, y + y2].UpdateCropRate(0.1f);
                                }
                            }
                        }
                        catch (IndexOutOfRangeException) { }
                        // set ef1, ef2 and type for removing water buildings
                        gridArray[x, y].type = 'W';
                        gridArray[x, y].ef1 = 10;
                        gridArray[x, y].ef2 = 1;
                    }
                }
            }
        }
    }

    public Vector3Int GetMouseCellPosition() {
        Vector3 temp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        temp.z = 0;
        Vector3Int result = gridLayout.WorldToCell(temp);
        return result;
    }

    public void SetOverlay(int type) {
        /* 
         * 1 - collecting
         * 2 - sell modifier
         * 3 - crop modifier
         */

        ClearOverlayMap();
        currentOverlay = type;

        switch (type) {
            case 0:
                return;

            case 1: // barn collecting overlay
                for (int x = 0; x < gridArray.GetLength(0); x++) {
                    for (int y = 0; y < gridArray.GetLength(1); y++) {
                        if (gridArray[x, y].collecting) {
                            overlay.SetTile(new Vector3Int(x, y, 0), build.whiteTile);
                            overlay.SetTileFlags(gridArray[x, y].position, TileFlags.None);
                            overlay.SetColor(gridArray[x, y].position, new Color(0.4f, 1f, 0.2f));
                        }
                    }
                }
                break;

            case 2: // sell modifier overlay
                List<float> levels = new List<float>();
                Dictionary<float, Color> colors = new Dictionary<float, Color>();

                // set all unique levels to list
                for (int x = 0; x < gridArray.GetLength(0); x++) {
                    for (int y = 0; y < gridArray.GetLength(1); y++) {
                        if (!levels.Contains(gridArray[x, y].sellModifier)) {
                            levels.Add(gridArray[x, y].sellModifier);
                        }
                    }
                }
                levels.Sort();



                if (levels[0] == levels[levels.Count - 1]) return;

                for (int i = 0; i < levels.Count; i++) {
                    if (levels[i] == 1) continue;
                    float change = (levels[i] - levels[0]) / (levels[levels.Count - 1] - levels[0]);
                    colors.Add(levels[i], new Color(0.8f - change / 2f, 0.3f + change / 2f, 0f));
                }



                for (int x = 0; x < gridArray.GetLength(0); x++) {
                    for (int y = 0; y < gridArray.GetLength(1); y++) {
                        if (gridArray[x, y].sellModifier != 1f) {
                            overlay.SetTile(gridArray[x, y].position, build.whiteTile);
                            overlay.SetTileFlags(gridArray[x, y].position, TileFlags.None);
                            overlay.SetColor(gridArray[x, y].position, colors[gridArray[x, y].sellModifier]);
                        }
                    }
                }

                break;

            case 3: // crop modifier overlay
                colors = new Dictionary<float, Color>
                {
                    { 1.1f, new Color(0.2f, 0.8f, 0.2f) },
                    { 1.15f, new Color(0.3f, 0.9f, 0.2f) },
                    { 1.3f, new Color(0.4f, 1f, 0.2f) },
                    { 1.5f, new Color(0.6f, 1f, 0.2f) }
                };

                for (int x = 0; x < gridArray.GetLength(0); x++) {
                    for (int y = 0; y < gridArray.GetLength(1); y++) {
                        if (gridArray[x, y].cropModifier != 1f) {
                            try {
                                overlay.SetTile(gridArray[x, y].position, build.whiteTile);
                                overlay.SetTileFlags(gridArray[x, y].position, TileFlags.None);
                                overlay.SetColor(gridArray[x, y].position, colors[gridArray[x, y].cropModifier]);
                            }
                            catch (KeyNotFoundException) {
                                Debug.LogError("Error when drawing crop modifier overlay:\nKey " + gridArray[x, y].cropModifier + " hasn't been found! It's probably missing from colors dictionary.");
                            }
                        }
                    }
                }

                break;

            default:
                Debug.LogError("\nOverlay with unknown type called from OverlayUI button\nProbably not defined in Main.GetOverlay()");
                break;
        }
    }

    public void ClearOverlayMap() {
        currentOverlay = 0;
        for (int x = 0; x < gridArray.GetLength(0); x++) {
            for (int y = 0; y < gridArray.GetLength(1); y++) {
                overlay.SetTile(new Vector3Int(x, y, 0), null);
            }
        }
    }

    #endregion

    #region WorldMap Unlocking

    public void UpdateUnlockUISize() {
        // get main camera orthographic size divided by 10 > values 0.2 - 1
        float scale = Camera.main.orthographicSize / 10;
        // set all UIs scale to that
        unlock1.localScale = new Vector3(scale, scale, 1);
        unlock2.localScale = new Vector3(scale, scale, 1);
        unlock3.localScale = new Vector3(scale, scale, 1);
    }

    public void UnlockMap(int islandNumber) {
        switch (islandNumber) {
            case 1:
                // check player for 1B $
                if (manager.CheckPlayerMoney(1, 3)) {
                    // turn off button and take money
                    unlock1.gameObject.SetActive(false);
                    manager.UpdateMoney(Manager.ChangeNumberToPower(-1, 3, manager.power));

                    // remove lock tiles
                    for (int x = 66; x < 101; x++) {
                        for (int y = 28; y < 60; y++) {
                            Vector3Int position = new Vector3Int(x, y, 0);
                            lockmap.SetTile(position, null);
                            if (building.HasTile(position) && building.GetTile(position).name == "Grass") {
                                building.SetTile(position, null);
                            }
                        }
                    }
                }
                break;

            case 2:
                // check player for 100T $
                if (manager.CheckPlayerMoney(100, 4)) {
                    // turn off button and take money
                    unlock2.gameObject.SetActive(false);
                    manager.UpdateMoney(Manager.ChangeNumberToPower(-100, 4, manager.power));

                    // remove lock tiles
                    for (int x = 30; x < 58; x++) {
                        for (int y = 64; y < 96; y++) {
                            Vector3Int position = new Vector3Int(x, y, 0);
                            lockmap.SetTile(position, null);
                            if (building.HasTile(position) && building.GetTile(position).name == "Grass") {
                                building.SetTile(position, null);
                            }
                        }
                    }
                }
                break;

            case 3:
                // check player for 100T $
                if (manager.CheckPlayerMoney(500, 5)) {
                    // turn off button and take money
                    unlock3.gameObject.SetActive(false);
                    manager.UpdateMoney(Manager.ChangeNumberToPower(-500, 5, manager.power));
                    this.gameObject.GetComponent<Achievements>().IslandUnlocked();

                    // remove lock tiles
                    for (int x = 71; x < 111; x++) {
                        for (int y = 76; y < 105; y++) {
                            Vector3Int position = new Vector3Int(x, y, 0);
                            lockmap.SetTile(position, null);
                            if (building.HasTile(position) && building.GetTile(position).name == "Grass") {
                                building.SetTile(position, null);
                            }
                        }
                    }
                }
                break;

            default:
                ShowErrorMessage("Invalid island number");
                break;
        }

        // check money

        // remove lock tilemap tiles for this area (needs to be hard defined)
        // somehow unlock island for building
    }

    #endregion

    #region Utility

    public void ShowErrorMessage(string message) {
        StartCoroutine(ShowErrorMessage(message, 2f));
    }

    private IEnumerator ShowErrorMessage(string message, float seconds) {
        errorLabel.gameObject.SetActive(true);
        errorLabel.text = message;
        yield return new WaitForSeconds(seconds);
        errorLabel.gameObject.SetActive(false);
    }

    #endregion
}
