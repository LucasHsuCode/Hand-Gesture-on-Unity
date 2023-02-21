using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
using Coretronic.Reality.Clients;
using Coretronic.Reality.Tools;

namespace Coretronic.Reality
{
    /** \brief CoreHMD is a script used to control the head-mounted display. */
    public class CoreHMD : MonoBehaviour
    {
        [SerializeField]
        private bool m_UseHMDStructure = true;

        [SerializeField, ConditionalHide("m_UseHMDStructure")]
        private string m_HMDStructureConfigPath = "";

        /** \brief Set the see through mode with side-by-side for head-mounted display. */
        [SerializeField]
        private bool m_UsePerEyeCameras = true;

        /** \brief Set the pass through mode for head-mounted display. */
        [SerializeField]
        private bool m_PassThroughMode = false;

        [SerializeField, ConditionalHide("m_PassThroughMode")]
        private float m_FocalOfViewForPassThrough = 42.5f;

        /** \brief Whether the center camera takes the left camera as the center */
        [SerializeField]
        private bool m_LeftCameraAsCenter = true;

        // public bool useFixedUpdateForTracking = false;

        /** \brief Set to keep initial transform in the Unity Scene after SLAM is tracking. */
        [SerializeField]
        private bool m_KeepInitialTransform = true;

        [SerializeField, Tooltip("Allowed back button to leaves App")]
        private bool m_UseBackToLeave = true;

        /** \brief Get the static class instance of CoreHMD created on Awake state. */
        public static CoreHMD Instance { get; private set; }

        /** \brief The focal length of AR Camera for head-mounted display. */
        public static readonly float FocalLength = 10.6f;

        /** \brief The sensor size of AR Camera for head-mounted display. */
        public static readonly Vector2 SensorSize = new Vector2(6.91f, 3.89f);

        public bool PassThroughMode => m_PassThroughMode;

        public bool UsePerEyeCameras => m_UsePerEyeCameras;

        public bool UseHMDStructure => m_UseHMDStructure;

        public float FocalOfViewForPassThrough => m_FocalOfViewForPassThrough;

        public bool LeftCameraAsCenter => m_LeftCameraAsCenter;

        /** \brief The left eye camera for head-mounted display. */
        public Camera LeftEyeCamera => _leftEyeCamera;

        /** \brief The right eye camera for head-mounted display. */
        public Camera RightEyeCamera => _rightEyeCamera; // default camera

        /** \brief The center eye camera for head-mounted display. */
        public Camera CenterEyeCamera => LeftCameraAsCenter ? _leftEyeCamera : _rightEyeCamera;

        /** \brief Transform of the tracking space for head-mounted display. */
        public Transform TrackingSpace { get; private set; }

        public Transform DisplayAnchor { get; private set; }

        /** \brief Transform of the left eye camera for head-mounted display. */
        public Transform LeftEyeAnchor => LeftEyeCamera.transform;

        /** \brief Transform of the right eye camera for head-mounted display. */
        public Transform RightEyeAnchor => RightEyeCamera.transform;

        /** \brief Transform of the center eye camera for head-mounted display. */
        public Transform CenterEyeAnchor => LeftCameraAsCenter ? LeftEyeAnchor : RightEyeAnchor;
        public Canvas mainCanvas;

        private Camera _leftEyeCamera;
        private Camera _rightEyeCamera;
        private Matrix4x4 _initTransform = Matrix4x4.identity;
        private bool _usePerEyeCameras = true;
        private bool _prevUsePerEyeCameras = true;
        private HMDStructureBuilder.DefinedStructure _hmdStructure = HMDStructureBuilder.DefaultArgosStructure;

        private const string TrackingSpaceName = "TrackingSpace";
        private const string DisplayName = "TrackingSpace/Display";
        private const string LeftEyeName = "TrackingSpace/Display/LeftEye";
        private const string RightEyeName = "TrackingSpace/Display/RightEye";

        void OnEnable()
        {
            if (m_HMDStructureConfigPath.Length > 0)
            {
                _hmdStructure = HMDStructureBuilder.Read(m_HMDStructureConfigPath);
            }
        }

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

            Input.backButtonLeavesApp = m_UseBackToLeave;
            InjectGameObject();
        }

        void Start()
        {
            _initTransform = transform.localToWorldMatrix;

        }

        void OnApplicationPause(bool pause)
        {
            //android onResume event
            if (!pause)
            {

                _usePerEyeCameras = Screen.width == 2560 && Screen.height == 720 && UsePerEyeCameras;
                _prevUsePerEyeCameras = _usePerEyeCameras;
                UpdateDisplay();
            }
        }

        // void FixedUpdate()
        // {
        //     if (useFixedUpdateForTracking)
        //         UpdateTrackingSpace();
        // }

        void Update()
        {
            _usePerEyeCameras = Screen.width == 2560 && Screen.height == 720 && UsePerEyeCameras;

            if (_prevUsePerEyeCameras != _usePerEyeCameras)
            {
                _prevUsePerEyeCameras = _usePerEyeCameras;
                UpdateDisplay();
            }

            // Debug.Log("CoreHMD Update");
            //if (!useFixedUpdateForTracking)
            //    UpdateTrackingSpace();
        }

        // void OnDestroy()
        // {
        // }

        /** \brief This is a function to update CoreHMD Gameobject. */
        public void SetCameraPose()
        {
            var tc = SLAMHandler.Instance.CameraTransform;
            Quaternion quat = tc.Rotation;
            Vector3 pos = tc.Position;

            if (m_KeepInitialTransform)
            {
                quat = _initTransform.rotation * quat;
                pos = _initTransform.MultiplyPoint3x4(pos);
            }

            transform.localRotation = quat;
            transform.localPosition = pos;
        }

        /** \brief This is a function to leave app if keyCode is Escape or LeftArrow. */
        public void OnAppLeave(KeyCode keyCode)
        {
            if (keyCode == KeyCode.Escape || keyCode == KeyCode.LeftArrow)
                Application.Quit();
        }

        void InjectGameObject()
        {
            TrackingSpace = GameObject.Find($"{gameObject.name}/{TrackingSpaceName}").transform;
            DisplayAnchor = GameObject.Find($"{gameObject.name}/{DisplayName}").transform;
            _leftEyeCamera = GameObject.Find($"{gameObject.name}/{LeftEyeName}").GetComponent<Camera>();
            _rightEyeCamera = GameObject.Find($"{gameObject.name}/{RightEyeName}").GetComponent<Camera>();
        }

        void OnValidate()
        {
            _usePerEyeCameras = UsePerEyeCameras;

            if (!UseHMDStructure) m_HMDStructureConfigPath = "";

            InjectGameObject();
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            Debug.Log($"CoreHMD UpdateDisplay usePerEyeCameras {_usePerEyeCameras}");
            _rightEyeCamera.enabled = _usePerEyeCameras;


            if (_usePerEyeCameras)
            {
                _rightEyeCamera.rect = new Rect(0.5f, 0, 0.5f, 1);
                _leftEyeCamera.rect = new Rect(0, 0, 0.5f, 1);

                if (PassThroughMode)
                {
                    _rightEyeCamera.usePhysicalProperties = false;
                    _rightEyeCamera.fieldOfView = m_FocalOfViewForPassThrough;
                    _leftEyeCamera.usePhysicalProperties = false;
                    _leftEyeCamera.fieldOfView = m_FocalOfViewForPassThrough;
                }
                else
                {
                    _rightEyeCamera.usePhysicalProperties = true;
                    _rightEyeCamera.focalLength = FocalLength;
                    _rightEyeCamera.sensorSize = SensorSize;
                    _leftEyeCamera.usePhysicalProperties = true;
                    _leftEyeCamera.focalLength = FocalLength;
                    _leftEyeCamera.sensorSize = SensorSize;
                }
                if (mainCanvas){
                    mainCanvas.renderMode = RenderMode.WorldSpace;
                }
            }
            else
            {
                _leftEyeCamera.rect = new Rect(0, 0, 1, 1);

                if (PassThroughMode)
                {
                    _leftEyeCamera.usePhysicalProperties = false;
                    _leftEyeCamera.fieldOfView = m_FocalOfViewForPassThrough;
                }
                else
                {
                    _leftEyeCamera.usePhysicalProperties = true;
                    _leftEyeCamera.focalLength = FocalLength;
                    _leftEyeCamera.sensorSize = SensorSize;
                }
                if (mainCanvas){
                    mainCanvas.renderMode = RenderMode.ScreenSpaceCamera;
                }
            }

            if (UseHMDStructure)
            {
                DisplayAnchor.localPosition = _hmdStructure.DisplayPos;
                DisplayAnchor.localRotation = Quaternion.Euler(_hmdStructure.DisplayRot);
                RightEyeAnchor.localPosition = _hmdStructure.RightPos;
                RightEyeAnchor.localRotation = Quaternion.Euler(_hmdStructure.RightRot);
                LeftEyeAnchor.localPosition = _hmdStructure.LeftPos;
                LeftEyeAnchor.localRotation = Quaternion.Euler(_hmdStructure.LeftRot);
            }
        }

        public bool SaveHMDStructure()
        {
            if (m_HMDStructureConfigPath.Length > 0)
            {
                HMDStructureBuilder.Write(m_HMDStructureConfigPath, LeftEyeAnchor, RightEyeAnchor, DisplayAnchor);
            }

            return false;
        }
    }
}
