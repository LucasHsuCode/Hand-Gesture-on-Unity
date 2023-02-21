using System.Threading;
using UnityEngine;
using Coretronic.Reality.Utils;

namespace Coretronic.Reality.Clients
{
    /** \brief A C# wrapper of SlamClient functions. */
    public class SlamClient
    {
        private static SlamClient _inst = null;

        /** \brief An instance of SlamClient class. */
        public static SlamClient Instance
        {
            get
            {
                if (_inst == null)
                {
                    CoreLibraryUtil.Init();
                    _inst = new SlamClient();
                }

                return _inst;
            }
        }

        private AndroidJavaObject _unityContext;
        private AndroidJavaObject _slamClient;
        public bool IsConnect { get; private set; } = false;

        private class SlamClientCallback : AndroidOnConnectionListener 
        {
            private SlamClient _parent;

            public SlamClientCallback(SlamClient parent)
            {
                _parent = parent;
            }

            public override void onConnect(bool isConnect) 
            {
                Debug.Log($"SlamClient is connected? {isConnect}");
                _parent.IsConnect = isConnect;
            }
        }

        private SlamClient() 
        { 
            var javaUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            _unityContext = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            _slamClient = new AndroidJavaObject("com.coretronic.clients.SlamClient", new SlamClientCallback(this));
        }

        ~SlamClient()  
        {  
            StopService();
        }

        public void StartService()
        {
            _slamClient.Call("startService", _unityContext); 
            // Thread.Sleep(100);
        }

        public void StopService()
        {
            if (IsConnect) _slamClient?.Call("stopService", _unityContext); 
        }

        public SharedMemory GetResultMemory()
        {
            if (!IsConnect) return null;
            var obj = _slamClient.Call<AndroidJavaObject>("getResultMemory"); 
            return obj == null ? null : new SharedMemory(obj);
        }

        public double GetFramePerSecond()
        {
            if (!IsConnect) return -1;
            return _slamClient.Call<double>("getFramePerSecond"); 
        }

        public bool DetectPlane()
        {
            if (!IsConnect) return false;
            var res = _slamClient.Call<int>("detectPlane"); 
            return res != 0;
        }
        
        public ServiceState GetServiceState()
        {
            if (!IsConnect) return ServiceState.ProxyNull;
            AndroidJavaObject nativeObject = _slamClient.Call<AndroidJavaObject>("getServiceState");
            int stateVal = nativeObject.Call<int>("getValue");
            return (ServiceState) stateVal;
        }

        public int GetMapId() 
        {
            if (!IsConnect) return -1;
            return _slamClient.Call<int>("getMapId");
        }

        public bool Initial(string mapPath) 
        {
            if (!IsConnect) return false;
            return _slamClient.Call<bool>("initial", mapPath);
        }

        public bool Initial() 
        {
            if (!IsConnect) return false;
            return _slamClient.Call<bool>("initial");
        }

        public bool SaveMap(string mapPath, bool restart) 
        {
            if (!IsConnect) return false;
            return _slamClient.Call<bool>("saveMap", mapPath, restart);
        }
    }
}
