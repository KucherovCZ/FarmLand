using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class Build : MonoBehaviour {

    private Main main;
    private Manager manager;

    public bool buildMode, cropMode, removeMode;
    private Transform canvas;
    private Transform confirmUI, removeUI;

    public Tile currentTile, placeTile, removeTile, overlayTile, whiteTile, fieldTile, forestTile, grassTile, sandTile, seaTile;
    private char type;

    public Sprite cropIcon, researchIcon, moneyIcon, timeIcon, storageIcon, rangeIcon, cropBoostIcon, sellBoostIcon, speedIcon;

    public static Vector3 startPos = new Vector3(496f, -140f);
    public static Vector3 change = new Vector3(0f, -280f);

    public static readonly string[] startBuildings = { "Barn1", "Market1", "Wheat", "Lab1"};


    public float totalPrice = 0;
    public int tpPower = 0;
    private void UpdateTotalPricePower() {
        if (totalPrice > 999f) {
            totalPrice /= 1000f;
            tpPower++;
        }
        else if (totalPrice < 1f && tpPower > 0) {
            totalPrice *= 1000f;
            tpPower--;
        }

        if (tpPower == 0 && tpPower > 100) {
            totalPrice = Mathf.Round(totalPrice);
        }
    }

    public int maxWaterRange = 4;
    public int maxBarnRange = 3;

    /* List format: [0] = displayName
     *              [1] = loadName
     *              [2] = Type
     *              [3] = Price
     *              [4] = PricePower
     *              [5] = Ef1
     *              [6] = Ef1Power
     *              [7] = Ef2
     *              [8] = Ef2Power
     *              [9] = Unlocked
     */
    readonly public Dictionary<string, List<string>> blockList = new Dictionary<string, List<string>>();
    readonly public List<TileInfo> placeList = new List<TileInfo>();


    private void Start() {
        main = GetComponent<Main>();
        manager = GetComponent<Manager>();

        canvas = main.canvas;

        confirmUI = canvas.Find("ConfirmUI");
        confirmUI.Find("ConfirmButton").GetComponent<Button>().onClick.AddListener(Place);
        confirmUI.Find("CancelButton").GetComponent<Button>().onClick.AddListener(CancelPlace);
        removeUI = canvas.Find("RemoveUI");

        LoadButtons();
    }

    private Vector3 lastMousePos;
    private bool wasOverUI = false;
    private void Update() {
        if ((buildMode || cropMode || removeMode) && Input.touchCount > 0) {
            if (Input.GetTouch(0).phase == TouchPhase.Began) {
                lastMousePos = Input.mousePosition;
                // if touch started over UI, ignore it when ended
                wasOverUI = EventSystem.current.IsPointerOverGameObject(0);
            }
            // on input released
            if (!wasOverUI && Input.GetTouch(0).phase == TouchPhase.Ended && Vector3.Distance(lastMousePos, Input.mousePosition) < 50) {
                Vector3Int mouseCellPos = main.GetMouseCellPosition();
                if (main.ground.HasTile(mouseCellPos)) {
                    if (removeMode) {
                        if (main.building.HasTile(mouseCellPos)) {
                            if (main.building.GetTile(mouseCellPos) == forestTile) return;
                            if (!main.highlight.HasTile(mouseCellPos)) {
                                // add ti to placeList and add highlight tile
                                placeList.Add(main.gridArray[mouseCellPos.x, mouseCellPos.y]);
                                main.highlight.SetTile(mouseCellPos, removeTile);

                                blockList.TryGetValue(main.gridArray[mouseCellPos.x, mouseCellPos.y].loadName, out List<string> block);
                                totalPrice += Manager.ChangeNumberToPower(float.Parse(block[3]) / 2f, int.Parse(block[4]), tpPower);
                                UpdateTotalPricePower();
                                removeUI.Find("Price").GetComponent<Text>().text = totalPrice.ToString() + manager.powerMap[tpPower];
                            }
                            else {
                                // remove from placeList (removeList)
                                main.highlight.SetTile(mouseCellPos, null);

                                foreach (TileInfo ti in placeList) {
                                    if (ti.position == mouseCellPos) {
                                        placeList.Remove(ti);
                                        blockList.TryGetValue(main.gridArray[mouseCellPos.x, mouseCellPos.y].loadName, out List<string> block);
                                        totalPrice -= Manager.ChangeNumberToPower(float.Parse(block[3]) / 2f, int.Parse(block[4]), tpPower);
                                        UpdateTotalPricePower();
                                        removeUI.Find("Price").GetComponent<Text>().text = totalPrice.ToString() + manager.powerMap[tpPower];
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else if (buildMode) {
                        if (main.ground.GetTile(mouseCellPos).name == "Grass") { // if placing on buildable tile
                            if (!main.building.HasTile(mouseCellPos)) { // if there is no building already
                                if (!main.highlight.HasTile(mouseCellPos)) { // if there is no building PLANNED already
                                    // add to placeList

                                    // get info about the tile for placing
                                    blockList.TryGetValue(currentTile.name, out List<string> block);

                                    // create new TileInfo and retake some tile properties
                                    TileInfo ti = new TileInfo()
                                    {
                                        position = mouseCellPos,
                                        loadName = currentTile.name,
                                        type = block[2][0],
                                        ef1 = float.Parse(block[5]),
                                        ef1Power = int.Parse(block[6]),
                                        ef2 = float.Parse(block[7]),
                                        ef2Power = int.Parse(block[8])
                                    };

                                    placeList.Add(ti);

                                    // visible stuff
                                    main.highlight.SetTile(mouseCellPos, placeTile);

                                    // money
                                    totalPrice += Manager.ChangeNumberToPower(float.Parse(block[3]), int.Parse(block[4]), tpPower);
                                    UpdateTotalPricePower();
                                    confirmUI.Find("Price").GetComponent<Text>().text = totalPrice.ToString() + manager.powerMap[tpPower];
                                }
                                else {
                                    // remove from placeList
                                    blockList.TryGetValue(currentTile.name, out List<string> block);
                                    main.highlight.SetTile(mouseCellPos, null);
                                    totalPrice -= Manager.ChangeNumberToPower(float.Parse(block[3]), int.Parse(block[4]), tpPower);
                                    UpdateTotalPricePower();
                                    confirmUI.Find("Price").GetComponent<Text>().text = totalPrice.ToString() + manager.powerMap[tpPower];
                                    foreach (TileInfo ti in placeList) {
                                        if (ti.position == mouseCellPos) {
                                            placeList.Remove(ti);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (cropMode) { // if ground is grass and no building is present
                        if ((main.ground.GetTile(mouseCellPos).name == "Grass" || main.ground.GetTile(mouseCellPos).name == "Field") && !main.building.HasTile(mouseCellPos)) {
                            if (!main.highlight.HasTile(mouseCellPos)) {


                                blockList.TryGetValue(currentTile.name, out List<string> block);

                                // create new TileInfo
                                TileInfo ti = new TileInfo()
                                {
                                    position = mouseCellPos,
                                    loadName = currentTile.name,
                                    type = block[2][0],
                                    ef1 = float.Parse(block[5]),
                                    ef1Power = int.Parse(block[6]),
                                    ef2 = float.Parse(block[7]),
                                    ef2Power = int.Parse(block[8])
                                };

                                // add edited tileInfo to placelist
                                placeList.Add(ti);

                                // visible stuff
                                main.highlight.SetTile(mouseCellPos, placeTile);

                                // money
                                totalPrice += Manager.ChangeNumberToPower(float.Parse(block[3]), int.Parse(block[4]), tpPower);
                                UpdateTotalPricePower();
                                confirmUI.Find("Price").GetComponent<Text>().text = totalPrice.ToString() + manager.powerMap[tpPower];
                            }
                            else {
                                // remove from placelist
                                blockList.TryGetValue(currentTile.name, out List<string> block);
                                main.highlight.SetTile(mouseCellPos, null);
                                totalPrice -= Manager.ChangeNumberToPower(float.Parse(block[3]), int.Parse(block[4]), tpPower);
                                UpdateTotalPricePower();
                                confirmUI.Find("Price").GetComponent<Text>().text = totalPrice.ToString() + manager.powerMap[tpPower];
                                foreach (TileInfo ti in placeList) {
                                    if (ti.position == mouseCellPos) {
                                        placeList.Remove(ti);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    // add new placed methods in TileInfo.cs Placed()
    #region Place

    public static event Action fieldPlaced;

    private void Place() {
        if (manager.money >= Manager.ChangeNumberToPower((float)totalPrice, tpPower, manager.power)) {
            foreach (TileInfo ti in placeList) {
                TileInfo changedTile = main.gridArray[ti.position.x, ti.position.y];
                changedTile.position = ti.position;
                changedTile.loadName = ti.loadName;
                changedTile.type = ti.type;
                changedTile.ef1 = ti.ef1;
                changedTile.ef1Power = ti.ef1Power;
                changedTile.ef2 = ti.ef2;
                changedTile.ef2Power = ti.ef2Power;


                main.highlight.SetTile(ti.position, null);
                main.building.SetTile(ti.position, currentTile);
                if (type == 'C') {
                    main.ground.SetTile(ti.position, fieldTile);
                    if (fieldPlaced != null) fieldPlaced.Invoke();
                }
                main.gridArray[ti.position.x, ti.position.y] = changedTile;
                changedTile.Placed();
                if (main.currentOverlay != 0) main.SetOverlay(main.currentOverlay);
            }

            manager.UpdateMoney(Manager.ChangeNumberToPower((float)-totalPrice, tpPower, manager.power));

            EndBuildMode();
        }
        else {
            main.ShowErrorMessage("You're poor");
        }
    }

    private void CancelPlace() {
        foreach (TileInfo ti in placeList) {
            main.highlight.SetTile(ti.position, null);
        }

        EndBuildMode();
    }

    #endregion

    #region BuildMode

    private void StartBuildMode() {
        switch(type) {
            case 'B':
                buildMode = true;
                break;
            case 'C':
                cropMode = true;
                break;
            default:
                Debug.LogError("Jsi retard");
                break;
        }
        // UI
        confirmUI.GetComponent<Animator>().ResetTrigger("Hidden");
        canvas.GetComponent<Animator>().SetTrigger("MainHidden");

        // money
        confirmUI.Find("Price").GetComponent<Text>().text = "0";
    }

    private void EndBuildMode() {
        // logic
        buildMode = false;
        cropMode = false;

        // UI
        confirmUI.GetComponent<Animator>().SetTrigger("Hidden");
        canvas.GetComponent<Animator>().ResetTrigger("MainHidden");

        // tile stuff
        currentTile = null;
        placeList.Clear();

        // money
        totalPrice = 0;
        tpPower = 0;
        confirmUI.Find("Price").GetComponent<Text>().text = "0";
    }

    #endregion

    #region RemoveMode

    public static event Action fieldRemoved;

    public void StartRemoveMode() {
        removeMode = true;

        totalPrice = 0f;
        tpPower = 0;
        removeUI.Find("Price").GetComponent<Text>().text = "0";
    }

    public void EndRemoveMode() {
        removeMode = false;

        if (placeList.Count > 0) {
            foreach (TileInfo ti in placeList) {
                main.highlight.SetTile(ti.position, null);
            }
            placeList.Clear();
        }

        totalPrice = 0f;
        tpPower = 0;
        removeUI.Find("Price").GetComponent<Text>().text = "0";
    }

    public void Remove() {
        foreach (TileInfo ti in placeList) {
            if (ti.type == 'C') {
                main.ground.SetTile(ti.position, grassTile);
                fieldRemoved.Invoke();
            }
            main.building.SetTile(ti.position, null);
            main.highlight.SetTile(ti.position, null);
            main.gridArray[ti.position.x, ti.position.y] = ti;
            ti.Remove();
            if (main.currentOverlay != 0) main.SetOverlay(main.currentOverlay);
        }

        manager.UpdateMoney(Manager.ChangeNumberToPower((float)totalPrice, tpPower, manager.power));
        totalPrice = 0f;

        placeList.Clear();
        EndRemoveMode();
    }
    #endregion

    #region button functions

    private void BuildButton(string loadName) {
        // hide buildMenu
        canvas.Find("BuildMenu").GetComponent<Animator>().SetTrigger("Hidden");

        // load placed tile
        currentTile = Resources.Load("Tiles/Building/" + loadName) as Tile;

        type = 'B';
        StartBuildMode();
    }

    private void CropButton(string loadName) {
        //hide buildMenu
        canvas.Find("BuildMenu").GetComponent<Animator>().SetTrigger("Hidden");

        // load placed crop
        currentTile = Resources.Load("Tiles/Crop/" + loadName + "/" + loadName) as Tile;

        type = 'C';
        StartBuildMode();
    }

    public static event Action itemResearched;

    private void ResButton(string loadName, float resCost, int power, GameObject button, Transform content) {
        // button - resButton || content - content with the other button

        if (manager.research >= Manager.ChangeNumberToPower(resCost, power, manager.rPower)) {
            manager.UpdateResearch(Manager.ChangeNumberToPower(-resCost, power, manager.rPower));
            button.GetComponent<Button>().enabled = false;
            button.GetComponent<Button>().onClick.RemoveAllListeners();
            blockList.TryGetValue(loadName, out List<string> block);
            block[9] = "t";
            blockList.Remove(loadName);
            blockList.Add(loadName, block);

            itemResearched.Invoke();

            Transform otherButton = content.Find(loadName);
            otherButton.Find("Cover").gameObject.SetActive(false);
            otherButton.gameObject.SetActive(true);
            otherButton.GetComponent<Button>().enabled = true;

            Transform viewport = canvas.Find("BuildMenu/ScrollView/Viewport");
            RectTransform resContent = (RectTransform)viewport.Find("ResContent");

            foreach (Transform resButton in resContent) {
                if (resButton.localPosition.y > button.transform.localPosition.y) continue;
                resButton.localPosition -= change;
            }
            
            resContent.sizeDelta = new Vector2(resContent.sizeDelta.x, resContent.sizeDelta.y + change.y);

            Destroy(button);
        } else {
            main.ShowErrorMessage("Not enough research");
        }
    }

    #endregion

    #region Initial setup of buttons

    public GameObject buildButton;
    public GameObject cropButton;
    public TextAsset buttonInfo;

    private void LoadButtons() {
        Transform viewport = canvas.Find("BuildMenu/ScrollView/Viewport");
        RectTransform buildContent = (RectTransform)viewport.Find("BuildContent");
        RectTransform cropContent = (RectTransform)viewport.Find("CropContent");
        RectTransform resContent = (RectTransform)viewport.Find("ResContent");
        int b = 0, c = 0, r = 0;

        string[] lineList = buttonInfo.text.Split('\n');

        int switchType = 0;
        for (int l = 1; l < lineList.Length; l++) {
            if (lineList[l].Length > 0) {
                if (lineList[l][0] == '*') {
                    switch (lineList[l][1]) {
                        case 'B':
                            switchType = 0;
                            break;

                        case 'C':
                            switchType = 1;
                            break;

                        case 'R':
                            switchType = 2;
                            break;

                        default:
                            Debug.LogError("\nType selection in buttonInfo.txt is incorrect!");
                            break;
                    }
                }
                else {
                    string[] arr = lineList[l].Split('-');
                    for (int i = 0; i < arr.Length; i++) {
                        arr[i] = arr[i].Trim('\t');
                    }


                    if (switchType == 2) {
                        // weird syntax for switch assigning into variable
                        GameObject button = (arr[2][0]) switch
                        {
                            'C' => Instantiate(cropContent.Find(arr[1]).gameObject, resContent),
                            _ => Instantiate(buildContent.Find(arr[1]).gameObject, resContent),
                        };

                        button.GetComponent<Button>().onClick.AddListener(delegate {
                            ResButton(arr[1], float.Parse(arr[3]), int.Parse(arr[4]), button,
                                       arr[2][0] == 'C' ? cropContent : buildContent);
                        });
                        button.transform.localPosition = (startPos + change * r);

                        r++;
                        button.name = arr[1];
                        button.SetActive(true);
                        button.transform.Find("Price/Icon").GetComponent<Image>().sprite = researchIcon;
                        button.transform.Find("Price").GetComponent<Text>().text = arr[3] + manager.powerMap[int.Parse(arr[4])];
                        button.transform.Find("Cover").gameObject.SetActive(false);
                        button.GetComponent<Button>().enabled = true;

                    } // research
                    else {
                        // creation of List and putting it in block Dictionary
                        List<string> temp = new List<string>();
                        for (int i = 0; i < arr.Length; i++) temp.Add(arr[i]); // 0-diplayName 1-loadName 2-type 3-price 4-pricePower 5-ef1 6-ef1P 7-ef2 8-ef2P 9-unlocked
                        temp.Add("f");

                        // enable starter building defined in build:25 startBuildings array
                        foreach (string startBuildingLoadName in startBuildings) {
                            if (arr[1] == startBuildingLoadName) temp[9] = "t";
                        }

                        // add block with name arr[1] and list of properties
                        blockList.Add(arr[1], temp);

                        if (switchType == 0) {
                            // BUILDING STUFF
                            GameObject button = Instantiate(buildButton, buildContent);
                            button.GetComponent<Button>().onClick.AddListener(delegate { BuildButton(arr[1]); });
                            Transform btn = button.transform;
                            btn.localPosition = (startPos + change * b);
                            b++;

                            button.name = arr[1];
                            btn.Find("Image").GetComponent<Image>().sprite = Resources.Load<Tile>("Tiles/Building/" + arr[1]).sprite;
                            btn.Find("Name").GetComponent<Text>().text = arr[0];
                            btn.Find("Price").GetComponent<Text>().text = arr[3] + manager.powerMap[int.Parse(arr[4])];

                            button.SetActive(true);
                            if (temp[9][0] == 'f') {
                                btn.GetComponent<Button>().enabled = false;
                                btn.Find("Cover").gameObject.SetActive(true);
                            }

                            // switch building type based on type (arr[2])
                            Transform ef1 = btn.Find("Ef1");
                            Transform ef2 = btn.Find("Ef2");
                            switch (arr[2][0]) {
                                // base ImageBG is grass
                                case 'B': // barn - Storage | Range
                                    ef1.Find("Icon").GetComponent<Image>().sprite = storageIcon;
                                    ef1.GetComponent<Text>().text = arr[5] + manager.powerMap[int.Parse(arr[6])];
                                    ef2.Find("Icon").GetComponent<Image>().sprite = rangeIcon;
                                    ef2.GetComponent<Text>().text = arr[7] + manager.powerMap[int.Parse(arr[8])];
                                    break;

                                case 'M': // market - sell speed
                                    ef1.gameObject.SetActive(false);
                                    ef2.Find("Icon").GetComponent<Image>().sprite = cropIcon;
                                    ef2.GetComponent<Text>().text = arr[5] + manager.powerMap[int.Parse(arr[6])] + "/s";
                                    break;

                                case 'R': // research - research speed
                                    ef1.gameObject.SetActive(false);
                                    ef2.Find("Icon").GetComponent<Image>().sprite = researchIcon;
                                    ef2.GetComponent<Text>().text = arr[5] + manager.powerMap[int.Parse(arr[6])] + "/s";
                                    break;

                                case 'V':
                                    ef1.Find("Icon").GetComponent<Image>().sprite = sellBoostIcon;
                                    ef1.GetComponent<Text>().text = arr[5] + manager.powerMap[int.Parse(arr[6])] + "%";
                                    ef2.Find("Icon").GetComponent<Image>().sprite = rangeIcon;
                                    ef2.GetComponent<Text>().text = arr[7] + manager.powerMap[int.Parse(arr[8])];
                                    break;

                                case 'W':
                                    ef1.Find("Icon").GetComponent<Image>().sprite = cropBoostIcon;
                                    ef1.GetComponent<Text>().text = arr[5] + manager.powerMap[int.Parse(arr[6])] + "%";
                                    ef2.Find("Icon").GetComponent<Image>().sprite = rangeIcon;
                                    ef2.GetComponent<Text>().text = arr[7] + manager.powerMap[int.Parse(arr[8])];
                                    break;
                            }
                        } // building
                        else if (switchType == 1) {
                            // CROP STUFF
                            // imageBG is always field
                            GameObject button = Instantiate(cropButton, cropContent);
                            button.GetComponent<Button>().onClick.AddListener(delegate { CropButton(arr[1]); });
                            Transform btn = button.transform;
                            btn.localPosition = (startPos + change * c);
                            c++;

                            button.name = arr[1];
                            try {
                                btn.Find("Image").GetComponent<Image>().sprite = Resources.Load<Tile>("Tiles/Crop/" + arr[1] + "/" + arr[1]).sprite;
                            } catch (NullReferenceException) {
                                Debug.LogError("Loading tile sprite for " + arr[1] + " failed!\nCheck Resources/Tiles/Crop folder for missing sprites or buttonInfo.txt entry.");
                            }
                            btn.Find("Name").GetComponent<Text>().text = arr[0];
                            btn.Find("Price").GetComponent<Text>().text = arr[3] + manager.powerMap[int.Parse(arr[4])];

                            btn.Find("Time").GetComponent<Text>().text = arr[5] + (int.Parse(arr[6]) == 0 ? "s" : int.Parse(arr[6]) == 1 ? "m" : "h");
                            btn.Find("Amount").GetComponent<Text>().text = arr[7] + manager.powerMap[int.Parse(arr[8])];

                            button.SetActive(true);
                            if (temp[9][0] == 'f') {
                                btn.GetComponent<Button>().enabled = false;
                                btn.Find("Cover").gameObject.SetActive(true);
                            }


                        } // crops
                    } // building and crops
                }
            }
        }

        // make contents have correct size based on button count
        buildContent.sizeDelta = new Vector2(buildContent.sizeDelta.x, change.y * -b);
        cropContent.sizeDelta = new Vector2(cropContent.sizeDelta.x, change.y * -c);
        resContent.sizeDelta = new Vector2(resContent.sizeDelta.x, change.y * -r);

    }

    #endregion
}
