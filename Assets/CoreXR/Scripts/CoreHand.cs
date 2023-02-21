using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using Coretronic.Reality.Hand;
using HandJoints = Coretronic.Reality.Hand.Joints;
using HandTypes = Coretronic.Reality.Hand.Types;

namespace Coretronic.Reality
{
    /** \brief CoreHand is APIs used to get hand positions from Unity Engine. */
    public class CoreHand : MonoBehaviour
    {
        /** \brief An empty gesture. */
        public static readonly Ray EmptyHandRay = new Ray(Vector3.zero, Vector3.forward);

        // public Camera eventCamera;

        /** \brief The hand type of this hand. */
        public HandTypes HandType = HandTypes.Unknown;

        [SerializeField]
        private bool m_AllowUnknownGesture = false;
        
        private Dictionary<HandJoints.JointName, Transform> jointDict = new Dictionary<HandJoints.JointName, Transform>();
        private Transform rayStartAnchor;
        private Vector3[] lastPosition = new Vector3[HandJoints.Length];
        private bool currentFound = false;
        private bool lastFound = false;
        private Gesture lastGesture = Gesture.unknown;
        private Gesture currentGesture = Gesture.unknown;

        private Ray currentRay = EmptyHandRay;
        private Ray lastRay = EmptyHandRay;
        private CoreHMD coreHMD => CoreHMD.Instance;

        /** \brief The gesture of this hand. */
        public Gesture gesture => currentGesture;

        /** \brief The hand ray of this hand. */
        public Ray handRay => currentRay;

        private Camera eventCamera => coreHMD.CenterEyeCamera;

        void Awake()
        {
            transform.localPosition = Vector3.zero;
            InjectGameObject();
        }

        void Start()
        {
#if !UNITY_EDITOR 
            gameObject.SetActive(currentFound);
#endif
        }

        private void InjectGameObject()
        {
            for (int i = 0; i < gameObject.transform.childCount - 1; i++)
                jointDict.Add(HandJoints.JointNameArray[i], gameObject.transform.GetChild(i));

            rayStartAnchor = gameObject.transform.GetChild(gameObject.transform.childCount - 1);
        }

        private bool isnan(Vector3 aaa)
        {
            return float.IsNaN(aaa.x) || float.IsNaN(aaa.y) || float.IsNaN(aaa.z);
        }

        /** \brief This is a function to update CoreHand Gameobject. */
        public void OnNewSample()
        {
            bool show = false;
            HandJoints joints = null;

            switch (HandType)
            {
                case HandTypes.Unknown:
                    return;
                case HandTypes.Left:
                    joints = CoreHandProvider.Instance.LeftJoints;
                    show = CoreHandProvider.Instance.LeftShow;
                    break;
                case HandTypes.Right:
                    joints = CoreHandProvider.Instance.RightJoints;
                    show = CoreHandProvider.Instance.RightShow;
                    break;
            }

            lastFound = currentFound;
            lastGesture = currentGesture;
            lastRay = currentRay;
            // currentJoints = null;
            currentFound = show;

            foreach(var jn in jointDict.Keys)
            {
                lastPosition[(int) jn] = jointDict[jn].position;
                jointDict[jn].localPosition = joints[jn];
            }

            currentGesture = currentFound ? Gesture.GetInstance(HandType, ToLocalPositionArray()) : Gesture.unknown;

            if (currentFound)
            {
                if (!m_AllowUnknownGesture && currentGesture.strictType == Gesture.StrictTypes.Unknown)
                    currentGesture = lastGesture;
                
                // update ray anchor
                var camRot = coreHMD.CenterEyeAnchor.eulerAngles;
                var rot = Quaternion.Euler(0, camRot.y, 0);
                rayStartAnchor.position = coreHMD.CenterEyeAnchor.position + rot * new Vector3(
                    HandType == HandTypes.Right ? 0.025f : HandType == HandTypes.Left ? -0.025f : 0, 0, 0);
                // assume ray
                // var rayOrig = Vector3.zero;
                // for (int i = 0; i < 6; i++) rayOrig += this[i].position;
                // rayOrig *= (1f/6f);
                var rayOrig = lastFound ? (0.9f*lastRay.origin + 0.1f*this[2].position) : this[2].position;
                // Debug.Log($"CoreHand Ray {this[2].position} {rayOrig}");
                var screenJn = eventCamera.WorldToScreenPoint(rayOrig);
                screenJn.z = 0.3f;
                var rayForward = (eventCamera.ScreenToWorldPoint(screenJn) - rayStartAnchor.position).normalized;
                currentRay = new Ray(rayOrig, rayForward);
            }

            gameObject.SetActive(currentFound);
        }
        
        /**
         * \brief Get the position of this joints by the joint name.
         * @param jn Name of the hand joint.
         * @return An Unity Transform that represents the joint position of this hand.
         */
        public Transform this[Hand.Joints.JointName jn]
        {
            get => jointDict[jn];
        }

        /**
         * \brief Get the position of this joints by the joint name.
         * @param jn Index of the hand joint.
         * @return An Unity Transform that represents the joint position of this hand.
         */
        public Transform this[int jn]
        {
            get => jointDict[(HandJoints.JointName) jn];
        }

        // private Vector3 MovingWorldDelta(HandJoints.JointName jn)
        // {
        //     return this[jn].position - GetLastPosition(jn);
        // }

        // public Vector3 GetMovingWorldDelta(Joints.JointName jn, bool isSameStrictGesture)
        // {
        //     if (currentFound && lastFound)
        //     {
        //         if (isSameStrictGesture)
        //             return lastGesture.strictType == currentGesture.strictType ? MovingWorldDelta(jn) : Vector3.zero;
        //         return MovingWorldDelta(jn);
        //     }

        //     return Vector3.zero;
        // }

        // public Vector3 GetMovingWorldDelta(Joints.JointName jn, Gesture.Types gestureType)
        // {
        //     if (currentFound && lastFound)
        //     {
        //         if (gestureType != Gesture.Types.Unknown)
        //         {
        //             bool currentSame = currentGesture.IsType(gestureType);
        //             bool lastSame = lastGesture.IsType(gestureType);
        //             return currentSame && lastSame ? MovingWorldDelta(jn) : Vector3.zero;
        //         }

        //         return MovingWorldDelta(jn);
        //     }

        //     return Vector3.zero;
        // }

        // private Vector2 MovingScreenDelta(HandJoints.JointName jn)
        // {
        //     Vector2 curr = eventCamera.WorldToScreenPoint(this[jn].position);
        //     Vector2 last = eventCamera.WorldToScreenPoint(GetLastPosition(jn));
        //     return curr - last;
        // }

        // public Vector2 GetMovingScreenDelta(Joints.JointName jn, bool isSameStrictGesture)
        // {
        //     if (currentFound && lastFound)
        //     {
        //         if (isSameStrictGesture)
        //             return lastGesture.strictType == currentGesture.strictType ? MovingScreenDelta(jn) : Vector2.zero;
        //         return MovingScreenDelta(jn);
        //     }

        //     return Vector2.zero;
        // }

        // public Vector2 GetMovingScreenDelta(Joints.JointName jn, Gesture.Types gestureType)
        // {
        //     if (currentFound && lastFound)
        //     {
        //         if (gestureType != Gesture.Types.Unknown)
        //         {
        //             bool currentSame = currentGesture.IsType(gestureType);
        //             bool lastSame = lastGesture.IsType(gestureType);
        //             return currentSame && lastSame ? MovingScreenDelta(jn) : Vector2.zero;
        //         }

        //         return MovingScreenDelta(jn);
        //     }
        //     return Vector2.zero;
        // }

        // private Vector3 RayWorldDelta()
        // {
        //     return currentRay.ray.origin - lastRay.ray.origin;
        // }

        // public Vector3 GetRayWorldDelta(bool isSameStrictGesture)
        // {
        //     if (currentFound && lastFound)
        //     {
        //         if (isSameStrictGesture)
        //             return lastGesture.strictType == currentGesture.strictType ? RayWorldDelta() : Vector3.zero;
        //         return RayWorldDelta();
        //     }

        //     return Vector3.zero;
        // }

        // public Vector3 GetRayWorldDelta(Gesture.Types gestureType)
        // {
        //     if (currentFound && lastFound)
        //     {
        //         if (gestureType != Gesture.Types.Unknown)
        //         {
        //             bool currentSame = currentGesture.IsType(gestureType);
        //             bool lastSame = lastGesture.IsType(gestureType);
        //             return currentSame && lastSame ? RayWorldDelta() : Vector3.zero;
        //         }

        //         return RayWorldDelta();
        //     }

        //     return Vector3.zero;
        // }

        // private Vector2 RayScreenDelta()
        // {
        //     Vector2 curr = eventCamera.WorldToScreenPoint(currentRay.ray.origin);
        //     Vector2 last = eventCamera.WorldToScreenPoint(lastRay.ray.origin);
        //     return curr - last;
        // }

        // public Vector2 GetRayScreenDelta(bool isSameStrictGesture)
        // {
        //     if (currentFound && lastFound)
        //     {
        //         if (isSameStrictGesture)
        //             return lastGesture.strictType == currentGesture.strictType ? RayScreenDelta() : Vector2.zero;
        //         return RayScreenDelta();
        //     }

        //     return Vector2.zero;
        // }

        // public Vector2 GetRayScreenDelta(Gesture.Types gestureType)
        // {
        //     if (currentFound && lastFound)
        //     {
        //         if (gestureType != Gesture.Types.Unknown)
        //         {
        //             bool currentSame = currentGesture.IsType(gestureType);
        //             bool lastSame = lastGesture.IsType(gestureType);
        //             return currentSame && lastSame ? RayScreenDelta() : Vector2.zero;
        //         }

        //         return RayScreenDelta();
        //     }
        //     return Vector2.zero;
        // }

        /**
         * \brief Check that the keeping gesture is the same as the selected strict gesture.
         * @param gestureType Selected strict gesture.
         * @return A boolean value represents that the keeping gesture is the same as the selected strict gesture.
         */
        public bool GetKeepingGesture(Gesture.StrictTypes gestureType)
        {
            return gestureType == currentGesture.strictType || currentGesture.strictType == lastGesture.strictType;
        }

        /**
         * \brief Check that the gesture is release after the selected strict gesture.
         * @param gestureType Selected strict gesture.
         * @return A boolean value that represents the gesture is release.
         */
        public bool GetReleasedGesture(Gesture.StrictTypes gestureType)
        {
            return !GetKeepingGesture(gestureType);
            // return gestureType == lastGesture.strictType && currentGesture.strictType != lastGesture.strictType;
        }

        /**
         * \brief Check that the keeping gesture is the same as the selected gesture.
         * @param gestureType Selected gesture.
         * @return A boolean value represents that the keeping gesture is the same as the selected strict gesture.
         */
        public bool GetKeepingGesture(Gesture.Types gestureType)
        {
            bool currentFound = currentGesture.IsType(gestureType);
            bool lastFound = lastGesture.IsType(gestureType);
            return currentFound || lastFound;
        }

        /**
         * \brief Check that the gesture is release after the selected gesture.
         * @param gestureType Selected gesture.
         * @return A boolean value that represents the gesture is release.
         */
        public bool GetReleasedGesture(Gesture.Types gestureType)
        {
            return !GetKeepingGesture(gestureType);
            // bool currentFound = currentGesture.IsType(gestureType);
            // bool lastFound = lastGesture.IsType(gestureType);
            // return lastFound && !currentFound;
        }

        // public Vector3 GetLastPosition(int jn)
        // {
        //     return lastFound ? new Vector3(lastPosition[jn].x, lastPosition[jn].y, lastPosition[jn].z) : Vector3.zero;
        // }

        // public Vector3 GetLastPosition(HandJoints.JointName jn)
        // {
        //     return GetLastPosition((int) jn);
        // }

        /**
         * \brief Get the world positions of joints for this hand.
         * @return An array of Unity Vector3 that represents the world positions of joints for this hand.
         */
        public Vector3[] ToPositionArray()
        {
            Vector3[] jointsArray = new Vector3[HandJoints.Length];
            for (int i = 0; i < HandJoints.Length; i++)
                jointsArray[i] = this[i].position;
            return jointsArray;
        }

        /**
         * \brief Get the local positions of joints for this hand.
         * @return An array of Unity Vector3 that represents the world positions of joints for this hand.
         */
        public Vector3[] ToLocalPositionArray()
        {
            Vector3[] jointsArray = new Vector3[HandJoints.Length];
            for (int i = 0; i < HandJoints.Length; i++)
                jointsArray[i] = this[i].localPosition;
            return jointsArray;
        }
    }
}