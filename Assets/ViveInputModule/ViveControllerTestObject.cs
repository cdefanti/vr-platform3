using UnityEngine;
using System.Collections;

public class ViveControllerTestObject : MonoBehaviour, IGripHandler {


    public void OnGripPressDown(VCPointerEventData eventData)
    {
        Debug.Log("OnGripPressDown, controller: " + eventData.controllerid);
    }

    public void OnGripPress(VCPointerEventData eventData)
    {
        Debug.Log("OnGripPress, controller: " + eventData.controllerid);
    }

    public void OnGripPressUp(VCPointerEventData eventData)
    {
        Debug.Log("OnGripPressUp, controller: " + eventData.controllerid);
    }
}
