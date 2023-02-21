using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Coretronic.Reality.UI
{
    /** 
     * \brief CoreCursor is an abstract class to control cursor.
     */
    abstract public class CoreCursor : MonoBehaviour
    {
        /** 
         * \brief Set cursor ray.
         * @param[in] start   The start point of the ray.
         * @param[in] forward The direction of the ray.
         */
        public abstract void SetCursorRay(Vector3 start, Vector3 forward);

        /** 
         * \brief Set the cursor ray through the line between the start point and the direction point.
         * @param[in] start  The origin point of the ray.
         * @param[in] dest   The direction point of the ray.
         * @param[in] normal The normal direction of the line.
         */
        public abstract void SetCursorStartDest(Vector3 start, Vector3 dest, Vector3 normal);

        /** 
         * \brief Occurs when cursor focus is acquired.
         */
        public abstract void OnFocusAcquired();

        /** 
         * \brief Occurs when cursor focus is lost.
         */
        public abstract void OnFocusLost();
    }
}