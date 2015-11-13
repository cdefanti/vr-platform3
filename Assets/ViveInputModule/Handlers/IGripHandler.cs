using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public interface IGripHandler : IEventSystemHandler {

    void OnGripPressDown(VCPointerEventData eventData);
    void OnGripPress(VCPointerEventData eventData);
    void OnGripPressUp(VCPointerEventData eventData);
}
