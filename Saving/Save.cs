using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;


public static class Save
{
    public static Main main;

    public static void SaveData(string saveName, DataPack data) {
        if (saveName == "" || saveName == null) {
            saveName = "autosave";
            Debug.LogWarning("SaveData() with no saveName defined, loading autosave.");
        }
        BinaryFormatter formatter = new BinaryFormatter();

        try {
            string path = Application.persistentDataPath + "/" + saveName + ".frm";
            FileStream stream = new FileStream(path, FileMode.Create);

            formatter.Serialize(stream, data);
            stream.Close();
        }
        catch (IOException e) {
            Debug.LogWarning(e);
        }
    }

    public static void LoadData(string saveName) {
        DataPack data;

        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/" + saveName + ".frm";
        try {
            FileStream stream = new FileStream(path, FileMode.Open);
            data = (DataPack)formatter.Deserialize(stream);
            stream.Close();
        }
        catch (FileNotFoundException e) {
            Debug.LogWarning("Save File " + path + " was not found.\nException details: " + e);
            return;
        }


        // load from DataPack to universe -- at this point Build.blockList must be initialized

        // if gridArrays of save and game doesnt match end loading
        if (data.tiLoadName.GetLength(0) != main.gridArray.GetLength(0)
            || data.tiLoadName.GetLength(1) != main.gridArray.GetLength(1)) {
            Debug.LogWarning("Loading warning - Trying to load gridArray with different size");
            return;
        }


        // load all buildings
        for (int x = 0; x < main.gridArray.GetLength(0); x++) {
            for (int y = 0; y < main.gridArray.GetLength(1); y++) {
                if (data.tiLoadName[x, y] == "") continue;

                main.build.blockList.TryGetValue(data.tiLoadName[x, y], out List<string> list);
                if (list[2][0] == 'C') continue;

                TileInfo changedTile = main.gridArray[x, y];
                changedTile.position = new Vector3Int(x, y, 0);
                changedTile.loadName = data.tiLoadName[x, y];
                changedTile.type = list[2][0];
                changedTile.ef1 = float.Parse(list[5]);
                changedTile.ef1Power = int.Parse(list[6]);
                changedTile.ef2 = float.Parse(list[7]);
                changedTile.ef2Power = int.Parse(list[8]);

                Tile currentTile = Resources.Load("Tiles/Building/" + changedTile.loadName) as Tile;

                main.building.SetTile(new Vector3Int(x, y, 0), currentTile);

                main.gridArray[x, y] = changedTile;
                changedTile.Placed();
            }
        }

        // load all crops
        for (int x = 0; x < main.gridArray.GetLength(0); x++) {
            for (int y = 0; y < main.gridArray.GetLength(1); y++) {
                if (data.tiLoadName[x, y] == "") continue;
                main.build.blockList.TryGetValue(data.tiLoadName[x, y], out List<string> list);
                if (list[2][0] != 'C') continue;

                TileInfo changedTile = main.gridArray[x, y];
                changedTile.position = new Vector3Int(x, y, 0);
                changedTile.loadName = data.tiLoadName[x, y];
                changedTile.type = list[2][0];
                changedTile.ef1 = float.Parse(list[5]);
                changedTile.ef1Power = int.Parse(list[6]);
                changedTile.ef2 = float.Parse(list[7]);
                changedTile.ef2Power = int.Parse(list[8]);

                Tile currentTile = Resources.Load("Tiles/Crop/" + changedTile.loadName + "/" + changedTile.loadName) as Tile;

                main.building.SetTile(changedTile.position, currentTile);
                main.ground.SetTile(changedTile.position, main.build.fieldTile);

                main.gridArray[x, y] = changedTile;
                changedTile.Placed();
            }
        }

        // load player, add idle time money + show UI screen with earnings
        Manager manager = main.manager;

        manager.uiUpdateSpeed = data.uiUpdateSpeed;

        manager.money = data.money;
        manager.power = data.power;

        manager.research = data.research;
        manager.rPower = data.rPower;
        manager.resRate = data.resRate;
        manager.rrPower = data.rrPower;

        manager.cropCount = data.cropCount;
        manager.ccPower = data.ccPower;
        manager.cropLimit = data.cropLimit;
        manager.clPower = data.clPower;

        manager.productionRate = 0;

        manager.sellRate = data.sellRate;
        manager.srPower = data.srPower;

        manager.UpdatePowers();

        // calculate idle time in seconds and call idleUI
        long currentTimeInSeconds = DateTime.Now.Ticks / 10000000;
        long deltaTimeInSeconds = currentTimeInSeconds - data.logoffTimeInSeconds;

        manager.ShowIdleUI((DateTime.Now.Ticks / 10000000) - data.logoffTimeInSeconds, data.productionRate, data.prPower, data.resRate, data.rrPower);

        // load blockResearched and remove already researched buttons from researchMenu
        Transform viewport = main.canvas.Find("BuildMenu/ScrollView/Viewport");
        RectTransform resContent = (RectTransform)viewport.Find("ResContent");
        RectTransform buildContent = (RectTransform)viewport.Find("BuildContent");
        RectTransform cropContent = (RectTransform)viewport.Find("CropContent");

        for (int i = 0; i < data.blockLoadNames.Length; i++) {
            if (data.blockResearched[i]) {
                string loadName = data.blockLoadNames[i];


                // change researched value in blockList
                main.build.blockList.TryGetValue(loadName, out List<string> block);
                block[9] = "t";
                main.build.blockList.Remove(loadName);
                main.build.blockList.Add(loadName, block);


                // remove researchButton and move all below up by change
                Transform resButton = resContent.Find(loadName);
                if (resButton == null) continue;

                // move all buttons under current up by change
                foreach (Transform button in resContent) {
                    if (button.localPosition.y < resButton.localPosition.y) button.localPosition -= Build.change;
                }
                // make resContent smaller by size of the button
                resContent.sizeDelta = new Vector2(resContent.sizeDelta.x, resContent.sizeDelta.y + Build.change.y);

                UnityEngine.Object.Destroy(resButton.gameObject);

                // change build / crop button cover active state to false
                if (block[2][0] == 'C') {
                    cropContent.Find(loadName + "/Cover").gameObject.SetActive(false);
                    cropContent.Find(loadName).GetComponent<Button>().enabled = true;
                }
                else {
                    buildContent.Find(loadName + "/Cover").gameObject.SetActive(false);
                    buildContent.Find(loadName).GetComponent<Button>().enabled = true;
                }
            }
        }

        // load map parts and remove lock tiles (grass)
        if (data.mapPartUnlocked[0]) {
            main.worldCanvas.Find("Unlock1").gameObject.SetActive(false);
            for (int x = 66; x < 101; x++) {
                for (int y = 28; y < 60; y++) {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    main.lockmap.SetTile(position, null);
                    if (main.building.HasTile(position) && main.building.GetTile(position).name == "Grass") {
                        main.building.SetTile(position, null);
                    }
                }
            }
        }
        if (data.mapPartUnlocked[1]) {
            main.worldCanvas.Find("Unlock2").gameObject.SetActive(false);
            for (int x = 30; x < 58; x++) {
                for (int y = 64; y < 96; y++) {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    main.lockmap.SetTile(position, null);
                    if (main.building.HasTile(position) && main.building.GetTile(position).name == "Grass") {
                        main.building.SetTile(position, null);
                    }
                }
            }
        }
        if (data.mapPartUnlocked[2]) {
            main.worldCanvas.Find("Unlock3").gameObject.SetActive(false);
            for (int x = 71; x < 111; x++) {
                for (int y = 76; y < 105; y++) {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    main.lockmap.SetTile(position, null);
                    if (main.building.HasTile(position) && main.building.GetTile(position).name == "Grass") {
                        main.building.SetTile(position, null);
                    }
                }
            }
        }
    }

    public static void RemoveSavedData(string saveName) {
        string path = Application.persistentDataPath + "/" + saveName + ".frm";
        File.Delete(path);
    }
}
