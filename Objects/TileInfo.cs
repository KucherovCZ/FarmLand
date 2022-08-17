using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class TileInfo
{
    // general
    public Vector3Int position;

    // tile information
    public bool collecting = false;
    public float cropModifier = 1f, sellModifier = 1f; 

    // building information
    public string loadName;
    public char type;
    public float ef1, ef2;
    public int ef1Power, ef2Power;

    private readonly Main main;

    public Crop crop = null;

    public TileInfo(Vector3Int pos, string loadName, char type, float ef1, int ef1Power, float ef2, int ef2Power, bool collecting, ref Main main) {
        position = pos;
        this.loadName = loadName;
        this.type = type;
        this.ef1 = ef1;
        this.ef1Power = ef1Power;
        this.ef2 = ef2;
        this.ef2Power = ef2Power;
        this.collecting = collecting;
        this.main = main;
    }

    public TileInfo() {
    }

    public static event Action<float, int> ResRateChanged;

    public void Placed() {
        switch (type) {
            case 'B': // barn
                // ef1 - crop storage   ef2 - collection range
                ChangeCollectingInGridArray(position, (int)ef2, true);
                main.manager.cropLimit += Manager.ChangeNumberToPower(ef1, ef1Power, main.manager.clPower);
                main.manager.UpdatePowers();
                break;

            case 'M': // market
                main.manager.sellRate += Manager.ChangeNumberToPower(ef1 * sellModifier, ef1Power, main.manager.srPower);
                main.manager.UpdatePowers();
                break;

            case 'V': // city (village)
                // update sell modifier for all tiles around in radius
                for (int x = (int)-ef2; x <= ef2; x++) {
                    for(int y = (int)-ef2; y <= ef2; y++) {
                        try {
                            main.gridArray[position.x + x, position.y + y].UpdateSellRate(ef1 / 100f);
                        }
                        catch (IndexOutOfRangeException) { }
                    }
                }
                break;

            case 'W': // cropBoost (water)
                // update crop modifier for all tiles around in radius
                for (int x = (int)-ef2; x <= ef2; x++) {
                    for (int y = (int)-ef2; y <= ef2; y++) {
                        try {
                            main.gridArray[position.x + x, position.y + y].UpdateCropRate(ef1 / 100f);
                        }
                        catch (IndexOutOfRangeException) { }
                    }
                }
                break;

            case 'R': // research building
                main.manager.resRate += Manager.ChangeNumberToPower(ef1, ef1Power, main.manager.rrPower);
                main.manager.UpdatePowers();
                ResRateChanged.Invoke(main.manager.resRate, main.manager.rrPower);
                break;

            case 'C': // crop
                crop = main.GetComponent<Transform>().gameObject.AddComponent<Crop>();
                crop.main = main;
                crop.pos = position;
                crop.loadName = loadName;
                crop.growthTime = ef1;
                crop.gtPower = ef1Power;
                crop.amount = ef2;
                crop.amPower = ef2Power;
                crop.modifier = cropModifier;
                crop.parentTi = this;
                break;
            default:
                Debug.LogWarning("\nTileInfo with unknown type was placed. " + position);
                break;
        }
    }

    public void Remove() {
        switch (type) {
            case 'C':
                crop.Remove();
                crop = null;
                break;
            case 'B':
                ChangeCollectingInGridArray(position, (int)ef2, false);

                // for all barns in radius ef2 + main.build.maxBarnRange changeCollectingingridarray()
                for (int x = (int)-ef2 - main.build.maxBarnRange; x <= ef2 + main.build.maxBarnRange; x++) {
                    for (int y = (int)-ef2 - main.build.maxBarnRange; y <= ef2 + main.build.maxBarnRange; y++) {
                        if (x == 0 && y == 0) continue;
                        try {
                            TileInfo ti = main.gridArray[position.x + x, position.y + y];
                            if (ti.type == 'B') ChangeCollectingInGridArray(ti.position, (int)ti.ef2, true);
                        } catch (IndexOutOfRangeException) { }
                    }
                }

                main.manager.cropLimit -= Manager.ChangeNumberToPower(ef1, ef1Power, main.manager.clPower);
                main.manager.UpdatePowers();
                break;
            case 'M':
                main.manager.sellRate -= Manager.ChangeNumberToPower(ef1 * sellModifier, ef1Power, main.manager.srPower);
                main.manager.UpdatePowers();
                break;
            case 'V':
                for (int x = (int)-ef2; x <= ef2; x++) {
                    for (int y = (int)-ef2; y <= ef2; y++) {
                        try {
                            main.gridArray[position.x + x, position.y + y].UpdateSellRate(-ef1  / 100f);
                        }
                        catch (IndexOutOfRangeException) { }
                    }
                }
                break;
            case 'W':
                // reset all crops to base values
                for (int x = (int)-ef2; x <= ef2; x++) {
                    for (int y = (int)-ef2; y <= ef2; y++) {
                        try {
                            main.gridArray[position.x + x, position.y + y].UpdateCropRate(0f);
                        }
                        catch (IndexOutOfRangeException) { }
                    }
                }
                
                // for every crop boost source in range, update crops around it
                for (int x = (int)-ef2-main.build.maxWaterRange; x <= ef2+ main.build.maxWaterRange; x++) {
                    for (int y = (int)-ef2- main.build.maxWaterRange; y <= ef2+ main.build.maxWaterRange; y++) {
                        try {
                            if(x == 0 && y == 0) continue;
                            TileInfo ti = main.gridArray[position.x + x, position.y + y];
                            if (ti.type == 'W') {
                                for (int a = (int)-ti.ef2; a <= ti.ef2; a++) { // a = x2
                                    for (int b = (int)-ti.ef2; b <= ti.ef2; b++) { // b = y2
                                        try {
                                            main.gridArray[ti.position.x + a, ti.position.y + b].UpdateCropRate(ti.ef1 / 100f);
                                        }
                                        catch (IndexOutOfRangeException) { }
                                    }
                                }
                            }
                        }
                        catch (IndexOutOfRangeException) { }
                    }
                }
                
                
                break;
            case 'R':
                main.manager.resRate -= Manager.ChangeNumberToPower(ef1, ef1Power, main.manager.rrPower);
                main.manager.UpdatePowers();
                break;

            default:
                Debug.LogWarning("TileInfo with unknown type was removed. " + position);
                break;
        }
        type = '0';
        loadName = "";
        ef1 = 0;
        ef2 = 0;
    }

    public new string ToString => "Pos: " + position + ", Type: " + type + ", Ef1: " + ef1.ToString() + ", Ef2: " + ef2.ToString();

    #region Barn

    private void ChangeCollectingInGridArray(Vector3Int barnPosition, int range, bool status) {
        for (int x = barnPosition.x - range; x <= barnPosition.x + range; x++) {
            for (int y = barnPosition.y - range; y <= barnPosition.y + range; y++) {
                if (x == barnPosition.x && y == barnPosition.y) continue;
                try {
                    TileInfo ti = main.gridArray[x, y];
                    ti.collecting = status;
                    if (ti.crop != null) {
                        ti.crop.UpdateCrop();
                    }
                } catch(IndexOutOfRangeException) { }
            }
        }
    }

    #endregion

    #region Market

    public void UpdateSellRate(float newModifier) {
        if (type == 'M') {
            main.manager.sellRate += Manager.ChangeNumberToPower(ef1 * newModifier, ef1Power, main.manager.srPower);
            main.manager.UpdatePowers();
        }
        sellModifier += newModifier;
    }

    public void UpdateCropRate(float newModifier) {
        if (newModifier + 1 > cropModifier || newModifier == 0f) {
            cropModifier = 1 + newModifier;
            if (type == 'C') crop.UpdateCrop();
        }
    }

    #endregion
}
