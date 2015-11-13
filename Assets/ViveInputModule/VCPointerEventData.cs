using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using Valve.VR;

public class VCPointerEventData : PointerEventData {

    public int controllerid;

    public Vector2 touchpadAxis;
    public Vector2 triggerAxis;

    public GameObject applicationMenuPress;
    public GameObject gripPress;
    public GameObject touchpadPress;
    public GameObject triggerPress;
    public GameObject touchpadTouch;
    public GameObject triggerTouch;

    public VCPointerEventData(EventSystem eventSystem) : base(eventSystem)
    {

    }

    
	
}
