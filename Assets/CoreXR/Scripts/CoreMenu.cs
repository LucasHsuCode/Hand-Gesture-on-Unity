using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** \brief UI provides a set of APIs for user interface on Unity Engine. */
namespace Coretronic.Reality.UI
{
    /** \brief CoreMenu is the abstract object used to control the menu. */
    public abstract class CoreMenu : MonoBehaviour
    {
        /** \brief An abstract function turns this menu into a focus state. */
        public abstract void OnFocusAcquired();
        /** \brief An abstract function turns this menu into a lost state. */
        public abstract void OnFocusLost();
        /** \brief An abstract function used to check whether the menu is in the focused state. */
        public abstract bool IsFocus();
    }
}