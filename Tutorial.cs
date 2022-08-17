using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Animations;
using UnityEngine.Tilemaps;

public class Tutorial : MonoBehaviour {

    // all buttons and shit here


    private Main main;
    private Transform tutorialHolder;
    private Animator animator;
    private Transform skipBtnTransform, nextBtnTransform;
    private Button skipButton, nextButton;
    Transform startMenu, mainMenu, buildMenu, confirmMenu;
    Transform viewport;

    private int partNumber = 0;

    public void StartTutorial(Main main) {
        this.main = main;
        tutorialHolder = main.canvas.Find("Tutorial");
        animator = tutorialHolder.GetComponent<Animator>();
        skipBtnTransform = tutorialHolder.Find("SkipButton");
        nextBtnTransform = tutorialHolder.Find("NextButton");
        skipButton = skipBtnTransform.GetComponent<Button>();
        nextButton = nextBtnTransform.GetComponent<Button>();

        startMenu = main.canvas.Find("StartMenu");
        mainMenu = main.canvas.Find("MainMenu");
        buildMenu = main.canvas.Find("BuildMenu");
        confirmMenu = main.canvas.Find("ConfirmUI");
        viewport = buildMenu.Find("ScrollView/Viewport");

        skipButton.onClick.AddListener(() => StartCoroutine(EndTutorial(0f)));
        nextButton.onClick.AddListener(() => NextPart());

        tutorialHolder.gameObject.SetActive(true);
        NextPart();
    }

    UnityAction listener;

    private void NextPart() {
        partNumber++;

        animator.Play("Part" + partNumber.ToString());

        listener = () => NextPart();

        switch (partNumber) {
            case 1:
                // turn off all 4 buttons in startMenu
                foreach(Transform button in startMenu) {
                    if (button.name == "Title") continue;
                    button.GetComponent<Button>().interactable = false;
                }
                break;

            case 2:
                // turn off all buttons in mainMenu
                main.canvas.GetComponent<Animator>().SetTrigger("Hidden");

                // allow camera movement
                GetComponent<CameraControl>().AllowMovement(true);

                foreach (Transform button in mainMenu) {
                    button.GetComponent<Button>().interactable = false;
                }
                break;

            case 3:
                // buttons are hidden and not active
                skipButton.interactable = false;
                nextButton.interactable = false;

                // allow buildButton in mainMenu and add listener for NextPart()
                mainMenu.Find("BuildButton").GetComponent<Button>().interactable = true;
                mainMenu.Find("BuildButton").GetComponent<Button>().onClick.AddListener(listener);
                
                break;

            case 4:
                // remove listener from buildButton
                mainMenu.Find("BuildButton").GetComponent<Button>().interactable = false;
                mainMenu.Find("BuildButton").GetComponent<Button>().onClick.RemoveListener(listener);

                // disable buildMenu closeButton and buildContent scrolling
                buildMenu.Find("CloseButton").GetComponent<Button>().interactable = false;
                buildMenu.Find("ScrollView").GetComponent<ScrollRect>().vertical = false;

                // add listener to barn1 button and disable other 2 unlocked buttons
                viewport.Find("BuildContent/Barn1").GetComponent<Button>().onClick.AddListener(listener);
                viewport.Find("BuildContent/Market1").GetComponent<Button>().interactable = false;
                viewport.Find("BuildContent/Lab1").GetComponent<Button>().interactable = false;
                break;

            case 5:
                // remove listener from Barn1 button
                viewport.Find("BuildContent/Barn1").GetComponent<Button>().onClick.RemoveListener(listener);

                // turn off confirmMenu buttons
                confirmMenu.Find("ConfirmButton").GetComponent<Button>().interactable = false;
                confirmMenu.Find("CancelButton").GetComponent<Button>().interactable = false;

                // continue once barn1 is placed
                StartCoroutine(BuildingPlacedCheck());
                break;

            case 6:
                // turn on buildButton
                mainMenu.Find("BuildButton").GetComponent<Button>().interactable = true;

                // turn off barn1 and turn on market1 buttons
                viewport.Find("BuildContent/Barn1").GetComponent<Button>().interactable = false;
                viewport.Find("BuildContent/Market1").GetComponent<Button>().interactable = true;

                // turn off confirmButton
                confirmMenu.Find("ConfirmButton").GetComponent<Button>().interactable = false;

                // check for market placed, then continue
                listener = () => StartCoroutine(BuildingPlacedCheck());
                viewport.Find("BuildContent/Market1").GetComponent<Button>().onClick.AddListener(listener);

                break;

            case 7:
                // remove market1 listener
                viewport.Find("BuildContent/Market1").GetComponent<Button>().onClick.RemoveListener(listener);

                // allow crop building
                mainMenu.Find("CropButton").GetComponent<Button>().interactable = true;

                // disable confirmButton
                confirmMenu.Find("ConfirmButton").GetComponent<Button>().interactable = false;

                // set listener for Wheat -> cropPlacedCheck coroutine
                listener = () => StartCoroutine(CropPlacedCheck());
                viewport.Find("CropContent/Wheat").GetComponent<Button>().onClick.AddListener(listener);

                break;

            case 8:
                viewport.Find("CropContent/Wheat").GetComponent<Button>().onClick.RemoveListener(listener);

                nextButton.interactable = true;
                skipButton.interactable = true;
                break;

            case 9:
                nextButton.interactable = false;
                skipButton.interactable = false;

                // allow reserachbutton
                mainMenu.Find("ResButton").GetComponent<Button>().interactable = true;

                // set listener for Lettuce resButton
                listener = () => NextPart();
                viewport.Find("ResContent/Lettuce").GetComponent<Button>().onClick.AddListener(listener);


                break;

            case 10:
                // remove listener from Lettuce resButton
                viewport.Find("ResContent/Lettuce").GetComponent<Button>().onClick.RemoveListener(listener);

                // turn of resButton
                mainMenu.Find("ResButton").GetComponent<Button>().interactable = false;

                // turn on nextButton
                nextBtnTransform.Find("Text").GetComponent<Text>().text = "Finish";
                nextButton.interactable = true;
                
                // hide research menu
                buildMenu.GetComponent<Animator>().SetTrigger("Hidden");

                break;

            case 11:
                // end tutorial with 1 second delay to wait for final animation
                StartCoroutine(EndTutorial(1f));

                break;
        }
    }

    private IEnumerator BuildingPlacedCheck() {
        mainMenu.Find("BuildButton").GetComponent<Button>().interactable = false;

        bool placed = false;
        while (main.build.buildMode) {
            if (!placed && main.build.placeList.Count == 1) {
                placed = true;
                confirmMenu.Find("ConfirmButton").GetComponent<Button>().interactable = true;
            }
            else if (placed && main.build.placeList.Count != 1) {
                placed = false;
                confirmMenu.Find("ConfirmButton").GetComponent<Button>().interactable = false;
            }
            yield return new WaitForSeconds(0.1f);
        }
        NextPart();
        StopAllCoroutines();
    }

    private IEnumerator CropPlacedCheck() {
        Debug.LogWarning("CropButton interactable = false");
        mainMenu.Find("CropButton").GetComponent<Button>().interactable = false;

        bool placed = false;
        while (main.build.cropMode) {
            if (!placed && main.build.placeList.Count == 3) {
                placed = true;
                confirmMenu.Find("ConfirmButton").GetComponent<Button>().interactable = true;
            }
            else if (placed && main.build.placeList.Count != 3) {
                placed = false;
                confirmMenu.Find("ConfirmButton").GetComponent<Button>().interactable = false;
            }
            yield return new WaitForSeconds(0.1f);
        }
        StopAllCoroutines();
        NextPart();
    }

    private IEnumerator EndTutorial(float waitTime) {
        skipButton.onClick.RemoveAllListeners();
        nextButton.onClick.RemoveAllListeners();

        partNumber = 0;

        yield return new WaitForSeconds(waitTime);

        // allow all disabled buttons in
        // startMenu
        foreach (Transform child in startMenu) {
            Button btn;
            if ((btn = child.GetComponent<Button>()) != null) {
                btn.interactable = true;
            }
        }

        // mainMenu
        foreach (Transform child in mainMenu) {
            Button btn;
            if ((btn = child.GetComponent<Button>()) != null) {
                btn.interactable = true;
            }
        }

        // buildMenu
        buildMenu.Find("ScrollView").GetComponent<ScrollRect>().vertical = true;
        buildMenu.Find("CloseButton").GetComponent<Button>().interactable = true;

        // build buttons
        viewport.Find("BuildContent/Barn1").GetComponent<Button>().interactable = true;
        viewport.Find("BuildContent/Market1").GetComponent<Button>().interactable = true;
        viewport.Find("BuildContent/Lab1").GetComponent<Button>().interactable = true;

        // confirmUI
        confirmMenu.Find("ConfirmButton").GetComponent<Button>().interactable = true;
        confirmMenu.Find("CancelButton").GetComponent<Button>().interactable = true;

        tutorialHolder.gameObject.SetActive(false);

        if (PlayerPrefs.GetInt("firstLaunch") == 3) {
            // continue normally with old save
            main.LoadGame();
        }
        else {
            // set tutorial as done
            PlayerPrefs.SetInt("firstLaunch", 1);
        }
    }
}
