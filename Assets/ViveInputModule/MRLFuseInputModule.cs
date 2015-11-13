using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;


public class MRLFuseInputModule : BaseInputModule {

    public bool fuseClickEnabled;
    public float fuseClickTime;

    private float _fuseTime;

    //Cursors for UI
    public bool showCursor = true;
    public bool scaleCursorSize = true;

    //public GameObject fuseCursor;
    //public GameObject fuseInactiveCursor;

    public GameObject cursor;
    private bool isCursorActive;
   
    //Optional tag for interaction. If left empty, will not be used.
    public string interactTag;

    /// Time in seconds between the pointer down and up events sent by a magnet click.
    /// Allows time for the UI elements to make their state transitions.
    [HideInInspector]
    public float clickTime = 0.1f;  // Based on default time for a button to animate to Pressed.

    /// The pixel through which to cast rays, in viewport coordinates.  Generally, the center
    /// pixel is best, assuming a monoscopic camera is selected as the `Canvas`' event camera.
    [HideInInspector]
    public Vector3 hotspot = new Vector2(0.5f, 0.5f);
    private PointerEventData pointerData;

    public override bool ShouldActivateModule()
    {
        if (!base.ShouldActivateModule())
        {
            return false;
        }

        //TO-DO: NON-HARDWARE SPECIFIC ENABLEMENT
        return true;
    }

    public override void ActivateModule()
    {
        base.ActivateModule();
        //SetActiveCursor();
        if (!IsPointerOverGameObject(0))
        {
            cursor.SetActive(false);
        }
    }

    public override void DeactivateModule()
    {
        base.DeactivateModule();
        if (pointerData != null)
        {
            HandlePendingClick();
            HandlePointerExitAndEnter(pointerData, null);
            pointerData = null;
        }
        eventSystem.SetSelectedGameObject(null, GetBaseEventData());
        if (cursor != null)
        {
            cursor.SetActive(false);
        }
    }

    public override bool IsPointerOverGameObject(int pointerId)
    {
        bool over = pointerData != null && pointerData.pointerEnter != null;
        return over;
    }

    public override void Process()
    {
        CastRayFromGaze();
        UpdateCurrentObject();
        PlaceCursor();
        HandleFuseGaze();
        HandlePendingClick();
        HandleTrigger();
    }

    private void CastRayFromGaze()
    {
        pointerData = (pointerData == null ? new PointerEventData(eventSystem) : pointerData);

        pointerData.Reset();
        pointerData.position = new Vector3(hotspot.x * Screen.width, hotspot.y * Screen.height);
        eventSystem.RaycastAll(pointerData, m_RaycastResultCache);
        //List<RaycastResult> removeResult = new List<RaycastResult>();
        //foreach (RaycastResult rayResult in m_RaycastResultCache)
        //{
        //    if (interactTag != null && interactTag.Length > 1 && !rayResult.gameObject.tag.Equals(interactTag))
        //    {
        //        removeResult.Add(rayResult);
        //    }
        //}

        //foreach (RaycastResult rayResult in removeResult)
        //{
        //    m_RaycastResultCache.Remove(rayResult);
        //}

        //TO-DO: FIND OUT HOW TO GET SPECIFIC RAYCAST FOR THIS OBJECT.
        pointerData.pointerCurrentRaycast = FindFirstRaycast(m_RaycastResultCache);
        m_RaycastResultCache.Clear();
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
            _fuseTime = 0; //??
        }
    }

    private void PlaceCursor()
    {
        if (cursor == null)
        {
            return;
        }
        var go = pointerData.pointerCurrentRaycast.gameObject;
        Camera cam = pointerData.enterEventCamera;  // Will be null for overlay hits.
        cursor.SetActive(go != null && cam != null && showCursor);
        if (cursor.activeInHierarchy)
        {
            isCursorActive = true;
            // Note: rays through screen start at near clipping plane.
            float dist = pointerData.pointerCurrentRaycast.distance + cam.nearClipPlane;
            Debug.Log(dist);
            cursor.transform.position = cam.transform.position + cam.transform.forward * dist;
            if (scaleCursorSize)
            {
                cursor.transform.localScale = Vector3.one * dist;
            }
        }
        else
        {
            isCursorActive = false;
            cursor.SetActive(false);
        }
    }

    private void HandleFuseGaze()
    {
        if (!isCursorActive || !fuseClickEnabled)
        {
            return;
        }
        _fuseTime += Time.unscaledDeltaTime;

        if (_fuseTime >= fuseClickTime)
        {
            _fuseTime = 0;
            HandleTrigger();
        }
    }

    private void HandlePendingClick()
    {
        if (!pointerData.eligibleForClick)
        {
            return;
        }
        var go = pointerData.pointerCurrentRaycast.gameObject;

        // Send pointer up and click events.
        ExecuteEvents.Execute(pointerData.pointerPress, pointerData, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(pointerData.pointerPress, pointerData, ExecuteEvents.pointerClickHandler);

        // Clear the click state.
        pointerData.pointerPress = null;
        pointerData.rawPointerPress = null;
        pointerData.eligibleForClick = false;
        pointerData.clickCount = 0;
        //pointerData.pointerDrag = null;
        //pointerData.dragging = false;
    }

    private void HandleTrigger()
    {
        var go = pointerData.pointerCurrentRaycast.gameObject;

        // Send pointer down event.
        pointerData.pressPosition = pointerData.position;
        pointerData.pointerPressRaycast = pointerData.pointerCurrentRaycast;
        pointerData.pointerPress =
          ExecuteEvents.ExecuteHierarchy(go, pointerData, ExecuteEvents.pointerDownHandler)
            ?? ExecuteEvents.GetEventHandler<IPointerClickHandler>(go);

        // Save the drag handler as well
        //pointerData.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(go);
        //if (pointerData.pointerDrag != null && !Cardboard.SDK.TapIsTrigger)
        //{
        //    ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.initializePotentialDrag);
        //}

        // Save the pending click state.
        pointerData.rawPointerPress = go;
        pointerData.eligibleForClick = true;
        pointerData.delta = Vector2.zero;
        //pointerData.dragging = false;
        //pointerData.useDragThreshold = true;
        pointerData.clickCount = 1;
        pointerData.clickTime = Time.unscaledTime;
    }
}
