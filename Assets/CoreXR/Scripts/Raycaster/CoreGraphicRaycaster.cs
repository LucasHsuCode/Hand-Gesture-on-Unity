using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Coretronic.Reality.EventSystems;

namespace Coretronic.Reality.UI
{
    /** \brief A BaseRaycaster to raycast against Graphic elements. */
    [RequireComponent(typeof(Canvas))]
    public class CoreGraphicRaycaster : GraphicRaycaster
    {
        /** \brief Priority of the raycaster based upon sort order. */
        public int sortOrder = 0;

        protected CoreGraphicRaycaster() { }

        [NonSerialized]
        private Canvas m_Canvas;

        private Canvas canvas
        {
            get
            {
                if (m_Canvas != null)
                    return m_Canvas;

                m_Canvas = GetComponent<Canvas>();
                return m_Canvas;
            }
        }

        /** \brief The camera that will generate rays for this raycaster. */
        public override Camera eventCamera
        {
            get
            {
                return canvas.worldCamera;
            }
        }

        /** \brief Priority of the raycaster based upon sort order. */
        public override int sortOrderPriority
        {
            get
            {
                return sortOrder;
            }
        }

        /** \brief Start is called on the frame when a script is enabled just before any of the Update methods are called the first time. */
        protected override void Start()
        {
            if(!canvas.worldCamera)
            {
                Debug.Log("Canvas does not have an event camera attached. Attaching CoreHMD.centerEyeAnchor as default.");
                CoreHMD hmd = FindObjectOfType<CoreHMD>();
                canvas.worldCamera = hmd.CenterEyeCamera;
            }
        }

        /// <summary>
        /// For the given ray, find graphics on this canvas which it intersects and are not blocked by other
        /// world objects
        /// </summary>
        [NonSerialized]
        private List<RaycastHit> m_RaycastResults = new List<RaycastHit>();
        private void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList, Ray ray, bool checkForBlocking)
        {
            //This function is closely based on
            //void GraphicRaycaster.Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)

            if (canvas == null)
                return;

            float hitDistance = float.MaxValue;

            if (checkForBlocking && blockingObjects != BlockingObjects.None)
            {
                float dist = eventCamera.farClipPlane;

                if (blockingObjects == BlockingObjects.ThreeD || blockingObjects == BlockingObjects.All)
                {
                    var hits = Physics.RaycastAll(ray, dist, m_BlockingMask);

                    if (hits.Length > 0 && hits[0].distance < hitDistance)
                    {
                        hitDistance = hits[0].distance;
                    }
                }

                if (blockingObjects == BlockingObjects.TwoD || blockingObjects == BlockingObjects.All)
                {
                    var hits = Physics2D.GetRayIntersectionAll(ray, dist, m_BlockingMask);

                    if (hits.Length > 0 && hits[0].fraction * dist < hitDistance)
                    {
                        hitDistance = hits[0].fraction * dist;
                    }
                }
            }

            m_RaycastResults.Clear();
            GraphicRaycast(canvas, ray, m_RaycastResults);

            for (var index = 0; index < m_RaycastResults.Count; index++)
            {
                var go = m_RaycastResults[index].graphic.gameObject;
                bool appendGraphic = true;

                if (ignoreReversedGraphics)
                {
                    // If we have a camera compare the direction against the cameras forward.
                    var cameraFoward = ray.direction;
                    var dir = go.transform.rotation * Vector3.forward;
                    appendGraphic = Vector3.Dot(cameraFoward, dir) > 0;
                }

                // Ignore points behind us (can happen with a canvas pointer)
                if (eventCamera.transform.InverseTransformPoint(m_RaycastResults[index].worldPos).z <= 0)
                {
                    appendGraphic = false;
                }

                if (appendGraphic)
                {
                    float distance = Vector3.Distance(ray.origin, m_RaycastResults[index].worldPos);

                    if (distance >= hitDistance)
                    {
                        continue;
                    }

                    var castResult = new RaycastResult
                    {
                        gameObject = go,
                        module = this,
                        distance = distance,
                        index = resultAppendList.Count,
                        depth = m_RaycastResults[index].graphic.depth,

                        worldPosition = m_RaycastResults[index].worldPos
                    };
                    resultAppendList.Add(castResult);
                }
            }
        }

        /// <summary>
        /// Performs a raycast using eventData.worldSpaceRay
        /// </summary>
        /// <param name="eventData">PointerEventData (e.g. CorePointerEventData)</param>
        /// <param name="resultAppendList">The raycast results</param>
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (eventData.IsCoreXRPointer())
            {
                Raycast(eventData, resultAppendList, eventData.GetRay(), true);
            }
        }

        /// <summary>
        /// Perform a raycast into the screen and collect all graphics underneath it.
        /// </summary>
        [NonSerialized]
        static readonly List<RaycastHit> s_SortedGraphics = new List<RaycastHit>();
        private void GraphicRaycast(Canvas canvas, Ray ray, List<RaycastHit> results)
        {
            //This function is based closely on :
            // void GraphicRaycaster.Raycast(Canvas canvas, Camera eventCamera, Vector2 pointerPosition, List<Graphic> results)
            // But modified to take a Ray instead of a canvas pointer, and also to explicitly ignore
            // the graphic associated with the pointer

            // Necessary for the event system
            var foundGraphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
            s_SortedGraphics.Clear();
            for (int i = 0; i < foundGraphics.Count; ++i)
            {
                Graphic graphic = foundGraphics[i];

                // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
                if (graphic.depth == -1)
                    continue;
                Vector3 worldPos;
                if (RayIntersectsRectTransform(graphic.rectTransform, ray, out worldPos))
                {
                    //Work out where this is on the screen for compatibility with existing Unity UI code
                    Vector2 screenPos = eventCamera.WorldToScreenPoint(worldPos);
                    // mask/image intersection - See Unity docs on eventAlphaThreshold for when this does anything
                    if (graphic.Raycast(screenPos, eventCamera))
                    {
                        RaycastHit hit;
                        hit.graphic = graphic;
                        hit.worldPos = worldPos;
                        // hit.fromMouse = false;
                        s_SortedGraphics.Add(hit);
                    }
                }
            }

            s_SortedGraphics.Sort((g1, g2) => g2.graphic.depth.CompareTo(g1.graphic.depth));

            for (int i = 0; i < s_SortedGraphics.Count; ++i)
            {
                results.Add(s_SortedGraphics[i]);
            }
        }

        /// <summary>
        /// Get screen position of worldPosition contained in this RaycastResult.
        /// </summary>
        /// <param name="raycastResult">The raycast results</param>
        /// <returns>An position on screen.</returns>
        public Vector2 GetScreenPosition(RaycastResult raycastResult)
        {
            // In future versions of Uinty RaycastResult will contain screenPosition so this will not be necessary
            return eventCamera.WorldToScreenPoint(raycastResult.worldPosition);
        }

        /// <summary>
        /// Detects whether a ray intersects a RectTransform and if it does also
        /// returns the world position of the intersection.
        /// </summary>
        /// <param name="rectTransform"></param>
        /// <param name="ray"></param>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        static bool RayIntersectsRectTransform(RectTransform rectTransform, Ray ray, out Vector3 worldPos)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Plane plane = new Plane(corners[0], corners[1], corners[2]);

            float enter;
            if (!plane.Raycast(ray, out enter))
            {
                worldPos = Vector3.zero;
                return false;
            }

            Vector3 intersection = ray.GetPoint(enter);

            Vector3 BottomEdge = corners[3] - corners[0];
            Vector3 LeftEdge = corners[1] - corners[0];
            float BottomDot = Vector3.Dot(intersection - corners[0], BottomEdge);
            float LeftDot = Vector3.Dot(intersection - corners[0], LeftEdge);
            if (BottomDot < BottomEdge.sqrMagnitude && // Can use sqrMag because BottomEdge is not normalized
                LeftDot < LeftEdge.sqrMagnitude &&
                    BottomDot >= 0 &&
                    LeftDot >= 0)
            {
                worldPos = corners[0] + LeftDot * LeftEdge / LeftEdge.sqrMagnitude + BottomDot * BottomEdge / BottomEdge.sqrMagnitude;
                return true;
            }
            else
            {
                worldPos = Vector3.zero;
                return false;
            }
        }


        struct RaycastHit
        {
            public Graphic graphic;
            public Vector3 worldPos;
        };
    }
}