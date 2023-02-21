using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Coretronic.Reality.EventSystems;

namespace Coretronic.Reality.UI
{
    /// <summary>
    /// Simple event system using physics raycasts. Very closely based on UnityEngine.EventSystems.PhysicsRaycaster
    /// </summary>
    [RequireComponent(typeof(CoreHMD))]
    public class CorePhysicsRaycaster : BaseRaycaster
    {
        //// <summary>
        /// Const to use for clarity when no event mask is set
        /// </summary>
        protected const int kNoEventMaskSet = -1;

        /// <summary>
        /// Layer mask used to filter events. Always combined with the camera's culling mask if a camera is used.
        /// </summary>
        [SerializeField]
        protected LayerMask m_EventMask = kNoEventMaskSet;

        /** \brief The default constructor. */
        protected CorePhysicsRaycaster()
        { }

        /** \brief The camera that will generate rays for this raycaster. */
        public override Camera eventCamera
        {
            get
            {
                return GetComponent<CoreHMD>().CenterEyeCamera;
            }
        }

        /// <summary>
        /// Depth used to determine the order of event processing.
        /// </summary>
        public virtual int depth
        {
            get { return (eventCamera != null) ? (int) eventCamera.depth : 0xFFFFFF; }
        }

        /** \brief Priority of the raycaster based upon sort order. */
        public int sortOrder = 0;

        /** \brief Priority of the raycaster based upon sort order. */
        public override int sortOrderPriority
        {
            get
            {
                return sortOrder;
            }
        }

        /// <summary>
        /// Event mask used to determine which objects will receive events.
        /// </summary>
        public int finalEventMask
        {
            get { return (eventCamera != null) ? eventCamera.cullingMask & m_EventMask : kNoEventMaskSet; }
        }

        /// <summary>
        /// Layer mask used to filter events. Always combined with the camera's culling mask if a camera is used.
        /// </summary>
        public LayerMask eventMask
        {
            get { return m_EventMask; }
            set { m_EventMask = value; }
        }


        /// <summary>
        /// Perform a raycast using the worldSpaceRay in eventData.
        /// </summary>
        /// <param name="eventData">PointerEventData (e.g. CorePointerEventData)</param>
        /// <param name="resultAppendList">The raycast results</param>
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            // This function is closely based on PhysicsRaycaster.Raycast
            if (eventCamera == null) return;
            if (!eventData.IsCoreXRPointer()) return;

            var ray = eventData.GetRay();
            float dist = eventCamera.farClipPlane - eventCamera.nearClipPlane;
            var hits = Physics.RaycastAll(ray, dist, finalEventMask);

            if (hits.Length > 1)
                System.Array.Sort(hits, (r1, r2) => r1.distance.CompareTo(r2.distance));
            if (hits.Length != 0)
            {
                for (int b = 0, bmax = hits.Length; b < bmax; ++b)
                {
                    var result = new RaycastResult
                    {
                        gameObject = hits[b].collider.gameObject,
                        module = this,
                        distance = hits[b].distance,
                        index = resultAppendList.Count,
                        worldPosition = hits[0].point,
                        worldNormal = hits[0].normal,
                    };
                    resultAppendList.Add(result);
                }
            }
        }

        /// <summary>
        ///  Perform a Spherecast using the worldSpaceRay in eventData.
        /// </summary>
        /// <param name="eventData">PointerEventData (e.g. CorePointerEventData)</param>
        /// <param name="resultAppendList">The raycast results</param>
        /// <param name="radius">Radius of the sphere</param>
        public void Spherecast(PointerEventData eventData, List<RaycastResult> resultAppendList, float radius)
        {
            if (eventCamera == null) return;
            if (!eventData.IsCoreXRPointer()) return;

            var ray = eventData.GetRay();
            float dist = eventCamera.farClipPlane - eventCamera.nearClipPlane;
            var hits = Physics.SphereCastAll(ray, radius, dist, finalEventMask);

            if (hits.Length > 1)
                System.Array.Sort(hits, (r1, r2) => r1.distance.CompareTo(r2.distance));

            if (hits.Length != 0)
            {
                for (int b = 0, bmax = hits.Length; b < bmax; ++b)
                {
                    var result = new RaycastResult
                    {
                        gameObject = hits[b].collider.gameObject,
                        module = this,
                        distance = hits[b].distance,
                        index = resultAppendList.Count,
                        worldPosition = hits[0].point,
                        worldNormal = hits[0].normal,
                    };
                    resultAppendList.Add(result);
                }
            }
        }

        /// <summary>
        /// Get screen position of worldPosition contained in this RaycastResult.
        /// </summary>
        /// <param name="raycastResult">Raycast Result</param>
        /// <returns>An position on screen.</returns>
        public Vector2 GetScreenPosition(RaycastResult raycastResult)
        {
            // In future versions of Uinty RaycastResult will contain screenPosition so this will not be necessary
            return eventCamera.WorldToScreenPoint(raycastResult.worldPosition);
        }
    }
}