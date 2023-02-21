using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Coretronic.Reality.Clients;
using Coretronic.Reality.Tools;

namespace Coretronic.Reality
{
    /// <summary>
    /// ClientsManager is a manager to control the connection between services and clients.
    /// </summary>
    public class ClientsManager : MonoBehaviour
    {
        [Header("Clients")]

        [SerializeField] 
        private bool m_UseCoreMemoryClient = false;

        [SerializeField] 
        private bool m_UseFrameProviderClient = true;

        [SerializeField] 
        private bool m_UseStereoClient = true;

        [SerializeField] 
        private bool m_UseSLAMClient = false;

        [SerializeField] 
        private bool m_UseHandTrackingClient = false;

        [Header("Components")]

        [SerializeField, ConditionalHide("m_UseFrameProviderClient, m_UseStereoClient", "false, false")] 
        private bool m_UseFrameProvider = true;

        [SerializeField, ConditionalHide("m_UseSLAMClient")] 
        private bool m_UseSLAMHandler = false;

        [SerializeField, ConditionalHide("m_UseHandTrackingClient")] 
        private bool m_UseHandProvider = false;

        /// <summary>
        /// Return connection state, true if all clients are connected.
        /// </summary>
        public bool ConnectedClients { get; private set; } = false;

        /// <summary>
        /// Get the static class instance of ClientsManager created on Awake state.
        /// </summary>
        public static ClientsManager Instance { get; private set; } = null;

        private bool _initializing = false;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        void Start()
        {
            SetFunction();
            
            if (!_initializing) StartCoroutine(WaitAndStartClients());
        }

        void OnDestroy()
        {
            if (CoreMemoryClient.Instance.IsConnect)
            {
                CoreMemoryClient.Instance.StopService();
            }
            if (FrameProviderClient.Instance.IsConnect)
            {
                FrameProviderClient.Instance.StopService();
            }
            if (StereoClient.Instance.IsConnect)
            {
                StereoClient.Instance.StopService();
            }
            if (SlamClient.Instance.IsConnect)
            {
                SlamClient.Instance.StopService();
            }
            if (HandTrackingClient.Instance.IsConnect)
            {
                HandTrackingClient.Instance.StopService();
            }

            ConnectedClients = false;
        }

        void OnValidate()
        {
            if (!(m_UseFrameProviderClient && m_UseStereoClient) && m_UseFrameProvider)
            {
                m_UseFrameProvider = false;
            }
            if (!m_UseSLAMClient && m_UseSLAMHandler)
            {
                m_UseSLAMHandler = false;
            }
            if (!m_UseHandTrackingClient && m_UseHandProvider)
            {
                m_UseHandProvider = false;
            }

            SetFunction();
        } 


        private void SetFunction()
        {
            var fpObj = gameObject.GetComponent(typeof(FrameProvider));
            var slamObj = gameObject.GetComponent(typeof(SLAMHandler));
            var hpObj = gameObject.GetComponent(typeof(CoreHandProvider));

            if (fpObj)
            {
                (fpObj as MonoBehaviour).enabled = m_UseFrameProvider;
            }
            if (slamObj)
            {
                (slamObj as MonoBehaviour).enabled = m_UseSLAMHandler;
            }
            if (hpObj)
            {
                (hpObj as MonoBehaviour).enabled = m_UseHandProvider;
            }
        }

        IEnumerator WaitAndStartClients()
        {
            if (ConnectedClients) yield break;

            _initializing = true;

            if (m_UseCoreMemoryClient && !CoreMemoryClient.Instance.IsConnect)
            { 
                CoreMemoryClient.Instance.StartService();
            }
            while (m_UseCoreMemoryClient && !CoreMemoryClient.Instance.IsConnect)
            {
                Debug.Log($"CoreMemoryClient waiting for connection...");
                yield return new WaitForSeconds(0.1f);
            }
            if (m_UseFrameProviderClient && !FrameProviderClient.Instance.IsConnect)
            { 
                FrameProviderClient.Instance.StartService();
            }
            while (m_UseFrameProviderClient && !FrameProviderClient.Instance.IsConnect)
            {
                Debug.Log($"FrameProviderClient waiting for connection...");
                yield return new WaitForSeconds(0.1f);
            }
            if (m_UseStereoClient)
            {
                var cameraMode = FrameProviderClient.Instance.GetCameraMode();
                m_UseStereoClient = cameraMode == LogicalCameraType.QvrStereoColor || cameraMode == LogicalCameraType.QvrStereoTracking;
            }
            if (m_UseStereoClient && !StereoClient.Instance.IsConnect)
            { 
                StereoClient.Instance.StartService();
            }
            while (m_UseStereoClient && !StereoClient.Instance.IsConnect)
            {
                Debug.Log($"StereoClient waiting for connection...");
                yield return new WaitForSeconds(0.1f);
            }
            if (m_UseSLAMClient && !SlamClient.Instance.IsConnect)
            { 
                SlamClient.Instance.StartService();
            }
            while (m_UseSLAMClient && !SlamClient.Instance.IsConnect)
            {
                Debug.Log($"SlamClient waiting for connection...");
                yield return new WaitForSeconds(0.1f);
            }
            if (m_UseHandTrackingClient && !HandTrackingClient.Instance.IsConnect)
            { 
                HandTrackingClient.Instance.StartService();
            }
            while (m_UseHandTrackingClient && !HandTrackingClient.Instance.IsConnect)
            {
                Debug.Log($"HandTrackingClient waiting for connection...");
                yield return new WaitForSeconds(0.1f);
            }

            ConnectedClients = true;
            _initializing = false;
        }
    }
}