using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
public interface ITouchpadHandler : IEventSystemHandler {

	void OnTouchpadPressDown(VCPointerEventData eventData);
    void OnTouchpadPress(VCPointerEventData eventData);
    void OnTouchpadPressUp(VCPointerEventData eventData);

    void OnTouchpadTouchDown(VCPointerEventData eventData);
    void OnTouchpadTouch(VCPointerEventData eventData);
    void OnTouchpadTouchUp(VCPointerEventData eventData);
}
