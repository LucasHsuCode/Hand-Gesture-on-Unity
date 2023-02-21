using System.Threading;
using UnityEngine;
using Coretronic.Reality.Utils;

namespace Coretronic.Reality.Clients
{
    /** \brief A C# wrapper of FrameProviderClient functions. */
    public class FrameProviderClient
    {
        private static FrameProviderClient _inst;

        /** \brief An instance of FrameProviderClient class. */
        public static FrameProviderClient Instance
        {
            get
            {
                if (_inst == null)
                {
                    CoreLibraryUtil.Init();
                    _inst = new FrameProviderClient();
                }

                return _inst;
            }
        }

        private AndroidJavaObject _unityContext;
        private AndroidJavaObject _frameProviderClient;
        public bool IsConnect { get; private set; }

        private class FrameProviderClientCallback : AndroidOnConnectionListener
        {
            private FrameProviderClient _parent;

            public FrameProviderClientCallback(FrameProviderClient p)
            {
                _parent = p;
            }

            public override void onConnect(bool isConnect)
            {
                Debug.Log($"FrameProviderClient is connected? {isConnect}");
                _parent.IsConnect = isConnect;
            }
        }

        private FrameProviderClient()
        {
            var javaUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            _unityContext = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            _frameProviderClient = new AndroidJavaObject(
                "com.coretronic.clients.FrameProviderClient", new FrameProviderClientCallback(this));
        }

        ~FrameProviderClient()
        {
            StopService();
        }

        public void StartService()
        {
            _frameProviderClient.Call("startService", _unityContext);
            // Thread.Sleep(100);
        }

        public void StopService()
        {
            if (IsConnect) _frameProviderClient?.Call("stopService", _unityContext);
        }

        public SharedMemory GetFrameBuffer()
        {
            if (!IsConnect) return null;
            var obj = _frameProviderClient.Call<AndroidJavaObject>("getFrameBuffer");
            return obj == null ? null : new SharedMemory(obj);
        }

        public SharedMemory GetFrameBuffer(int index)
        {
            if (!IsConnect) return null;
            var obj = _frameProviderClient.Call<AndroidJavaObject>("getFrameBuffer", index);
            return obj == null ? null : new SharedMemory(obj);
        }

        public SharedMemory GetParameters()
        {
            if (!IsConnect) return null;
            var obj = _frameProviderClient.Call<AndroidJavaObject>("getParameters");
            return obj == null ? null : new SharedMemory(obj);
        }

        public ServiceState GetServiceState()
        {
            if (!IsConnect) return ServiceState.ProxyNull;
            AndroidJavaObject nativeObject = _frameProviderClient.Call<AndroidJavaObject>("getServiceState");
            int stateVal = nativeObject.Call<int>("getValue");
            return (ServiceState) stateVal;
        }

        public LogicalCameraType GetCameraMode()
        {
            if (!IsConnect) return LogicalCameraType.None;
            AndroidJavaObject nativeObject = _frameProviderClient.Call<AndroidJavaObject>("getCameraMode");
            int camModeVal = nativeObject.Call<int>("getValue");
            return (LogicalCameraType) camModeVal;
        }

        // public int GetRingBufferSize()
        // {
        //     if (!IsConnect) return -1;
        //     return _frameProviderClient.Call<int>("getRingBufferSize");
        // }

        public int GetCurrentFrameIndex()
        {
            if (!IsConnect) return -1;
            return _frameProviderClient.Call<int>("getCurrentFrameIndex");
        }
    }
}