using System.Threading;
using UnityEngine;
using Coretronic.Reality.Utils;

namespace Coretronic.Reality.Clients
{
    /** \brief A C# wrapper of StereoClient functions. */
    public class StereoClient
    {
        private static StereoClient _inst = null;
        /** \brief An instance of StereoClient class. */
        
        public static StereoClient Instance
        {
            get
            {
                if (_inst == null)
                {
                    CoreLibraryUtil.Init();
                    _inst = new StereoClient();
                }

                return _inst;
            }
        }
        private AndroidJavaObject _unityContext;
        private AndroidJavaObject _stereoClient;
        public bool IsConnect { get; private set; } = false;
        
        private class StereoClientCallback : AndroidOnConnectionListener 
        {
            private StereoClient _parent;

            public StereoClientCallback(StereoClient parent)
            {
                _parent = parent;
            }

            public override void onConnect(bool isConnect) 
            {
                Debug.Log($"StereoClient is connected? {isConnect}");
                _parent.IsConnect = isConnect;
            }
        }
        
        private StereoClient() 
        { 
            var javaUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            _unityContext = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            _stereoClient = new AndroidJavaObject("com.coretronic.clients.StereoClient", new StereoClientCallback(this));
        }
        
        ~StereoClient()  
        {  
            StopService();
        }
        
        public void StartService()
        {
            _stereoClient.Call("startService", _unityContext); 
            // Thread.Sleep(100);
        }

        public void StopService()
        {
            if (IsConnect) _stereoClient?.Call("stopService", _unityContext); 
        }

        public SharedMemory GetFrameBuffer()
        {
            if (!IsConnect) return null;
            var obj = _stereoClient.Call<AndroidJavaObject>("getFrameBuffer");
            return obj == null ? null : new SharedMemory(obj);
        }

        public SharedMemory GetParameters()
        {
            if (!IsConnect) return null;
            var obj = _stereoClient.Call<AndroidJavaObject>("getParameters");
            return obj == null ? null : new SharedMemory(obj);
        }

        public ServiceState GetServiceState()
        {
            if (!IsConnect) return ServiceState.ProxyNull;
            AndroidJavaObject nativeObject = _stereoClient.Call<AndroidJavaObject>("getServiceState");
            int stateVal = nativeObject.Call<int>("getValue");
            return (ServiceState) stateVal;
        }

        public int GetCurrentFrameIndex()
        {
            if (!IsConnect) return -1;
            return _stereoClient.Call<int>("getCurrentFrameIndex");
        }

        public SharedMemory GetFrameBuffer(int index)
        {
            if (!IsConnect) return null;
            var obj = _stereoClient.Call<AndroidJavaObject>("getFrameBuffer", index);
            return obj == null ? null : new SharedMemory(obj);
        }
    }
}