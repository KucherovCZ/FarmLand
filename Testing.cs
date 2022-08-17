using System;
using UnityEngine;

public static class Testing
{
    public static Manager manager;

    public static void Cheat(int code) {
        switch (code) {
            // normal start of the game
            case 0:
                manager.UpdateResearch(1000);
                manager.SetMoney(1000);
                break;
            
            // cheat code 1
            case 1:
                manager.UpdateResearch(5200);
                manager.SetMoney(50000);
                manager.cropLimit = 500;
                manager.productionRate = 200;
                manager.sellRate = 200;
                break;

            // cheat code 2
            case 2:
                manager.SetMoney(100000000000000000);
                manager.UpdateResearch(10000000000000000);
                break;
        }
    }
}
