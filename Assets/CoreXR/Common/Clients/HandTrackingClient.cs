using System.Threading;
using UnityEngine;
using Coretronic.Reality.Utils;

namespace Coretronic.Reality.Clients
{
    /** \brief A C# wrapper of HandTrackingClient functions. */
    public class HandTrackingClient
    {
        private static HandTrackingClient _inst = null;

        /** \brief An instance of HandTrackingClient class. */
        public static HandTrackingClient Instance
        {
            get
            {
                if (_inst == null) 
                {
                    CoreLibraryUtil.Init();
                    _inst = new HandTrackingClient();
                }

                return _inst;
            }
        }

        private AndroidJavaObject _unityContext;
        private AndroidJavaObject _handTrackingClient;
        public bool IsConnect { get; private set; } = false;

        private class HandTrackingClientCallback : AndroidOnConnectionListener 
        {
            private HandTrackingClient _parent;

            public HandTrackingClientCallback(HandTrackingClient parent)
            {
                _parent = parent;
            }

            public override void onConnect(bool isConnect) 
            {
                Debug.Log($"HandTrackingClient is connected? {isConnect}");
                _parent.IsConnect = isConnect;
            }
        }

        private HandTrackingClient() 
        { 
            var javaUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            _unityContext = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            _handTrackingClient = new AndroidJavaObject(
                "com.coretronic.clients.HandTrackingClient", new HandTrackingClientCallback(this));
        }

        ~HandTrackingClient()  
        {  
            StopService();
        }

        public void StartService()
        {
            _handTrackingClient.Call("startService", _unityContext); 
            // Thread.Sleep(100);
        }

        public void StopService()
        {
            if (IsConnect) _handTrackingClient?.Call("stopService", _unityContext); 
        }


        public SharedMemory GetResultMemory()
        {
            if (!IsConnect) return null;
            var obj = _handTrackingClient.Call<AndroidJavaObject>("getResultMemory"); 
            return obj == null ? null : new SharedMemory(obj);
        }

        public double GetFramePerSecond()
        {
            if (!IsConnect) return -1;
            return _handTrackingClient.Call<double>("getFramePerSecond"); 
        }
        
        public ServiceState GetServiceState()
        {
            if (!IsConnect) return ServiceState.ProxyNull;
            AndroidJavaObject nativeObject = _handTrackingClient.Call<AndroidJavaObject>("getServiceState");
            int stateVal = nativeObject.Call<int>("getValue");
            return (ServiceState) stateVal;
        }
    }
}
