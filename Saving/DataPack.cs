using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DataPack {
    public DataPack(Main main, Manager manager) {
        FetchGridArray(main.gridArray);
        FetchManager(manager);
        FetchBlockData(main);
        FetchMapUnlocks(main);
    }

    #region blockData
    public string[] blockLoadNames;
    public bool[] blockResearched;
    private void FetchBlockData(Main main) {
        blockResearched = new bool[main.build.blockList.Count];
        blockLoadNames = new string[main.build.blockList.Count];
        int i = 0;
        foreach (List<string> list in main.build.blockList.Values) {
            if (list[9] == "t") {
                blockResearched[i] = true;
                blockLoadNames[i] = list[1];
            }
            i++;
        }
    }

    #endregion

    #region TileInfos in GridArray
    public string[,] tiLoadName;

    private void FetchGridArray(TileInfo[,] gridArray) {
        tiLoadName = new string[gridArray.GetLength(0), gridArray.GetLength(1)];

        for (int x = 0; x < gridArray.GetLength(0); x++) {
            for (int y = 0; y < gridArray.GetLength(1); y++) {
                tiLoadName[x, y] = gridArray[x, y].loadName;
            }
        }
    }
    #endregion



    #region Manager

    // currentTime
    public long logoffTimeInSeconds;

    public float uiUpdateSpeed;
    // money
    public float money;
    public int power;

    // research
    public float research, resRate;
    public int rPower, rrPower;

    // crops
    public float cropCount, cropLimit;
    public int ccPower, clPower;

    // production
    public float productionRate, sellRate;
    public int prPower, srPower;

    private void FetchManager(Manager manager) {
        logoffTimeInSeconds = System.DateTime.Now.Ticks / 10000000;

        uiUpdateSpeed = manager.uiUpdateSpeed;

        money = manager.money;
        power = manager.power;

        research = manager.research;
        rPower = manager.rPower;
        resRate = manager.resRate;
        rrPower = manager.rrPower;

        cropCount = manager.cropCount;
        ccPower = manager.ccPower;
        cropLimit = manager.cropLimit;
        clPower = manager.clPower;

        productionRate = manager.productionRate;
        prPower = manager.prPower;
        sellRate = manager.sellRate;
        srPower = manager.srPower;
    }
    #endregion

    #region MapUnlocks
    public bool[] mapPartUnlocked = new bool[3];

    private void FetchMapUnlocks(Main main) {
        mapPartUnlocked[0] = !main.worldCanvas.Find("Unlock1").gameObject.activeSelf;
        mapPartUnlocked[1] = !main.worldCanvas.Find("Unlock2").gameObject.activeSelf;
        mapPartUnlocked[2] = !main.worldCanvas.Find("Unlock3").gameObject.activeSelf;
    }
    #endregion

}
