using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
public interface IApplicationMenuHandler : IEventSystemHandler {

    void OnApplicationMenuPressDown(VCPointerEventData eventData);
    void OnApplicationMenuPress(VCPointerEventData eventData);
    void OnApplicationMenuPressUp(VCPointerEventData eventData);
}
