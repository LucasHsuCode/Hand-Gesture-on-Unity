using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Coretronic.Reality.UI
{
    /** \brief CoreGazePointer is the advanced cursor module implemented by the CoreCursor. */
    public class CoreGazePointer : CoreCursor
    {
        // private Transform gazeIcon; //the transform that rotates according to our movement

        /** \brief Should the pointer be hidden when not over interactive objects. */
        [Tooltip("Should the pointer be hidden when not over interactive objects.")]
        public bool hideByDefault = true;

        /** \brief Time after leaving interactive object before pointer fades. */
        [Tooltip("Time after leaving interactive object before pointer fades.")]
        public float showTimeoutPeriod = 1;

        /** \brief Time after mouse pointer becoming inactive before pointer unfades. */
        [Tooltip("Time after mouse pointer becoming inactive before pointer unfades.")]
        public float hideTimeoutPeriod = 0.1f;

        /** \brief Keep a faint version of the pointer visible while using a mouse. */
        [Tooltip("Keep a faint version of the pointer visible while using a mouse.")]
        public bool dimOnHideRequest = true;

        /** \brief Angular scale of pointer */
        [Tooltip("Angular scale of pointer")]
        public float depthScaleMultiplier = 0.03f;

        /** \brief The pointer visual object. */
        public GameObject pointerVisual;

        /** \brief The cursor visual object. */
        public GameObject cursorVisual;

        /** \brief The line visual object. */
        public GameObject lineVisual;

        /// <summary>
        /// Is gaze pointer current visible
        /// </summary>
        public bool hidden { get; private set; }

        /// <summary>
        /// Current scale applied to pointer
        /// </summary>
        public float currentScale { get; private set; }

        /// <summary>
        /// Current depth of pointer from camera
        /// </summary>
        private float depth = 3;

        /// <summary>
        /// Position last frame.
        /// </summary>
        private Vector3 lastPosition;

        /// <summary>
        /// Last time code requested the pointer be shown. Usually when pointer passes over interactive elements.
        /// </summary>
        private float lastShowRequestTime;

        /// <summary>
        /// Last time pointer was requested to be hidden. Usually mouse pointer activity.
        /// </summary>
        private float lastHideRequestTime;

        private bool m_restoreOnInputAcquired = false;
        private CoreHMD coreHMD;
        private Vector3 _forward;
        private Vector3 _startPoint;
        private Vector3 _endPoint;
        private bool _hitTarget;
        private Vector3 _positionDelta;

        /** \brief Show how much the gaze pointer moved in the last frame. */
        public Vector3 positionDelta => _positionDelta;

        /// <summary>
        /// Used to determine alpha level of gaze cursor. Could also be used to determine cursor size, for example, as the cursor fades out.
        /// </summary>
        public float visibilityStrength 
        { 
            get 
            {
                // It's possible there are reasons to show the cursor - such as it hovering over some UI - and reasons to hide 
                // the cursor - such as another input method (e.g. mouse) being used. We take both of these in to account.
                float strengthFromShowRequest;
                if (hideByDefault)
                {
                    // fade the cursor out with time
                    strengthFromShowRequest =  Mathf.Clamp01(1 - (Time.time - lastShowRequestTime) / showTimeoutPeriod);
                }
                else
                {
                    // keep it fully visible
                    strengthFromShowRequest = 1;
                }

                // Now consider factors requesting pointer to be hidden
                float strengthFromHideRequest = (lastHideRequestTime + hideTimeoutPeriod > Time.time) ? (dimOnHideRequest ? 0.1f : 0) : 1;
                // Hide requests take priority
                return Mathf.Min(strengthFromShowRequest, strengthFromHideRequest);
            } 
        }

        void Awake()
        {
            currentScale = 1;
            coreHMD = FindObjectOfType<CoreHMD>();
        }

        void Update()
        {
            // Should we show or hide the gaze cursor?
            if (visibilityStrength == 0 && !hidden)
            {
                Hide();
            }
            else if (visibilityStrength > 0 && hidden)
            {
                Show();
            }
        }

        void LateUpdate()
        {
            var pos = _hitTarget ? _endPoint : _startPoint + _forward*depth;
            var _depth = (coreHMD.CenterEyeAnchor.transform.position - pos).magnitude;

            if (pointerVisual)
            {
                pointerVisual.transform.position = _startPoint;
            }
            if (lineVisual)
            {
                var lineRenderer = lineVisual.GetComponent<LineRenderer>();
                lineRenderer.SetPosition(0, _startPoint);
                lineRenderer.SetPosition(1, pos);
                lineRenderer.endWidth = lineRenderer.startWidth * _depth * depthScaleMultiplier;
            }
            if (cursorVisual)
            {
                cursorVisual.transform.position = pos;
                currentScale = _depth * depthScaleMultiplier;
                cursorVisual.transform.localScale = new Vector3(currentScale, currentScale, currentScale);
                Quaternion newRot = cursorVisual.transform.rotation;
                newRot.SetLookRotation(_forward, coreHMD.CenterEyeAnchor.transform.up);
                cursorVisual.transform.rotation = newRot;

                // Keep track of cursor movement direction
                _positionDelta = cursorVisual.transform.position - lastPosition;
                lastPosition = cursorVisual.transform.position;
            }
        }

        public override void SetCursorRay(Vector3 start, Vector3 forward)
        {
            _startPoint = start;
            _forward = forward;
            _hitTarget = false;
        }

        public override void SetCursorStartDest(Vector3 start, Vector3 pos, Vector3 normal)
        {
            _startPoint = start;
            _forward = normal;
            _endPoint = pos;
            _hitTarget = true;
            RequestShow();
        }

        public override void OnFocusLost()
        {
            if (gameObject && gameObject.activeInHierarchy)
            {
                m_restoreOnInputAcquired = true;
                gameObject.SetActive(false);
            }
        }

        public override void OnFocusAcquired()
        {
            if (m_restoreOnInputAcquired && gameObject)
            {
                m_restoreOnInputAcquired = false;
                gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Request the pointer be hidden
        /// </summary>
        public void RequestHide()
        {
            if (!dimOnHideRequest)
            {
                Hide();
            }
            lastHideRequestTime = Time.time;
        }

        /// <summary>
        /// Request the pointer be shown. Hide requests take priority
        /// </summary>
        public void RequestShow()
        {
            Show();
            lastShowRequestTime = Time.time;
        }

        // Disable/Enable child elements when we show/hide the cursor. For performance reasons.
        void Hide()
        {
            if (pointerVisual)
            {
                pointerVisual.SetActive(false);
            }
            if (cursorVisual)
            {
                foreach (Transform child in cursorVisual.transform)
                {
                    child.gameObject.SetActive(false);
                }
                if (cursorVisual.GetComponent<Renderer>())
                    cursorVisual.GetComponent<Renderer>().enabled = false;
            }
            if (lineVisual)
            {
                lineVisual.SetActive(false);
                // lineRenderer.enabled = false;
                // lineRenderer.GetComponent<Renderer>().enabled = false;
            }

            hidden = true;
        }

        void Show()
        {
            if (pointerVisual)
            {
                pointerVisual.SetActive(true);
            }
            if (cursorVisual)
            {
                foreach (Transform child in cursorVisual.transform)
                {
                    child.gameObject.SetActive(true);
                }
                if (cursorVisual.GetComponent<Renderer>())
                    cursorVisual.GetComponent<Renderer>().enabled = true;
            }
            if (lineVisual)
            {
                lineVisual.SetActive(true);
                // lineRenderer.enabled = true;
                // lineRenderer.GetComponent<Renderer>().enabled = true;
            }

            hidden = false;
        }
    }
}