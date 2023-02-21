using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using Coretronic.Reality.Clients;

namespace Coretronic.Reality
{

    /** \brief A SLAM handler in Unity */
    public class SLAMHandler : MonoBehaviour
    {
        /** \brief An enum of statuses */
        public enum StateType
        {
            Off = 1,  /**< Visual SLAM is not initialized */
            Tracking = 2,  /**< Tracking */
            RecentlyLost = 3,  /**< Visual SLAM is recently lost */
            Lost = 4,  /**< Visual SLAM is lost */
            DetectPlaneFailed = 1234,  /**< A plane is not detected */
            DetectPlaneSuccess = 233,  /**< A plane is detected */
        }

        private const int _matrixBufferSize = 16 * 4;
        private const int _resultBufferSize = 4;
        private const int _timestampBufferSize = 8;

        private const string serverName = "http://203.145.218.176:9090";  // NCHC
        private const string uploadFile1 = "MAPS/test.osa";
        private const string uploadFile2 = "TxtFiles/modelMatrix.yaml";
        private const string downloadFile = "test.osa";
        private const string downloadFile2 = "modelMatrix.yaml";
        private const string saveModelMPath = "/storage/emulated/0/TxtFiles/modelMatrix.yaml";
        private const string loadModelMPath = "/storage/emulated/0/CoreDownloads/down_modelMatrix.yaml";

        private const string saveAtlasPath = "/storage/emulated/0/MAPS/test";
        private const string loadAtlasPath = "/storage/emulated/0/CoreDownloads/down_test";

        public bool whDownloadAtlas = false;

        /** \brief Get the static class instance of SLAMHandler created on Awake state. */
        public static SLAMHandler Instance { get; private set; }

        /** \brief SlamEvent is a class inherited from UnityEvent. */
        [Serializable]
        public class SlamEvent : UnityEvent { }

        /** \brief Relatived Mode. */
        public bool RelativedMode = false;

        /** \brief Initialize SLAM by loading the map. */
        public bool InitialWithMap = false;

        /** \brief Full path to save the map */
        [Tooltip("When setting MapFullPath to empty string, SLAM would not load map and cannot use SaveMap function")]
        public string MapFullPath = "";

        [Space]
        /** \brief Notify the new pose, a Quaternion, a Vector3, and an enum called Status, after SLAM processes frames */
        public SlamEvent CameraMatrixBinding = new SlamEvent();

        /** \brief Notify the new pose, a Quaternion, a Vector3, and an enum called Status, after detected plane processes frames */
        public SlamEvent DetectedPlaneBinding = new SlamEvent();

        private bool _enable = false;
        private SlamClient _client => SlamClient.Instance;
        private int[] _slamResult = new int[1];
        private double[] _slamTimestamp = new double[1];
        private float[] _cameraMatrix = new float[16];
        private int[] _detectedResult = new int[1];
        private double[] _detectedTimestamp = new double[1];
        private float[] _modelMatrix = new float[16];
        private Matrix4x4 _cameraMat44 = Matrix4x4.identity;
        private Matrix4x4 _modelMat44 = Matrix4x4.identity;
        private readonly object _objectLock = new object();
        
        /** \brief Check if SLAMHandler is running. */
        public bool Running { get; private set; } = false;

        /** \brief The tracking state of visual SLAM */
        public StateType State => (StateType) _slamResult[0]; 

        /** \brief The detected-plane state of visual SLAM */
        public StateType DetectedState => (StateType) _detectedResult[0]; 

        private TransformComponent _cameraTrans;

        private TransformComponent _planeTrans;

        /** \brief The transform component for camera */
        public TransformComponent CameraTransform => _cameraTrans;

        /** \brief The transform component for detected-plane */
        public TransformComponent PlaneTransform => _planeTrans;

        /** \brief Frame per second */
        public double FPS => _client.GetFramePerSecond();

        /** \brief Current Map ID */
        public int MapId => _client.GetMapId();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        void OnEnable()
        {
            if (!_enable) 
            {
                _enable = true;
                StartCoroutine(WaitAndStart());
            }
        }

        IEnumerator WaitAndStart()
        {
            bool flag = true;

            while (flag)
            {
                if (ClientsManager.Instance)
                {
                    flag = _enable && !ClientsManager.Instance.ConnectedClients;
                }
                
                yield return new WaitForSeconds(0.1f);
            }

            if (whDownloadAtlas)
            {
                int downloadState = httpClientDownload();
                if (downloadState == 0)
                {
                    whDownloadAtlas = false;
                    Debug.Log("Download a map and a model matrix successfully");
                }
                else
                {
                    Debug.Log("Fail to download a map and a model matrix");
                }

            }
            else
            {
                deleteOldFiles();
                Debug.Log("Delete old maps and model matrices");
            }

            if (_enable && _client.IsConnect)
            {
                if (InitialWithMap)
                {
                    Debug.Log($"SLAMHandler Initial {MapFullPath}");
                    _client.Initial(MapFullPath);
                }
                else
                {
                    _client.Initial("");
                }
                
                Running = true;
            }
            else
            {
                _enable = false;
            }
        }

        void OnDisable()
        {
            if (_enable) 
            {
                _enable = false;
            }
        }

        void OnDestroy()
        {
            Debug.Log($"SLAMHandler OnDestroy");
        }

        void GetCameraPose()
        {
            lock (_objectLock)
            {
                var mem = _client.GetResultMemory();
                if (mem == null) return;
                IntPtr r1Buf = mem.ReadLock();
                IntPtr t1Buf = r1Buf + _resultBufferSize;
                IntPtr m1Buf = t1Buf + _timestampBufferSize;
                Marshal.Copy(r1Buf, _slamResult, 0, _slamResult.Length);
                Marshal.Copy(t1Buf, _slamTimestamp, 0, _slamTimestamp.Length);
                Marshal.Copy(m1Buf, _cameraMatrix, 0, _cameraMatrix.Length);
                mem.Unlock(r1Buf);
            }
        }

        void GetModelPose()
        {
            lock (_objectLock)
            {
                var mem = _client.GetResultMemory();
                if (mem == null) return;
                IntPtr r1Buf = mem.ReadLock();
                IntPtr r2Buf = r1Buf + _resultBufferSize + _timestampBufferSize + _matrixBufferSize;
                IntPtr t2Buf = r2Buf + _resultBufferSize;
                IntPtr m2Buf = t2Buf + _timestampBufferSize;
                Marshal.Copy(r2Buf, _detectedResult, 0, _detectedResult.Length);
                Marshal.Copy(t2Buf, _detectedTimestamp, 0, _detectedTimestamp.Length);
                Marshal.Copy(m2Buf, _modelMatrix, 0, _modelMatrix.Length);
                mem.Unlock(r1Buf);
            }
        }

        void Update()
        {
            if (!Running) return;
            GetCameraPose();

            if (State == StateType.Tracking)
            {
                _cameraMat44[0, 0] = _cameraMatrix[0];
                _cameraMat44[1, 0] = _cameraMatrix[1];
                _cameraMat44[2, 0] = _cameraMatrix[2];
                _cameraMat44[3, 0] = _cameraMatrix[3];
                _cameraMat44[0, 1] = _cameraMatrix[4];
                _cameraMat44[1, 1] = _cameraMatrix[5];
                _cameraMat44[2, 1] = _cameraMatrix[6];
                _cameraMat44[3, 1] = _cameraMatrix[7];
                _cameraMat44[0, 2] = _cameraMatrix[8];
                _cameraMat44[1, 2] = _cameraMatrix[9];
                _cameraMat44[2, 2] = _cameraMatrix[10];
                _cameraMat44[3, 2] = _cameraMatrix[11];
                _cameraMat44[0, 3] = _cameraMatrix[12];
                _cameraMat44[1, 3] = _cameraMatrix[13];
                _cameraMat44[2, 3] = _cameraMatrix[14];
                _cameraMat44[3, 3] = _cameraMatrix[15];
                var mat44 = _cameraMat44.inverse;

                if (RelativedMode && DetectedState == StateType.DetectPlaneSuccess)
                {
                    var absCamQuat = mat44.rotation;
                    var absModelQuat = _modelMat44.rotation;
                    var quatCam = new Quaternion(-absCamQuat.x, -absCamQuat.y, absCamQuat.z, absCamQuat.w);
                    var quatPlane = new Quaternion(-absModelQuat.x, -absModelQuat.y, absModelQuat.z, absModelQuat.w);
                    Quaternion relCamQuat = Quaternion.Inverse(quatPlane) * quatCam;
                    _cameraTrans.Rotation = relCamQuat;

                    var absCamPos = mat44.GetColumn(3);
                    var invModelMat44 = _modelMat44.inverse;
                    var relCamPos = invModelMat44.MultiplyPoint3x4(new Vector3(absCamPos.x, absCamPos.y, absCamPos.z));
                    _cameraTrans.Position = new Vector3(relCamPos.x, relCamPos.y, -relCamPos.z);
                }
                else
                {
                    var ea = mat44.rotation.eulerAngles;
                    _cameraTrans.Rotation = Quaternion.Euler(-ea.x, -ea.y, ea.z);
                    var pos = mat44.GetColumn(3);
                    _cameraTrans.Position = new Vector3(pos.x, pos.y, -pos.z);
                }
            }
           
            CameraMatrixBinding?.Invoke();
        }

        /**
         * \brief Detect a plane in the 3D point cloud.
         */
        public void DetectPlane()
        {
            if (_client.DetectPlane()) 
            {
                GetModelPose();
                _modelMat44[0, 0] = _modelMatrix[0];
                _modelMat44[1, 0] = _modelMatrix[1];
                _modelMat44[2, 0] = _modelMatrix[2];
                _modelMat44[3, 0] = _modelMatrix[3];
                _modelMat44[0, 1] = _modelMatrix[4];
                _modelMat44[1, 1] = _modelMatrix[5];
                _modelMat44[2, 1] = _modelMatrix[6];
                _modelMat44[3, 1] = _modelMatrix[7];
                _modelMat44[0, 2] = _modelMatrix[8];
                _modelMat44[1, 2] = _modelMatrix[9];
                _modelMat44[2, 2] = _modelMatrix[10];
                _modelMat44[3, 2] = _modelMatrix[11];
                _modelMat44[0, 3] = _modelMatrix[12];
                _modelMat44[1, 3] = _modelMatrix[13];
                _modelMat44[2, 3] = _modelMatrix[14];
                _modelMat44[3, 3] = _modelMatrix[15];

                writeModelMtoFile(saveModelMPath, _modelMatrix);

                var ea = _modelMat44.rotation.eulerAngles;
                var pos = _modelMat44.GetColumn(3);
                _planeTrans.Rotation = Quaternion.Euler(-ea.x, -ea.y, ea.z);
                _planeTrans.Position = new Vector3(pos.x, pos.y, -pos.z);
            }

            DetectedPlaneBinding?.Invoke();
        }

        /**
         * \brief Get the model matrix of a previously detected plane from a file.
         */
        public void DetectPlaneV2()
        {
            _detectedResult[0] = 233;
            readModelMfromFileV2(loadModelMPath, _modelMatrix);

            _modelMat44[0, 0] = _modelMatrix[0];
            _modelMat44[1, 0] = _modelMatrix[1];
            _modelMat44[2, 0] = _modelMatrix[2];
            _modelMat44[3, 0] = _modelMatrix[3];
            _modelMat44[0, 1] = _modelMatrix[4];
            _modelMat44[1, 1] = _modelMatrix[5];
            _modelMat44[2, 1] = _modelMatrix[6];
            _modelMat44[3, 1] = _modelMatrix[7];
            _modelMat44[0, 2] = _modelMatrix[8];
            _modelMat44[1, 2] = _modelMatrix[9];
            _modelMat44[2, 2] = _modelMatrix[10];
            _modelMat44[3, 2] = _modelMatrix[11];
            _modelMat44[0, 3] = _modelMatrix[12];
            _modelMat44[1, 3] = _modelMatrix[13];
            _modelMat44[2, 3] = _modelMatrix[14];
            _modelMat44[3, 3] = _modelMatrix[15];

            var ea = _modelMat44.rotation.eulerAngles;
            var pos = _modelMat44.GetColumn(3);
            _planeTrans.Rotation = Quaternion.Euler(-ea.x, -ea.y, ea.z);
            _planeTrans.Position = new Vector3(pos.x, pos.y, -pos.z);

            DetectedPlaneBinding?.Invoke();
        }

        /**
         * \brief Save the map with the specified path.
         * @param mapPath The specified path
         * @param restart Restart SLAM
         */
        public bool SaveMap(string mapPath, bool restart)
        {
            MapFullPath = mapPath;
            return _client.SaveMap(MapFullPath, restart);
        }

        /**
         * \brief Save the map with the default path.
         * @param restart Restart SLAM
         */
        public bool SaveMap(bool restart)
        {
            return _client.SaveMap(MapFullPath, restart);
        }

        /**
         * \brief Upload maps and model matrices to an http server.
         */
        public int httpClientUpload()
        {
            int upStatuses;
            int upStatus1 = -1;
            int upStatus2 = -1;

            for (int i = 0; i < 5; i++)
            {   
                upStatus1 = httpClientUpload(serverName, uploadFile1);
                if (upStatus1 == 0)
                {
                    break;
                }
            }

            for (int i = 0; i < 5; i++)
            {
                upStatus2 = httpClientUpload(serverName, uploadFile2);
                if (upStatus2 == 0)
                {
                    break;
                }
            }

            if (upStatus1 == 0 && upStatus2 == 0)
            {
                upStatuses = 0;  // upload successfully
            }
            else
            {
                upStatuses = 1;  // upload unsuccessfully
            }

            return upStatuses;
        }

        /**
         * \brief Download maps and model matrices from an http server.
         */
        public int httpClientDownload()
        {
            int downStatuses;
            int downStatus1 = -1;
            int downStatus2 = -1;

            for (int i = 0; i < 5; i++)
            {
                downStatus1 = httpClientDownload(serverName, downloadFile);
                if (downStatus1 == 0)
                {
                    break;
                }
            }

            for (int i = 0; i < 5; i++)
            {
                downStatus2 = httpClientDownload(serverName, downloadFile2);
                if (downStatus2 == 0)
                {
                    break;
                }
            }

            if (downStatus1 == 0 && downStatus2 == 0)
            {
                downStatuses = 0;  // download successfully
            }
            else
            {
                downStatuses = 1;  // download unsuccessfully
            }

            return downStatuses;
        }

        /**
         * \brief Delete maps and model matrices stored in an Argos pre evt.
         */
        public void deleteOldFiles()
        {
            string filePath1 = saveAtlasPath + ".osa";
            string filePath2 = saveModelMPath;
            string filePath3 = loadAtlasPath + ".osa";
            string filePath4 = loadModelMPath;
            
            if (File.Exists(filePath1))
            {
                Debug.Log(filePath1 + " file already exists, deleting ...");
                File.Delete(filePath1);
            }

            if (File.Exists(filePath2))
            {
                Debug.Log(filePath2 + " file already exists, deleting ...");
                File.Delete(filePath2);
            }

            if (File.Exists(filePath3))
            {
                Debug.Log(filePath3 + " file already exists, deleting ...");
                File.Delete(filePath3);
            }

            if (File.Exists(filePath4))
            {
                Debug.Log(filePath4 + " file already exists, deleting ...");
                File.Delete(filePath4);
            }
        }




        private const string LIBNAME = "SLAM_Native2";

        [DllImport(LIBNAME)]
        private static extern void writeModelMtoFile(string path, float[] modelM);
        [DllImport(LIBNAME)]
        private static extern void readModelMfromFileV2(string path, float[] modelM);
        [DllImport(LIBNAME)]
        private static extern int httpClientUpload(string ser, string fil);
        [DllImport(LIBNAME)]
        private static extern int httpClientDownload(string ser, string fil);
        
    }
}
