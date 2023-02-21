using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

namespace Coretronic.Reality.Utils
{
    public static class CoreLibraryUtil
    {
        private static bool _isInit = false;

        public static void Init()
        {
            if (_isInit) return;
            _isInit = true;
            using (var system = new AndroidJavaClass("java.lang.System"))
                system.CallStatic("loadLibrary", "corelibrary_jni");
        }

        // public unsafe static void Flip(byte[] src, Texture2D tex, int channel, int code)
        // {
        //     fixed (byte* s = src)
        //     {
        //         IntPtr texPtr = (IntPtr) NativeArrayUnsafeUtility.GetUnsafePtr(tex.GetRawTextureData<byte>());
        //         CoreLibraryUtilFlip(tex.width, tex.height, channel, code, (IntPtr) s, texPtr);
        //     }
        // }

        public unsafe static void Flip(Texture2D src, Texture2D dst, int channel, int code)
        {
            IntPtr srcPtr = (IntPtr) NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(src.GetRawTextureData<byte>());
            IntPtr dstPtr = (IntPtr) NativeArrayUnsafeUtility.GetUnsafePtr(dst.GetRawTextureData<byte>());
            CoreLibraryUtilFlip(src.width, src.height, channel, code, srcPtr, dstPtr);
        }


        public unsafe static void Flip(IntPtr src, Texture2D tex, int channel, int code)
        {
            IntPtr texPtr = (IntPtr) NativeArrayUnsafeUtility.GetUnsafePtr(tex.GetRawTextureData<byte>());
            CoreLibraryUtilFlip(tex.width, tex.height, channel, code, src, texPtr);
        }

        public unsafe static void Flip(Texture2D tex, byte[] dst, int channel, int code)
        {
            IntPtr texPtr = (IntPtr) NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(tex.GetRawTextureData<byte>());
            
            fixed (byte* d = dst)
            {
                CoreLibraryUtilFlip(tex.width, tex.height, channel, code, texPtr, (IntPtr) d);
            }
            
        }

        public unsafe static void CovertNv12ToRGB(IntPtr src, Texture2D tex)
        {
            IntPtr texPtr = (IntPtr) NativeArrayUnsafeUtility.GetUnsafePtr(tex.GetRawTextureData<byte>());
            CoreLibraryUtilCovertNv12ToRGB(tex.width, tex.height, src, texPtr);
        }

        private const string LIBNAME = "corelibrary_jni";

        [DllImport(LIBNAME)]
        private unsafe static extern void CoreLibraryUtilFlip(int width, int height, int channel, int code, IntPtr src, IntPtr dst);

        [DllImport(LIBNAME)]
        private unsafe static extern void CoreLibraryUtilCovertNv12ToRGB(int width, int height, IntPtr src, IntPtr dst);
    }
}