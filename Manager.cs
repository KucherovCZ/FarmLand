using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class Manager : MonoBehaviour {

    /* this class manages all things based on custom gametick
     * 
     * crops
     * research
     * crops -> money transaction
     * various boosts
     * providing overlays
     * 
     */

    public float uiUpdateSpeed;
    public float idleMoneyModifier = 0.05f; // 5% base offline yield
    public float idleResearchModifier = 0.05f; // 5% base offline yield
    public Text moneyLabel, cropLabel, sellRateLabel;
    public Text resLabel, resRateLabel;
    public Text idleMoneyLabel, idleResearchLabel;
    public Animator idleUIAnimator;
    public GameObject cover;

    // events for achievements
    public static event Action<float, int> moneyChanged; // money + power
        
    #region NumberHandling

    public static float ChangeNumberToPower(float num, int numPower, int changePower) {
        return num * Mathf.Pow(10, 3 * (numPower - changePower));
    }

    public string FormatNumber(float number, int power) {
        return number.ToString(number >= 99.9f || number == 0 || power == 0 ? "F0" : number >= 10f ? "F1" : "F2") + powerMap[power];
    }

    #endregion

    #region Money

    public float money = 0;
    public int power = 0;

    public readonly string[] powerMap = {"", "k", "M", "B", "T", "Qd", "Qt", "Sx", "Sp", "Oc"};
    private readonly int powerMax = 9;

    public void SetMoney(float money) {
        this.money = money;
        moneyLabel.text = FormatNumber(money, power);
    }

    public void UpdateMoney(float change) {
        money += change;

        if (Mathf.Round(money) > 999 && power < powerMax) {
            money /= 1000;
            power++;
        }
        else if (money < 1f && power != 0) {
            money *= 1000;
            power--;
        }

        moneyChanged?.Invoke(money, power); // if moneyChanged != null, call event

        moneyLabel.text = FormatNumber(money, power);
    }

    public bool CheckPlayerMoney(float money, int power) {
        return this.money > ChangeNumberToPower(money, power, this.power);
    }

    #endregion

    #region IdleUpdate

    private float idleMoney = 0f;
    private float idleResearch = 0f;
    private int idleMoneyPower = 0;
    private int idleResearchPower = 0;

    public void ShowIdleUI(float idleTimeInSeconds, float lastProductionRate, int lastPrPower, float lastResRate, int lastRrPower) {
        // calculate and store idle money
        idleMoney = idleTimeInSeconds * lastProductionRate;
        idleMoney *= idleMoneyModifier;
        idleMoneyPower = lastPrPower;

        idleResearch = idleTimeInSeconds * lastResRate;
        idleResearch *= idleResearchModifier;
        idleResearchPower = lastRrPower;

        while (Mathf.Round(idleMoney) > 999) {
            idleMoney /= 1000f;
            idleMoneyPower++;
        }

        while (Mathf.Round(idleResearch) > 999) {
            idleResearch /= 1000f;
            idleResearchPower++;
        }

        if (idleMoney == 0 && idleResearch == 0) return;
     

        // show UI
        idleUIAnimator.ResetTrigger("Hidden");
        cover.SetActive(true);
        idleMoneyLabel.text = FormatNumber(idleMoney, idleMoneyPower);
        idleResearchLabel.text = FormatNumber(idleResearch, idleResearchPower);
    }

    public void UpdateMoneyAfterIdle(bool doubled) {
        if (doubled) {
            // show add, after completion double money
            idleMoney *= 2;
            idleResearch *= 2;
        }

        UpdateMoney(ChangeNumberToPower(idleMoney, idleMoneyPower, power));
        UpdateResearch(ChangeNumberToPower(idleResearch, idleResearchPower, rPower));
    }

    #endregion

    #region Research 

    public float research = 0;
    public int rPower = 0;
    public float resRate = 0;
    public int rrPower = 0;

    public void UpdateResearch(float change) {
        research += change;
        if (Mathf.Round(research) > 999f && rPower < powerMax) {
            research /= 1000;
            rPower++;
        }
        else if (research < 1f && rPower != 0) {
            research *= 1000;
            rPower--;
        }
        resLabel.text = FormatNumber(research, rPower);
    }

    #endregion

    #region Crops

    public float cropCount = 0;
    public int ccPower = 0;
    public float cropLimit = 0;
    public int clPower = 0;

    // used for SellButton only
    public void SellCrops() {
        UpdateMoney(ChangeNumberToPower(cropCount, ccPower, power));
        cropCount = 0;
        cropLabel.text = FormatNumber(cropCount, ccPower) + "/" + FormatNumber(cropLimit, clPower);
    }

    #endregion

    #region Production

    public float productionRate = 0;
    public int prPower = 0;
    public float sellRate = 0;
    public int srPower = 0;

    private int counter = 0;
    private void FixedUpdate() {
        counter++;
        if (counter >= (50f / uiUpdateSpeed)) {
            counter = 0;


            // add all crops to crop count
            cropCount += ChangeNumberToPower(productionRate / uiUpdateSpeed, prPower, ccPower);

            // if cropCount exceeds cropLimit -> set cropCount to cropLimit
            if (cropCount > ChangeNumberToPower(cropLimit, clPower, ccPower)) cropCount = ChangeNumberToPower(cropLimit, clPower, ccPower);

            // write out "$cropCount/$cropLimit"
            cropLabel.text = FormatNumber(cropCount, ccPower) + "/" + FormatNumber(cropLimit, clPower);

            // selling crops
            // if cropCount exceeds sellRate -> update money by sellRate
            if (cropCount > ChangeNumberToPower(sellRate / uiUpdateSpeed, srPower, ccPower)) {
                UpdateMoney(ChangeNumberToPower(sellRate / uiUpdateSpeed, srPower, power));
                cropCount -= ChangeNumberToPower(sellRate / uiUpdateSpeed, srPower, ccPower);
            }
            // else means sellRate is bigger than cropCount, thus bigger than production rate
            else {
                UpdateMoney(ChangeNumberToPower(cropCount, ccPower, power));
                cropCount = 0;
                cropLabel.text = 0 + "/" + FormatNumber(cropLimit, clPower);
            }

            if(productionRate > ChangeNumberToPower(sellRate, srPower, prPower)) {
                sellRateLabel.text = "+ " + FormatNumber(sellRate, srPower) + "/s";
            } else {
                sellRateLabel.text = "+ " + FormatNumber(productionRate, prPower) + "/s";
            }

            // research
            UpdateResearch(ChangeNumberToPower(resRate / uiUpdateSpeed, rrPower, rPower));
            resRateLabel.text = "+ " + FormatNumber(resRate, rrPower) + "/s";
            
            UpdatePowers();
        }
    }

    // handling of every number and its power
    public void UpdatePowers() {
        if (cropCount > 999f && ccPower < powerMax) {
            cropCount /= 1000;
            ccPower++;
        }
        else if (cropCount < 1f && ccPower != 0) {
            cropCount *= 1000;
            ccPower--;
        }

        if (cropLimit > 999f && clPower < powerMax) {
            cropLimit /= 1000;
            clPower++;
        }
        else if (cropLimit < 1f && clPower != 0) {
            cropLimit *= 1000;
            clPower--;
        }

        if (productionRate > 999f && prPower < powerMax) {
            productionRate /= 1000;
            prPower++;
        }
        else if (productionRate < 1f && prPower != 0) {
            productionRate *= 1000;
            prPower--;
        }

        if (sellRate > 999f && srPower < powerMax) {
            sellRate /= 1000;
            srPower++;
        }
        else if (sellRate < 1f && srPower != 0) {
            sellRate *= 1000;
            srPower--;
        }

        if (resRate > 999f && rrPower < powerMax) {
            resRate /= 1000;
            rrPower++;
        }
        else if (resRate < 1f && rrPower != 0) {
            resRate *= 1000;
            rrPower--;
        }
    }

    #endregion
}
