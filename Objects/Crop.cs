using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Crop : MonoBehaviour
{

    public Vector3Int pos;
    public Main main;
    private Tile Base, Half, Full;
    public float growthTime, halfTime, amount;
    public int gtPower, amPower;
    public float baseProdRate, modifier;
    public int bprPower;
    public string loadName;
    public TileInfo parentTi;

    private bool canGrow = false;
    private bool active = false;

    private IEnumerator cropGrow;

    void Start() {
        // load tile images and manager reference
        Base = Resources.Load("Tiles/Crop/" + loadName + "/Base") as Tile;
        Half = Resources.Load("Tiles/Crop/" + loadName + "/Half") as Tile;
        Full = Resources.Load("Tiles/Crop/" + loadName + "/" + loadName) as Tile;

        // set base production rate and precalculate halftime
        baseProdRate = (float)amount / (float)growthTime;
        bprPower = amPower;

        while (baseProdRate < 1f && bprPower != 0) {
            baseProdRate *= 1000;
            bprPower--;
        }

        halfTime = growthTime / 2;
        if (halfTime * 2 < growthTime) {
            halfTime++;
        }

        UpdateCrop();
    }

    // updates crop behaviour -> coroutine, manager.productionRate, active
    public void UpdateCrop() {
        canGrow = main.gridArray[pos.x, pos.y].collecting;
        if (!active && canGrow) StartGrow();
        if (active && !canGrow) StopGrow();

        if (parentTi.cropModifier != modifier) {
            main.manager.productionRate -= Manager.ChangeNumberToPower(baseProdRate * modifier, bprPower, main.manager.prPower);
            modifier = parentTi.cropModifier;
            main.manager.productionRate += Manager.ChangeNumberToPower(baseProdRate * modifier, bprPower, main.manager.prPower);
            main.manager.UpdatePowers();
        }
    }

    private void StartGrow() {
        active = true;
        main.manager.productionRate += Manager.ChangeNumberToPower(baseProdRate * modifier, bprPower, main.manager.prPower);
        main.manager.UpdatePowers();
        if (cropGrow != null) StopCoroutine(cropGrow);
        cropGrow = Grow();
        StartCoroutine(cropGrow);
    }

    private IEnumerator Grow() {
        main.building.SetTile(pos, Base);
        yield return new WaitForSeconds(halfTime - 1f);
        main.building.SetTile(pos, Half);
        yield return new WaitForSeconds(halfTime);
        main.building.SetTile(pos, Full);
        yield return new WaitForSeconds(1);
        StartCoroutine(Grow());
    }

    private void StopGrow() {
        main.manager.productionRate -= Manager.ChangeNumberToPower(baseProdRate * modifier, bprPower, main.manager.prPower);
        main.manager.UpdatePowers();
        StopCoroutine(Grow());
        main.building.SetTile(pos, Base);
        active = false;
    }

    public void Remove() {
        StopCoroutine(Grow());
        if (parentTi.collecting) main.manager.productionRate -= Manager.ChangeNumberToPower(baseProdRate * modifier, bprPower, main.manager.prPower);
        main.manager.UpdatePowers();
        main.building.SetTile(pos, null);
        Destroy(this);
    }
}