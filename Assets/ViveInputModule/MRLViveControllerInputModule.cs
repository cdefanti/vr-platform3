using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using Valve.VR;

public class MRLViveControllerInputModule : BaseInputModule {

    //STEAMVR VARS

    List<int> controllerIndices = new List<int>();
    Dictionary<int, VCPointerEventData> pointers = new Dictionary<int, VCPointerEventData>();

    //INPUTMODULE VARS

    //Optional tag for interaction. If left empty, will not be used.
    public string interactTag;

    //fuse stuff
    public bool fuseClickEnabled = false;
    public float fuseClickTime;
    private bool isCursorActive;
    private float _fuseTime;

    public bool hapticPulse = true;

    [HideInInspector]
    public float clickTime = 0.1f;  // Based on default time for a button to animate to Pressed.

    private VCPointerEventData pointerData;

    private void OnDeviceConnected(params object[] args)
    {
        var index = (int)args[0];

        var vr = SteamVR.instance;
        if (vr.hmd.GetTrackedDeviceClass((uint)index) != TrackedDeviceClass.Controller)
            return;

        var connected = (bool)args[1];
        if (connected)
        {
            Debug.Log(string.Format("Controller {0} connected.", index));
            PrintControllerStatus(index);
            controllerIndices.Add(index);
            SteamVR_Utils.Event.Remove("device_connected", OnDeviceConnected);
        }
        else
        {
            Debug.Log(string.Format("Controller {0} disconnected.", index));
            PrintControllerStatus(index);
            controllerIndices.Remove(index);
            SteamVR_Utils.Event.Listen("device_connected", OnDeviceConnected);
        }
    }

    //void OnDisable()
    //{
        
    //}

    void PrintControllerStatus(int index)
    {
        var device = SteamVR_Controller.Input(index);
        Debug.Log("index: " + device.index);
        Debug.Log("connected: " + device.connected);
        Debug.Log("hasTracking: " + device.hasTracking);
        Debug.Log("outOfRange: " + device.outOfRange);
        Debug.Log("calibrating: " + device.calibrating);
        Debug.Log("uninitialized: " + device.uninitialized);
        Debug.Log("pos: " + device.transform.pos);
        Debug.Log("rot: " + device.transform.rot.eulerAngles);
        Debug.Log("velocity: " + device.velocity);
        Debug.Log("angularVelocity: " + device.angularVelocity);

        var l = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Leftmost);
        var r = SteamVR_Controller.GetDeviceIndex(SteamVR_Controller.DeviceRelation.Rightmost);
        Debug.Log((l == r) ? "first" : (l == index) ? "left" : "right");
    }

    EVRButtonId[] buttonIds = new EVRButtonId[] {
		EVRButtonId.k_EButton_ApplicationMenu,
		EVRButtonId.k_EButton_Grip,
		EVRButtonId.k_EButton_SteamVR_Touchpad,
		EVRButtonId.k_EButton_SteamVR_Trigger
	};

    EVRButtonId[] axisIds = new EVRButtonId[] {
		EVRButtonId.k_EButton_SteamVR_Touchpad,
		EVRButtonId.k_EButton_SteamVR_Trigger
	};

    //public override bool ShouldActivateModule()
    //{
    //    //Debug.Log("ShouldActivateModule");
    //    if (!base.ShouldActivateModule() || controllerIndice == -1)
    //    {
    //        return false;
    //    }

    //    return true;
    //}

    public override void ActivateModule()
    {
        base.ActivateModule();
        SteamVR_Utils.Event.Listen("device_connected", OnDeviceConnected);
    }

    public override void DeactivateModule()
    {
        base.DeactivateModule();
        if (pointerData != null)
        {
            //HandlePendingClick();
            HandlePointerExitAndEnter(pointerData, null);
            pointerData = null;
        }
        eventSystem.SetSelectedGameObject(null, GetBaseEventData());
        SteamVR_Utils.Event.Remove("device_connected", OnDeviceConnected);
    }

    public override void Process()
    {
        print("Processing");
        foreach (var index in controllerIndices)
        {
            CastRayFromController(index);
            UpdateCurrentObject();
            //PlaceCursor();
            //HandleFuseController(); //OPTIONAL - HANDLE FUSE FOR CONTROLLERS?
            HandleAxesAndButtons(index);
        }
        
    }

    private void CastRayFromController(int controllerIndice)
    {
        
        while (!pointers.TryGetValue(controllerIndice, out pointerData)) {
            pointers.Add(controllerIndice, new VCPointerEventData(eventSystem));
        }

        pointerData.Reset();
        pointerData.position = transform.position; //?????

        eventSystem.RaycastAll(pointerData, m_RaycastResultCache);

        

        List<RaycastResult> removeResult = new List<RaycastResult>();
        foreach (RaycastResult rayResult in m_RaycastResultCache)
        {
            if (interactTag != null && interactTag.Length > 1 && !rayResult.gameObject.tag.Equals(interactTag))
            {
                removeResult.Add(rayResult);
            }
        }

        foreach (RaycastResult rayResult in removeResult)
        {
            m_RaycastResultCache.Remove(rayResult);
        }

        //TO-DO: FIND OUT HOW TO GET SPECIFIC RAYCAST FOR THIS OBJECT.
        pointerData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
        m_RaycastResultCache.Clear();

        //Debug.Log(pointerData.pointerCurrentRaycast.gameObject.name);
    }

    private void UpdateCurrentObject()
    {
        //Send enter event and update the highlight.
        var go = pointerData.pointerCurrentRaycast.gameObject;
        HandlePointerExitAndEnter(pointerData, go);
        //Update the current selection, or clear if it no longer the current object.
        var selected = ExecuteEvents.GetEventHandler<ISelectHandler>(go);
        if (selected == eventSystem.currentSelectedGameObject)
        {
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, GetBaseEventData(), ExecuteEvents.updateSelectedHandler);
        }
        else
        {
            eventSystem.SetSelectedGameObject(null, pointerData);
            //_fuseTime = 0; //??
        }
    }

    //private void HandleFuseController()
    //{
    //    if (!isCursorActive || !fuseClickEnabled)
    //    {
    //        return;
    //    }
    //    _fuseTime += Time.unscaledDeltaTime;

    //    if (_fuseTime >= fuseClickTime)
    //    {
    //        _fuseTime = 0;
    //    }
    //}

    private void HandleAxesAndButtons(int controllerIndice)
    {
        //HANDLE AXES
        pointerData.touchpadAxis = SteamVR_Controller.Input(controllerIndice).GetAxis(axisIds[0]);
        pointerData.triggerAxis  = SteamVR_Controller.Input(controllerIndice).GetAxis(axisIds[1]);
        pointerData.controllerid = controllerIndice;

        //HANDLE PRESSES
        foreach (var buttonId in buttonIds)
        {
            if (SteamVR_Controller.Input(controllerIndice).GetPressDown(buttonId))
                ExecutePressDown(buttonId);

            if (SteamVR_Controller.Input(controllerIndice).GetPress(buttonId))
                ExecutePress(buttonId);

            if (SteamVR_Controller.Input(controllerIndice).GetPressUp(buttonId))
                ExecutePressUp(buttonId);
        }

        foreach (var buttonId in axisIds)
        {
            if (SteamVR_Controller.Input(controllerIndice).GetTouchDown(buttonId))
                ExecuteTouchDown(buttonId);

            if (SteamVR_Controller.Input(controllerIndice).GetTouch(buttonId))
                ExecuteTouch(buttonId);

            if (SteamVR_Controller.Input(controllerIndice).GetTouchUp(buttonId))
                ExecuteTouch(buttonId);
        }
    }


    private void ExecutePressDown(EVRButtonId id)
    {
        switch (id)
        {
            case EVRButtonId.k_EButton_ApplicationMenu:
                pointerData.applicationMenuPress = pointerData.pointerCurrentRaycast.gameObject;
                ExecuteEvents.Execute<IApplicationMenuHandler>(pointerData.applicationMenuPress, pointerData,
                    (x, y) => x.OnApplicationMenuPressDown(pointerData));
                break;
            case EVRButtonId.k_EButton_Grip:
                pointerData.gripPress = pointerData.pointerCurrentRaycast.gameObject;
                ExecuteEvents.Execute<IGripHandler>(pointerData.gripPress, pointerData,
                    (x, y) => x.OnGripPressDown(pointerData));
                break;
            case EVRButtonId.k_EButton_SteamVR_Touchpad:
                pointerData.touchpadPress = pointerData.pointerCurrentRaycast.gameObject;
                ExecuteEvents.Execute<ITouchpadHandler>(pointerData.touchpadPress, pointerData,
                    (x, y) => x.OnTouchpadPressDown(pointerData));
                break;
            case EVRButtonId.k_EButton_SteamVR_Trigger:
                pointerData.triggerPress = pointerData.pointerCurrentRaycast.gameObject;
                ExecuteEvents.Execute<ITriggerHandler>(pointerData.triggerPress, pointerData,
                    (x, y) => x.OnTriggerPressDown(pointerData));
                break;
        }
    }

    private void ExecutePress(EVRButtonId id)
    {
        switch (id)
        {
            case EVRButtonId.k_EButton_ApplicationMenu:
                ExecuteEvents.Execute<IApplicationMenuHandler>(pointerData.applicationMenuPress, pointerData,
                    (x, y) => x.OnApplicationMenuPress(pointerData));
                break;
            case EVRButtonId.k_EButton_Grip:
                ExecuteEvents.Execute<IGripHandler>(pointerData.gripPress, pointerData,
                    (x, y) => x.OnGripPress(pointerData));
                break;
            case EVRButtonId.k_EButton_SteamVR_Touchpad:
                ExecuteEvents.Execute<ITouchpadHandler>(pointerData.touchpadPress, pointerData,
                    (x, y) => x.OnTouchpadPress(pointerData));
                break;
            case EVRButtonId.k_EButton_SteamVR_Trigger:
                ExecuteEvents.Execute<ITriggerHandler>(pointerData.triggerPress, pointerData,
                    (x, y) => x.OnTriggerPress(pointerData));
                break;
        }
    }

    private void ExecutePressUp(EVRButtonId id)
    {
        switch (id)
        {
            case EVRButtonId.k_EButton_ApplicationMenu:
                ExecuteEvents.Execute<IApplicationMenuHandler>(pointerData.applicationMenuPress, pointerData,
                    (x, y) => x.OnApplicationMenuPressUp(pointerData));
                pointerData.applicationMenuPress = null;
                break;
            case EVRButtonId.k_EButton_Grip:
                ExecuteEvents.Execute<IGripHandler>(pointerData.gripPress, pointerData,
                    (x, y) => x.OnGripPressUp(pointerData));
                pointerData.gripPress = null;
                break;
            case EVRButtonId.k_EButton_SteamVR_Touchpad:
                ExecuteEvents.Execute<ITouchpadHandler>(pointerData.touchpadPress, pointerData,
                    (x, y) => x.OnTouchpadPressUp(pointerData));
                pointerData.touchpadPress = null;
                break;
            case EVRButtonId.k_EButton_SteamVR_Trigger:
                ExecuteEvents.Execute<ITriggerHandler>(pointerData.triggerPress, pointerData,
                    (x, y) => x.OnTriggerPressUp(pointerData));
                pointerData.triggerPress = null;
                break;
        }
    }

    private void ExecuteTouchDown(EVRButtonId id)
    {
        switch (id)
        {
            case EVRButtonId.k_EButton_SteamVR_Touchpad:
                pointerData.touchpadTouch = pointerData.pointerCurrentRaycast.gameObject;
                ExecuteEvents.Execute<ITouchpadHandler>(pointerData.touchpadTouch, pointerData,
                    (x, y) => x.OnTouchpadTouchDown(pointerData));
                break;
            case EVRButtonId.k_EButton_SteamVR_Trigger:
                pointerData.triggerTouch = pointerData.pointerCurrentRaycast.gameObject;
                ExecuteEvents.Execute<ITriggerHandler>(pointerData.triggerTouch, pointerData,
                    (x, y) => x.OnTriggerTouchDown(pointerData));
                break;
        }
    }

    private void ExecuteTouch(EVRButtonId id)
    {
        switch (id)
        {
            case EVRButtonId.k_EButton_SteamVR_Touchpad:
                ExecuteEvents.Execute<ITouchpadHandler>(pointerData.touchpadTouch, pointerData,
                    (x, y) => x.OnTouchpadTouch(pointerData));
                break;
            case EVRButtonId.k_EButton_SteamVR_Trigger:
                ExecuteEvents.Execute<ITriggerHandler>(pointerData.triggerTouch, pointerData,
                    (x, y) => x.OnTriggerTouch(pointerData));
                break;
        }
    }

    private void ExecuteTouchUp(EVRButtonId id)
    {
        switch (id)
        {
            case EVRButtonId.k_EButton_SteamVR_Touchpad:
                ExecuteEvents.Execute<ITouchpadHandler>(pointerData.touchpadTouch, pointerData,
                    (x, y) => x.OnTouchpadTouchUp(pointerData));
                pointerData.touchpadTouch = null;
                break;
            case EVRButtonId.k_EButton_SteamVR_Trigger:
                ExecuteEvents.Execute<ITriggerHandler>(pointerData.triggerTouch, pointerData,
                    (x, y) => x.OnTriggerTouchUp(pointerData));
                pointerData.triggerTouch = null;
                break;
        }
    }
}
