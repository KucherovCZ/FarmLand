using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Achievement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    private GameObject description;

    private void Start() {
        description = gameObject.GetComponentInParent<Transform>().Find("Description").gameObject;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        description.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        description.SetActive(false);
    }

}
