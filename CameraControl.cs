using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CameraControl : MonoBehaviour {
    public Main main;

    public static bool canMove = false;
    public static bool cinematicAllowed = false;

    // change these from editor
    public float minZoom = 2f;
    public float maxZoom = 10f;
    public float cameraMinY = 1f;
    public float cameraMaxY = 10f;
    public float cameraMinX = -14f;
    public float cameraMaxX = 14f;

    private Vector3 startPosition = Vector3.zero;
    private Vector3 startPosition2 = Vector3.zero;
    private Vector3 endPosition = Vector3.zero;
    private Vector3 endPosition2 = Vector3.zero;

    private Vector3 change = Vector3.zero;
    private Vector3 newCamPos = Vector3.zero;

    bool switch1 = false, switch2 = false;

    private void Start() {
        main = GetComponent<Main>();
    }

    private void Update() {
        // will work only if canMove == true
        // one touch   -- moving
        // two touches -- zooming

        if (canMove) {

            // MOVE - when only one touch is active and NOT OVER UI
            if (Input.touchCount == 1) {
                if (Input.touches[0].phase == TouchPhase.Began) {
                    startPosition = TouchPosition(0);
                    return;
                }

                if (EventSystem.current.IsPointerOverGameObject(0)) {
                    switch1 = true;
                    return;
                }
                else if (switch1) {
                    startPosition = WorldTouchPosition(0);
                    switch1 = false;
                    return;
                }

                // get new camera position
                endPosition = WorldTouchPosition(0);
                change = (endPosition - startPosition) * -1;
                change.z = 0;

                // this is here because camera sometimes jumped kilometers far away
                if (change.x > 4) change.x = 0;
                if (change.y > 4) change.y = 0;

                newCamPos = Camera.main.transform.position + change;
                newCamPos.x = Mathf.Clamp(newCamPos.x, cameraMinX, cameraMaxX);
                newCamPos.y = Mathf.Clamp(newCamPos.y, cameraMinY, cameraMaxY);
                Camera.main.transform.position = newCamPos;

                startPosition = WorldTouchPosition(0);
            }

            // ZOOM - when two touches are active and both are NOT over UI
            else if (Input.touchCount == 2) {
                if (Input.touches[1].phase == TouchPhase.Began) {
                    startPosition = TouchPosition(0);
                    startPosition2 = TouchPosition(1);
                    return;
                }

                if (EventSystem.current.IsPointerOverGameObject(0) || EventSystem.current.IsPointerOverGameObject(1)) {
                    switch2 = true;
                    return;
                }
                else if (switch2) {
                    switch2 = false;
                    startPosition = TouchPosition(0);
                    startPosition2 = TouchPosition(1);
                }
                else {
                    endPosition = TouchPosition(0);
                    endPosition2 = TouchPosition(1);

                    // calculate distance bewteen touches
                    float startDistance = Vector3.Distance(startPosition, startPosition2);
                    float endDistance = Vector3.Distance(endPosition, endPosition2);
                    float result = startDistance - endDistance;

                    startPosition = endPosition;
                    startPosition2 = endPosition2;

                    Zoom(result * 0.04f);
                }
            }
        }
    }

    public void AllowMovement(bool status) {
        canMove = status;
        cinematicAllowed = !status;

        //startPosition = WorldTouchPosition(0);
        startPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    private Vector3 WorldTouchPosition(int index) {
        return Camera.main.ScreenToWorldPoint(Input.touches[index].position);
    }

    private Vector3 TouchPosition(int index) {
        return Input.touches[index].position;
    }

    private void Zoom(float increment) {
        Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize + increment / 4, minZoom, maxZoom);
        main.UpdateUnlockUISize();
    }
    
}

