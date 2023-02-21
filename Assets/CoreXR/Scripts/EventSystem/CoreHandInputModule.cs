using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Coretronic.Reality.UI;
using Coretronic.Reality.Hand;

/** 
 * \brief EventSystems provides a set of APIs to cowork with "UnityEngine.EventSystems".
 */
namespace Coretronic.Reality.EventSystems
{
    /// <summary>
    /// XR extension of PointerInputModule which supports gaze and controller pointing.
    /// </summary>
    public class CoreHandInputModule : PointerInputModule
    {
        [SerializeField, Tooltip("The input of left hand or right hand")]
        private CoreHand m_Hand;

        [SerializeField]
        private CoreCursor m_Cursor;

        [SerializeField]
        private CoreMenu m_Menu;

        [SerializeField, Tooltip("The minimal control time of calling up the menu.")]
        private float m_MenuMinimalTime = 1.0f;

        [SerializeField, Tooltip("Gesture to act as gaze click")]
        private Gesture.Types clickGesture = Gesture.Types.Pinch;

        [Header("Physics")]
        [SerializeField, Tooltip("Perform the sphere cast to determine correct depth for gaze pointer")]
        private bool performSphereCastForGazepointer = true;

        [SerializeField, Tooltip("The radius of the physics sphere")]
        private float m_SpherecastRadius = 1.0f; 

        [Header("Dragging")]
        [Tooltip("Minimum pointer movement in degrees to start dragging")]
        private float angleDragThreshold = 1;      
        
        private bool handIsActivate = false;
        private bool rayIsActivate = false;
        private bool menuIsCallable = false;
        private float menuCumulativeTime = 0;
        private bool lastGestureIsClick = false;
        private CoreHMD coreHMD;
        private Vector3 lastHandRayPos = Vector3.zero;
        private Vector3 cursorPosition = Vector3.zero;
        private Vector3 cursorLastPosition = Vector3.zero;

        /** \brief Awake is called when the script instance is being loaded. */
        protected override void Awake()
        {
            coreHMD = FindObjectOfType<CoreHMD>();
        }

        // The following region contains code exactly the same as the implementation
        // of StandaloneInputModule. It is copied here rather than inheriting from StandaloneInputModule
        // because most of StandaloneInputModule is private so it isn't possible to easily derive from.
        // Future changes from Unity to StandaloneInputModule will make it possible for this class to
        // derive from StandaloneInputModule instead of PointerInput module.
        //
        // The following functions are not present in the following region since they have modified
        // versions in the next region:
        // Process
        // ProcessMouseEvent
        // UseMouse

        /** 
         * \brief The default constructor.
         */
        protected CoreHandInputModule()
        {}

        /** 
         * \brief Update the internal state of the Module.
         */
        public override void UpdateModule()
        {
        }

        /** 
         * \brief Check to see if the module is supported.
         * 
         * @returns A boolean to check whether the module is supported.
         */
        public override bool IsModuleSupported()
        {
            // Check for mouse presence instead of whether touch is supported,
            // as you can connect mouse to a tablet and in that case we'd want
            // to use StandaloneInputModule for non-touch input events.
            // return m_AllowActivationOnMobileDevice || Input.mousePresent;
            bool shouldSupport = m_Hand;
            return shouldSupport;
        }

        /** 
         * \brief Should the module be activated.
         * 
         * @returns A boolean to check whether the module should be activated.
         */
        public override bool ShouldActivateModule()
        {
            if (!base.ShouldActivateModule())
                return false;
            
            handIsActivate = m_Hand.isActiveAndEnabled;
            
            if (handIsActivate)
            {
                menuIsCallable = m_Hand.gesture.MatchConditions(Gesture.Conditions.PalmFacingCamera);
                rayIsActivate = !menuIsCallable;
            }
            else
            {
                menuIsCallable = false;
                rayIsActivate = false;
            }
            if (rayIsActivate)
                m_Cursor.OnFocusAcquired();
            else
                m_Cursor.OnFocusLost();
            if (!menuIsCallable) 
                menuCumulativeTime = 0;
            
            return handIsActivate;
        }

        /** 
         * \brief Called when the module is activated.
         */
        public override void ActivateModule()
        {
            base.ActivateModule();
            var toSelect = eventSystem.currentSelectedGameObject;

            if (toSelect == null)
                toSelect = eventSystem.firstSelectedGameObject;

            eventSystem.SetSelectedGameObject(toSelect, GetBaseEventData());
        }

        /** 
         * \brief Called when the module is deactivated. 
         */
        public override void DeactivateModule()
        {
            base.DeactivateModule();
            ClearSelection();
        }

        private bool SendUpdateEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return false;

            var data = GetBaseEventData();
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
            return data.used;
        }

        /// <summary>
        /// Process the current mouse press.
        /// </summary>
        private void ProcessMousePress(MouseButtonEventData data)
        {
            var pointerEvent = data.buttonData;
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if (data.PressedThisFrame())
            {
                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pressPosition = pointerEvent.position;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;
                DeselectIfSelectionChanged(currentOverGo, pointerEvent);
                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);
                
                // didnt find a press handler... search for a click handler
                if (newPressed == null)
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                float time = Time.unscaledTime;

                if (newPressed == pointerEvent.lastPress)
                {
                    var diffTime = time - pointerEvent.clickTime;
                    if (diffTime < 0.3f)
                        ++pointerEvent.clickCount;
                    else
                        pointerEvent.clickCount = 1;

                    pointerEvent.clickTime = time;
                }
                else
                {
                    pointerEvent.clickCount = 1;
                }

                pointerEvent.pointerPress = newPressed;
                pointerEvent.rawPointerPress = currentOverGo;

                pointerEvent.clickTime = time;

                // Save the drag handler as well
                pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (pointerEvent.pointerDrag != null)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);
            }

            // PointerUp notification
            if (data.ReleasedThisFrame())
            {
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);
                // see if we mouse up on the same element that we clicked on...
                var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // PointerClick and Drop events
                if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
                {
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
                }
                else if (pointerEvent.pointerDrag != null)
                {
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
                }

                pointerEvent.eligibleForClick = false;
                pointerEvent.pointerPress = null;
                pointerEvent.rawPointerPress = null;

                if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

                pointerEvent.dragging = false;
                pointerEvent.pointerDrag = null;

                // redo pointer enter / exit to refresh state
                // so that if we moused over somethign that ignored it before
                // due to having pressed on something else
                // it now gets it.
                if (currentOverGo != pointerEvent.pointerEnter)
                {
                    HandlePointerExitAndEnter(pointerEvent, null);
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                }
            }
        }

        /// <summary>
        /// Process all mouse events. This is the same as the StandaloneInputModule version except that
        /// it takes MouseState as a parameter, allowing it to be used for both Gaze and Mouse
        /// pointerss.
        /// </summary>
        private void ProcessMouseEvent(MouseState mouseData)
        {
            if (!handIsActivate) return;
            var pressed = mouseData.AnyPressesThisFrame();
            var released = mouseData.AnyReleasesThisFrame();
            var leftButtonData = mouseData.GetButtonState(PointerEventData.InputButton.Left).eventData;

            if (!UseMouse(pressed, released, leftButtonData.buttonData)) return;

            // Process the first mouse button fully
            ProcessMousePress(leftButtonData);
            ProcessMove(leftButtonData.buttonData);
            ProcessDrag(leftButtonData.buttonData);

            // Now process right / middle clicks
            ProcessMousePress(mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData);
            ProcessDrag(mouseData.GetButtonState(PointerEventData.InputButton.Right).eventData.buttonData);
            ProcessMousePress(mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData);
            ProcessDrag(mouseData.GetButtonState(PointerEventData.InputButton.Middle).eventData.buttonData);

            if (!Mathf.Approximately(leftButtonData.buttonData.scrollDelta.sqrMagnitude, 0.0f))
            {
                var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(leftButtonData.buttonData.pointerCurrentRaycast.gameObject);
                ExecuteEvents.ExecuteHierarchy(scrollHandler, leftButtonData.buttonData, ExecuteEvents.scrollHandler);
            }
        }

        /// <summary>
        /// Process this InputModule. Same as the StandaloneInputModule version, except that it calls
        /// ProcessMouseEvent twice, once for gaze pointers, and once for mouse pointers.
        /// </summary>
        public override void Process()
        {
            bool usedEvent = SendUpdateEventToSelectedObject();
            cursorLastPosition = cursorPosition;
            ProcessMouseEvent(GetGazePointerData());
            if (menuIsCallable) CallMenu();
            lastHandRayPos = handIsActivate ? m_Hand.handRay.origin : Vector3.zero;
        }

        /// <summary>
        /// Decide if mouse events need to be processed this frame. Same as StandloneInputModule except
        /// that the IsPointerMoving method from this class is used, instead of the method on PointerEventData
        /// </summary>
       private static bool UseMouse(bool pressed, bool released, PointerEventData pointerData)
        {
            return pressed || released || IsPointerMoving(pointerData) || pointerData.IsScrolling();
        }


        /// <summary>
        /// Convenience function for cloning PointerEventData
        /// </summary>
        /// <param name="from">Copy this value</param>
        /// <param name="to">to this object</param>
        protected void CopyFromTo(CorePointerEventData @from, CorePointerEventData @to)
        {
            @to.position = @from.position;
            @to.delta = @from.delta;
            @to.scrollDelta = @from.scrollDelta;
            @to.pointerCurrentRaycast = @from.pointerCurrentRaycast;
            @to.pointerEnter = @from.pointerEnter;
            @to.worldSpaceRay = @from.worldSpaceRay;
        }

        /// <summary>
        /// Convenience function for cloning PointerEventData
        /// </summary>
        /// <param name="from">Copy this value</param>
        /// <param name="to">to this object</param>
        protected new void CopyFromTo(PointerEventData @from, PointerEventData @to)
        {
            @to.position = @from.position;
            @to.delta = @from.delta;
            @to.scrollDelta = @from.scrollDelta;
            @to.pointerCurrentRaycast = @from.pointerCurrentRaycast;
            @to.pointerEnter = @from.pointerEnter;
        }

        private void CallMenu()
        {
            if (m_Menu == null) return;
            // if (m_Menu.isActiveAndEnabled) return;

            var pressed = m_Hand.GetKeepingGesture(clickGesture);

            if (pressed)
            {
                if (menuCumulativeTime > m_MenuMinimalTime)
                {
                    if (m_Menu.IsFocus())
                        m_Menu.OnFocusLost();
                    else
                        m_Menu.OnFocusAcquired();
                    menuCumulativeTime = -1;
                }
                else
                    menuCumulativeTime += Time.unscaledDeltaTime;
            }
            else
            {
                menuCumulativeTime = 0;
            }
        }


        // In the following region we extend the PointerEventData system implemented in PointerInputModule
        // We define an additional dictionary for ray(e.g. gaze) based pointers. Mouse pointers still use the dictionary
        // in PointerInputModule

        /** \brief Pool for CorePointerEventData for ray based pointers. */
        protected Dictionary<int, CorePointerEventData> m_XRRayPointerData = new Dictionary<int, CorePointerEventData>();

        /** 
         * \brief Get CorePointerEventData.
         * @param[in]  id     Get CorePointerEventData by ID.
         * @param[out] data   CorePointerEventData.
         * @param[in]  create To create new CorePointerEventData.
         */
        protected bool GetPointerData(int id, out CorePointerEventData data, bool create)
        {
            if (!m_XRRayPointerData.TryGetValue(id, out data) && create)
            {
                data = new CorePointerEventData(eventSystem)
                {
                    pointerId = id,
                };

                m_XRRayPointerData.Add(id, data);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clear pointer state for both types of pointer
        /// </summary>
        protected new void ClearSelection()
        {
            var baseEventData = GetBaseEventData();

            foreach (var pointer in m_PointerData.Values)
            {
                // clear all selection
                HandlePointerExitAndEnter(pointer, null);
            }
            foreach (var pointer in m_XRRayPointerData.Values)
            {
                // clear all selection
                HandlePointerExitAndEnter(pointer, null);
            }

            m_PointerData.Clear();
            eventSystem.SetSelectedGameObject(null, baseEventData);
        }

        /// <summary>
        /// For RectTransform, calculate it's normal in world space
        /// </summary>
        static Vector3 GetRectTransformNormal(RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Vector3 BottomEdge = corners[3] - corners[0];
            Vector3 LeftEdge = corners[1] - corners[0];
            return Vector3.Cross(BottomEdge, LeftEdge).normalized;
        }

        private readonly MouseState m_MouseState = new MouseState();


        // The following 2 functions are equivalent to PointerInputModule.GetMousePointerEventData but are customized to
        // get data for ray pointers and canvas mouse pointers.

        /// <summary>
        /// State for a pointer controlled by a world space ray. E.g. gaze pointer
        /// </summary>
        /// <returns></returns>
        virtual protected MouseState GetGazePointerData()
        {
            // Get the CorePointerEventData reference
            CorePointerEventData leftData;
            GetPointerData(kMouseLeftId, out leftData, true );
            leftData.Reset();
            //Populate some default values
            leftData.button = PointerEventData.InputButton.Left;
            leftData.useDragThreshold = true;

            if (rayIsActivate)
            {
                var handRay = m_Hand.handRay;
                //Now set the world space ray. This ray is what the user uses to point at UI elements
                leftData.worldSpaceRay = handRay;
                leftData.scrollDelta = Vector2.zero;
                // Perform raycast to find intersections with world
                eventSystem.RaycastAll(leftData, m_RaycastResultCache);
                var raycast = FindFirstRaycast(m_RaycastResultCache);
                leftData.pointerCurrentRaycast = raycast;
                m_RaycastResultCache.Clear();
                m_Cursor.SetCursorRay(handRay.origin, handRay.direction);
                // 3 is default depth value in CoreGazePointer
                cursorPosition = handRay.origin + handRay.direction * 3;
                leftData.position = coreHMD.CenterEyeCamera.WorldToScreenPoint(cursorPosition);
                

                CoreGraphicRaycaster graphicRaycaster = raycast.module as CoreGraphicRaycaster;
                // We're only interested in intersections from CoreGraphicRaycaster
                if (graphicRaycaster)
                {
                    // The Unity UI system expects event data to have a screen position
                    // so even though this raycast came from a world space ray we must get a screen
                    // space position for the camera attached to this raycaster for compatability
                    leftData.position = graphicRaycaster.GetScreenPosition(raycast);
                    cursorPosition = raycast.worldPosition;

                    // Find the world position and normal the Graphic the ray intersected
                    RectTransform graphicRect = raycast.gameObject.GetComponent<RectTransform>();
                    if (graphicRect != null)
                    {
                        // Set are gaze indicator with this world position and normal
                        Vector3 worldPos = raycast.worldPosition;
                        Vector3 normal = GetRectTransformNormal(graphicRect);
                        m_Cursor.SetCursorStartDest(handRay.origin, worldPos, normal);
                    }
                }

                // Now process physical raycast intersections
                CorePhysicsRaycaster physicsRaycaster = raycast.module as CorePhysicsRaycaster;
                if (physicsRaycaster)
                {
                    Vector3 position = raycast.worldPosition;
                    cursorPosition = raycast.worldPosition;

                    if (performSphereCastForGazepointer)
                    {
                        // Here we cast a sphere into the scene rather than a ray. This gives a more accurate depth
                        // for positioning a circular gaze pointer
                        List<RaycastResult> results = new List<RaycastResult>();
                        physicsRaycaster.Spherecast(leftData, results, m_SpherecastRadius);
                        if (results.Count > 0 && results[0].distance < raycast.distance)
                        {
                            position = results[0].worldPosition;
                        }
                    }

                    leftData.position = physicsRaycaster.GetScreenPosition(raycast);
                    m_Cursor.SetCursorStartDest(handRay.origin, position, raycast.worldNormal);
                }
            }
            else
            {
                leftData.position = Vector2.zero;
                cursorPosition = Vector3.zero;
            }
            
            // Stick default data values in right and middle slots for compatability
            // copy the apropriate data into right and middle slots
            CorePointerEventData rightData;
            GetPointerData(kMouseRightId, out rightData, true );
            CopyFromTo(leftData, rightData);
            rightData.button = PointerEventData.InputButton.Right;
            
            CorePointerEventData middleData;
            GetPointerData(kMouseMiddleId, out middleData, true );
            CopyFromTo(leftData, middleData);
            middleData.button = PointerEventData.InputButton.Middle;

            m_MouseState.SetButtonState(PointerEventData.InputButton.Left, GetGazeButtonState(), leftData);
            m_MouseState.SetButtonState(PointerEventData.InputButton.Right, PointerEventData.FramePressState.NotChanged, rightData);
            m_MouseState.SetButtonState(PointerEventData.InputButton.Middle, PointerEventData.FramePressState.NotChanged, middleData);
            return m_MouseState;
        }

        /// <summary>
        /// New version of ShouldStartDrag implemented first in PointerInputModule. This version differs in that
        /// for ray based pointers it makes a decision about whether a drag should start based on the angular change
        /// the pointer has made so far, as seen from the camera. This also works when the world space ray is
        /// translated rather than rotated, since the beginning and end of the movement are considered as angle from
        /// the same point.
        /// </summary>
        private bool ShouldStartDrag(PointerEventData pointerEvent)
        {
            if (pointerEvent == null) return false;
            if (!pointerEvent.useDragThreshold) return true;
            if (!pointerEvent.IsCoreXRPointer())
            {
                 // Same as original behaviour for canvas based pointers
                return (pointerEvent.pressPosition - pointerEvent.position).sqrMagnitude >= eventSystem.pixelDragThreshold * eventSystem.pixelDragThreshold;
            }
            else
            {
                // When it's not a screen space pointer we have to look at the angle it moved rather than the pixels distance
                // For gaze based pointing screen-space distance moved will always be near 0
                if (pointerEvent.pointerPressRaycast.isValid && pointerEvent.pointerCurrentRaycast.isValid)
                {
                    Vector3 cameraPos = pointerEvent.pressEventCamera.transform.position;
                    Vector3 pressDir = (pointerEvent.pointerPressRaycast.worldPosition - cameraPos).normalized;
                    Vector3 currentDir = (pointerEvent.pointerCurrentRaycast.worldPosition - cameraPos).normalized;
                    var currentPressDot = Vector3.Dot(pressDir, currentDir);
                    var threshold = Mathf.Cos(Mathf.Deg2Rad * (angleDragThreshold));
                    return currentPressDot < threshold;
                }
                
                return false;
            }
        }

        /// <summary>
        /// The purpose of this function is to allow us to switch between using the standard IsPointerMoving
        /// method for mouse driven pointers, but to always return true when it's a ray based pointer.
        /// All real-world ray-based input devices are always moving so for simplicity we just return true
        /// for them.
        ///
        /// If PointerEventData.IsPointerMoving was virtual we could just override that in
        /// CorePointerEventData.
        /// </summary>
        /// <param name="pointerEvent"></param>
        /// <returns></returns>
        static bool IsPointerMoving(PointerEventData pointerEvent)
        {
            if (pointerEvent.IsCoreXRPointer())
                return true;
            else
                return pointerEvent.IsPointerMoving();
        }

        /// <summary>
        /// Exactly the same as the code from PointerInputModule, except that we call our own
        /// IsPointerMoving.
        ///
        /// This would also not be necessary if PointerEventData.IsPointerMoving was virtual
        /// </summary>
        /// <param name="pointerEvent"></param>
        protected override void ProcessDrag(PointerEventData pointerEvent)
        {
            Vector2 originalPosition = pointerEvent.position;
            bool moving = IsPointerMoving(pointerEvent);
            bool shouldStartDrag = ShouldStartDrag(pointerEvent);

            if (moving && pointerEvent.pointerDrag != null
                && !pointerEvent.dragging
                && shouldStartDrag)
            {
                if (pointerEvent.IsCoreXRPointer())
                {
                    var eventCamera = pointerEvent.pressEventCamera;
                    pointerEvent.delta = originalPosition - (Vector2) eventCamera.WorldToScreenPoint(cursorLastPosition);
                }
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.beginDragHandler);
                pointerEvent.dragging = true;
            }

            // Drag notification
            if (pointerEvent.dragging && moving && pointerEvent.pointerDrag != null)
            {
                if (pointerEvent.IsCoreXRPointer())
                {
                    var eventCamera = pointerEvent.pressEventCamera;
                    pointerEvent.delta = originalPosition - (Vector2) eventCamera.WorldToScreenPoint(cursorLastPosition);
                }
                // Before doing drag we should cancel any pointer down state
                // And clear selection!
                if (pointerEvent.pointerPress != pointerEvent.pointerDrag)
                {
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                    pointerEvent.eligibleForClick = false;
                    pointerEvent.pointerPress = null;
                    pointerEvent.rawPointerPress = null;
                }
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.dragHandler);
            }
        }

        /// <summary>
        /// Get state of button corresponding to gaze pointer
        /// </summary>
        /// <returns></returns>
        virtual protected PointerEventData.FramePressState GetGazeButtonState()
        {
            if (!rayIsActivate)
                return PointerEventData.FramePressState.NotChanged;

            var pressed = m_Hand.GetKeepingGesture(clickGesture);
            var released = !pressed;
            
            if (!lastGestureIsClick && pressed)
            {
                lastGestureIsClick = true;
                return PointerEventData.FramePressState.Pressed;
            }
            if (lastGestureIsClick && released)
            {
                lastGestureIsClick = false;
                return PointerEventData.FramePressState.Released;
            }
            return PointerEventData.FramePressState.NotChanged;
        }
    }
}