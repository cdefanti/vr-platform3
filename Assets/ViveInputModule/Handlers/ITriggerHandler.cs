using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public interface ITriggerHandler : IEventSystemHandler {

    void OnTriggerPressDown(VCPointerEventData eventData);
    void OnTriggerPress(VCPointerEventData eventData);
    void OnTriggerPressUp(VCPointerEventData eventData);

    void OnTriggerTouchDown(VCPointerEventData eventData);
    void OnTriggerTouch(VCPointerEventData eventData);
    void OnTriggerTouchUp(VCPointerEventData eventData);
}
