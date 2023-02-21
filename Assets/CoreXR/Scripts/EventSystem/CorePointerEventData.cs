using System;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace Coretronic.Reality.EventSystems
{
    /// <summary>
    /// Extension of Unity's PointerEventData to support ray based pointing and also touchpad swiping
    /// </summary>
    public class CorePointerEventData : PointerEventData
    {
        /// <summary>
        /// The default constructor
        /// </summary>
        public CorePointerEventData(EventSystem eventSystem) : base(eventSystem)
        {
        }

        /// <summary>
        /// The world space ray of PointerEventData
        /// </summary>
        public Ray worldSpaceRay;
        
        /** 
         * \brief A string that represents the current object.
         * @returns A string that represents the current object.
         */
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<b>Position</b>: " + position);
            sb.AppendLine("<b>delta</b>: " + delta);
            sb.AppendLine("<b>eligibleForClick</b>: " + eligibleForClick);
            sb.AppendLine("<b>pointerEnter</b>: " + pointerEnter);
            sb.AppendLine("<b>pointerPress</b>: " + pointerPress);
            sb.AppendLine("<b>lastPointerPress</b>: " + lastPress);
            sb.AppendLine("<b>pointerDrag</b>: " + pointerDrag);
            sb.AppendLine("<b>worldSpaceRay</b>: " + worldSpaceRay);
            sb.AppendLine("<b>Use Drag Threshold</b>: " + useDragThreshold);
            return sb.ToString();
        }
    }


    /// <summary>
    /// Static helpers for CorePointerEventData to extend PointerEventData.
    /// </summary>
    public static class PointerEventDataExtension
    {
        /** 
         * \brief To check PointerEventData whether supports CorePointerEventData.
         * @returns A boolean is true if CorePointerEventData is supported.
         */
        public static bool IsCoreXRPointer(this PointerEventData pointerEventData)
        {
            return (pointerEventData is CorePointerEventData);
        }

        /** 
         * \brief Get ray from CorePointerEventData.
         * @returns Unity Ray object.
         */
        public static Ray GetRay(this PointerEventData pointerEventData)
        {
            CorePointerEventData vrPointerEventData = pointerEventData as CorePointerEventData;
            Assert.IsNotNull(vrPointerEventData);
            return vrPointerEventData.worldSpaceRay;
        }
    }
}
