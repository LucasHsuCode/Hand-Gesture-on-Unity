using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Coretronic.Reality.Utils;

namespace Coretronic.Reality.Clients
{
    /** \brief A C# wrapper of CoreMemoryClient functions. */
    public class CoreMemoryClient
    {
        private static CoreMemoryClient _inst = null;

        /** \brief An instance of CoreMemoryClient class. */
        public static CoreMemoryClient Instance
        {
            get
            {
                if (_inst == null)
                {
                    CoreLibraryUtil.Init();
                    _inst = new CoreMemoryClient();
                }
                return _inst;
            }
        }

        private AndroidJavaObject _unityContext;
        private AndroidJavaObject _memoryClient;
        public bool IsConnect { get; private set; } = false;

        private class CoreMemoryClientCallback : AndroidOnConnectionListener 
        {
            private CoreMemoryClient _parent;

            public CoreMemoryClientCallback(CoreMemoryClient parent)
            {
                _parent = parent;
            }

            public override void onConnect(bool isConnect) 
            {
                Debug.Log($"CoreMemoryClient is connected? {isConnect}");
                _parent.IsConnect = isConnect;
            }
        }

        private CoreMemoryClient()
        {
            var javaUnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            _unityContext = currentActivity.Call<AndroidJavaObject>("getApplicationContext");
            _memoryClient = new AndroidJavaObject("com.coretronic.clients.CoreMemoryClient", new CoreMemoryClientCallback(this));
        }

        ~CoreMemoryClient()  
        {  
            StopService();
        }

        // public bool IsConnecting()    // Lucas add
        // {
        //     IsConnect = _memoryClient.Call<bool>("isConnecting", _unityContext);
        //     return IsConnect;
        // }
        
        public void StartService()
        {
            _memoryClient.Call("startService", _unityContext); 
            // Thread.Sleep(100);
        }

        public void StopService()
        {
            _memoryClient.Call("stopService", _unityContext); 
        }

        public List<string> GetSharedMemoryList()
        {
            if (!IsConnect) return null;
            var jList = _memoryClient.Call<AndroidJavaObject>("getSharedMemoryList"); 
            int count = jList.Call<int>("size");
            List<string> list = new List<string>(count);
            for(int i = 0; i < count; i++) list.Add(jList.Call<string>("get", i));
            return list;
        }

        public MemoryCertificate Register(string name, int bytes)
        {
            if (!IsConnect) return null;
            var obj = _memoryClient.Call<AndroidJavaObject>("register", name, bytes);
            return obj == null ? null : new MemoryCertificate(obj);
        }

        public void Unregister(MemoryCertificate certificate)
        {
            if (!IsConnect) return;
            if (certificate != null && certificate.NativeObject != null)
                _memoryClient.Call("unregister", certificate.NativeObject);
        }

        public MemoryAccesser Access(string regName)
        {
            if (!IsConnect) return null;
            return new MemoryAccesser(this, regName);
        }

        public void Close(MemoryAccesser accesser)
        {
            if (!IsConnect) return;
            if (accesser!= null && accesser.NativeObject != null)
                _memoryClient.Call("close", accesser.NativeObject);
        }

        public class MemoryCertificate
        {
            public AndroidJavaObject NativeObject { get; private set; }

            public MemoryCertificate(AndroidJavaObject o)
            {
                NativeObject = o;
                Debug.Log($"MemoryCertificate.NativeObject is null {NativeObject == null}.");
            }
        }

        public class MemoryAccesser
        {
            private class AndroidOnUnregisterListener : AndroidJavaProxy
            {
                private MemoryAccesser _accesser;

                public AndroidOnUnregisterListener(MemoryAccesser accesser) 
                    : base("com.coretronic.clients.CoreMemoryClient$OnUnregisterListener")
                {
                    _accesser = accesser;
                }

                public void onUnregister()
                {
                    _accesser.Release();
                }
            }

            public delegate void OnUnregisterHandler();
            
            public AndroidJavaObject NativeObject { get; private set; }
            public SharedMemory Memory { get; private set; } = null;
            public OnUnregisterHandler OnUnregister = null;

            public MemoryAccesser(CoreMemoryClient inst, string regName) 
            {
                var cb = new AndroidOnUnregisterListener(this);
                NativeObject = inst._memoryClient.Call<AndroidJavaObject>("access", regName, cb);
                Debug.Log($"MemoryAccesser.NativeObject is null {NativeObject == null}.");
                if (NativeObject != null) Memory = new SharedMemory(NativeObject.Call<AndroidJavaObject>("getMemory"));
            }

            public void Release()
            {
                Debug.Log("MemoryAccesser is release.");
                NativeObject = null;
                OnUnregister?.Invoke();
            }
        }
    }
}
