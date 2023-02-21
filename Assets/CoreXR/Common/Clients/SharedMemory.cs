using System;
using UnityEngine;

namespace Coretronic.Reality.Clients
{
    public class SharedMemory : IDisposable
    {
        public AndroidJavaObject NativeObject { get; private set; }

        public SharedMemory(AndroidJavaObject o)
        {
            NativeObject = o;
        }

        public IntPtr ReadLock()
        {
            return new IntPtr(NativeObject.Call<long>("readLock"));
        } 

        public IntPtr WriteLock()
        {
            return new IntPtr(NativeObject.Call<long>("writeLock"));
        } 

        public bool Unlock(IntPtr handler)
        {
            return NativeObject.Call<bool>("unlock", handler.ToInt64());
        }

        public void Dispose()
        {
            NativeObject = null;
        }
    }
}