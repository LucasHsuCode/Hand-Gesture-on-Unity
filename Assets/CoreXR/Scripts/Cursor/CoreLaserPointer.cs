using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Coretronic.Reality.UI
{
    /** \brief CoreLaserPointer is the basic cursor module implemented by the CoreCursor. */
    public class CoreLaserPointer : CoreCursor
    {
        /** \brief The laser beam behavior. */
        public enum LaserBeamBehavior
        {
            On, /**< Laser beam always on */
            Off,  /**< Laser beam always off */ 
            OnWhenHitTarget,  /**< Laser beam only activates when hit valid target */ 
        }

        /** \brief The pointer visual object. */
        public GameObject pointerVisual;

        /** \brief The cursor visual object. */
        public GameObject cursorVisual;
        
        [SerializeField] 
        float maxLength = 10.0f;
        
        [SerializeField] 
        LaserBeamBehavior laserBeamBehavior = LaserBeamBehavior.On;

        bool m_restoreOnInputAcquired = false;

        private Vector3 _startPoint;
        private Vector3 _forward;
        private Vector3 _endPoint;
        private bool _hitTarget;
        private LineRenderer lineRenderer;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        private void Start()
        {
            if (cursorVisual) cursorVisual.SetActive(false);
            if (pointerVisual) pointerVisual.SetActive(false);
        }

        public override void SetCursorStartDest(Vector3 start, Vector3 dest, Vector3 normal)
        {
            _startPoint = start;
            _endPoint = dest;
            _hitTarget = true;
        }

        public override void SetCursorRay(Vector3 start, Vector3 forward)
        {
            _startPoint = start;
            _forward = forward;
            _hitTarget = false;
        }

        private void Update()
        {
            if (laserBeamBehavior == LaserBeamBehavior.Off || laserBeamBehavior == LaserBeamBehavior.OnWhenHitTarget)
            {
                lineRenderer.enabled = false;
            }
            else
            {
                lineRenderer.enabled = true;
            }   
        }

        private void LateUpdate()
        {
            if (pointerVisual)
            {
                pointerVisual.transform.position = _startPoint;
                pointerVisual.SetActive(true);
            }
            if (_hitTarget)
            {
                UpdateLaserBeam(_startPoint, _endPoint);

                if (cursorVisual)
                {
                    cursorVisual.transform.position = _endPoint;
                    cursorVisual.SetActive(true);
                }
            }
            else
            {
                UpdateLaserBeam(_startPoint, _startPoint + maxLength * _forward);
                if (cursorVisual) cursorVisual.SetActive(false);
            }
        }

        // make laser beam a behavior with a prop that enables or disables
        private void UpdateLaserBeam(Vector3 start, Vector3 end)
        {
            if (laserBeamBehavior == LaserBeamBehavior.Off)
            {
                return;
            }
            else if (laserBeamBehavior == LaserBeamBehavior.On)
            {
                lineRenderer.SetPosition(0, start);
                lineRenderer.SetPosition(1, end);
            }
            else if (laserBeamBehavior == LaserBeamBehavior.OnWhenHitTarget)
            {
                lineRenderer.SetPosition(0, start);
                lineRenderer.SetPosition(1, end);
                if (_hitTarget)
                {
                    if (!lineRenderer.enabled)
                    {
                        lineRenderer.enabled = true;
                    }
                }
                else
                {
                    if (lineRenderer.enabled)
                    {
                        lineRenderer.enabled = false;
                    }
                }
            }
        }

        void OnDisable()
        {
            if (cursorVisual) cursorVisual.SetActive(false);
            if (pointerVisual) pointerVisual.SetActive(false);
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

        private void OnDestroy()
        {
        }
    }
}