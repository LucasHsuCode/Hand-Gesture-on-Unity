using System;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Coretronic.Reality.Clients;
using Coretronic.Reality.Utils;

namespace Coretronic.Reality
{
    /**
     * \brief FrameProvider is a provider to provide the frame
     * in the format of Unity Texture.
     */
    public class FrameProvider : MonoBehaviour
    {
        /** \brief Get the static class instance of FrameProvider created on Awake state. */
        public static FrameProvider Instance { get; private set; }

        /** \brief Make FrameProvider try to use rectified image. */
        [SerializeField]
        private bool m_TryToUseRectifiedImage = false;

        /** \brief Make FrameProvider try to use grayscale image. */
        [SerializeField]
        private bool m_TryToUseGrayImage = false;

        /** \brief Enable color texture of left and right frame.  */
        public bool EnableColorTexture = true; 

        /** \brief Enable depth texture.   */
        public bool EnableDepthTexture = true; 
        
        /** \brief TextureEvent is a class inherited from UnityEvent. */
        [Serializable]
        public class TextureEvent : UnityEvent<Texture> { }

        /** \brief TextureEvent is a class inherited from UnityEvent. */
        [Serializable]
        public class StateEvent : UnityEvent { }
        
        /** \brief Notifies upon the color texture of left frame is created. */
        public TextureEvent LeftTextureBinding = new TextureEvent();

        /** \brief Notifies upon the color texture of right frame is created. */
        public TextureEvent RightTextureBinding = new TextureEvent();

        /** \brief Notifies upon depth texture is created. */
        public TextureEvent DepthTextureBinding = new TextureEvent();

        /** \brief Notifies upon streaming start. */
        public StateEvent OnStart = new StateEvent();
        
        /** \brief Fired when a new frame is available. */
        public StateEvent OnFrames = new StateEvent(); 
        
        /** \brief Notifies when streaming has stopped. */
        public StateEvent OnStop = new StateEvent();  

        private bool _enable;
        private FrameProviderClient _fpc => FrameProviderClient.Instance;
        private int _fpcWidth;
        private int _fpcHeight;
        private int _fpcBytes1;
        private int _fpcBytes2;
        private StereoClient _sc => StereoClient.Instance;
        private int _scWidth;
        private int _scHeight;
        private int _scBytes1;
        private int _scBytes2;
        private int _scBytes3;
        private double[] _timestamp = new double[1] { -1 };
        
        /** \brief Check if FrameProvider uses QVR API. */
        public bool IsQvrApi => (CameraMode == LogicalCameraType.QvrStereoColor || CameraMode == LogicalCameraType.QvrStereoTracking);

        public bool UseGrayImage { get; private set; } = false;

        /** \brief Check if FrameProvider uses rectified Image. */
        private bool UseRectifiedImage => m_TryToUseRectifiedImage && IsQvrApi;

        private LogicalCameraType CameraMode = LogicalCameraType.None;

        /** \brief Check if FrameProvider is running. */
        public bool Running { get; private set; } = false;

        /** \brief The timestamp from frame handle in milliseconds. */
        public double Timestamp { get; private set; } = -1;

        private int ImageWidth => UseRectifiedImage ? _scWidth : _fpcWidth;

        private int ImageHeight => UseRectifiedImage ? _scHeight : _fpcHeight;

        /** \brief Get the color texture of left frame. */
        public Texture2D LeftTexture { get; private set; }
        
        /** \brief Get the color texture of right frame. */
        public Texture2D RightTexture { get; private set; }

        /** \brief Get the depth texture. */
        public Texture2D DepthTexture { get; private set; }

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

        void OnDisable()
        {
            if (_enable)
            {
                _enable = false;
                Running = false;
                OnStop?.Invoke();
            }
        }

        int ParseInt(IntPtr addr)
        {
            int[] singleIntVal = new int[1] { 0 };
            Marshal.Copy(addr, singleIntVal, 0, singleIntVal.Length);
            return singleIntVal[0];
        }

        void SetFrameProviderBytes()
        {
            SharedMemory paramMem = _fpc.GetParameters();
            IntPtr paramBuf = paramMem.ReadLock();
            _fpcWidth = ParseInt(paramBuf);
            _fpcHeight = ParseInt(paramBuf + 4);

            switch (CameraMode)
            {
                case LogicalCameraType.None: 
                    Debug.Log($"get none camera mode");
                    break; 
                case LogicalCameraType.RsColorDepth:
                    _fpcBytes1 = ParseInt(paramBuf + 8);
                    _fpcBytes2 = ParseInt(paramBuf + 12);
                    break;
                case LogicalCameraType.RsStereoIr:
                    _fpcBytes1 = ParseInt(paramBuf + 8);
                    _fpcBytes2 = _fpcBytes1;
                    break;
                case LogicalCameraType.QvrStereoTracking:
                case LogicalCameraType.QvrStereoColor:
                    _fpcBytes1 = ParseInt(paramBuf + 12);
                    _fpcBytes2 = _fpcBytes1;
                    break;
            }

            paramMem.Unlock(paramBuf);
            // Debug.Log($"FrameProvider {CameraMode} {_fpcWidth} {_fpcHeight} {_fpcBytes1} {_fpcBytes2}");
        }

        void SetStereoBytes() 
        {
            SharedMemory paramMem = _sc.GetParameters();
            IntPtr paramBuf = paramMem.ReadLock();
            _scWidth = ParseInt(paramBuf);
            _scHeight = ParseInt(paramBuf + 4);
            _scBytes1 = ParseInt(paramBuf + 12);
            _scBytes2 = _scBytes1;
            _scBytes3 = ParseInt(paramBuf + 16);
            paramMem.Unlock(paramBuf);
            // Debug.Log($"FrameProvider {CameraMode} {_scWidth} {_scHeight} {_scBytes1} {_scBytes2} {_scBytes3}");
        }

        void SetTexture()
        {
            Debug.Log($"FrameProvider {CameraMode} {ImageWidth} x {ImageHeight}");
            DisposeUtil.TryDispose(LeftTexture);
            DisposeUtil.TryDispose(RightTexture);
            DisposeUtil.TryDispose(DepthTexture);
            LeftTexture = null;
            RightTexture = null;
            DepthTexture = null;

            switch (CameraMode)
            {
                case LogicalCameraType.None: 
                    break;
                case LogicalCameraType.RsColorDepth:
                    UseGrayImage = false;
                    LeftTexture = new Texture2D(ImageWidth, ImageHeight, TextureFormat.RGB24, false, false)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear
                    };
                    RightTexture = LeftTexture;
                    DepthTexture = new Texture2D(ImageWidth, ImageHeight, TextureFormat.R16, false, true)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Point
                    };
                    break;
                case LogicalCameraType.RsStereoIr:
                    UseGrayImage = true;
                    LeftTexture = new Texture2D(ImageWidth, ImageHeight, TextureFormat.R8, false, false)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear
                    };
                    RightTexture = new Texture2D(ImageWidth, ImageHeight, TextureFormat.R8, false, false)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear
                    };
                    break;
                case LogicalCameraType.QvrStereoTracking:
                    UseGrayImage = true;
                    DepthTexture = new Texture2D(_scWidth, _scHeight, TextureFormat.R16, false, true)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Point
                    };
                    LeftTexture = new Texture2D(ImageWidth, ImageHeight, TextureFormat.R8, false, false)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear
                    };
                    RightTexture = new Texture2D(ImageWidth, ImageHeight, TextureFormat.R8, false, false)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear
                    };
                    break;  
                case LogicalCameraType.QvrStereoColor:
                    UseGrayImage = m_TryToUseGrayImage;
                    LeftTexture = new Texture2D(ImageWidth, ImageHeight, UseGrayImage ? TextureFormat.R8 : TextureFormat.RGB24, false, false)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear
                    };
                    RightTexture = new Texture2D(ImageWidth, ImageHeight, UseGrayImage ? TextureFormat.R8 : TextureFormat.RGB24, false, false)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear
                    };
                    DepthTexture = new Texture2D(_scWidth, _scHeight, TextureFormat.R16, false, true)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Point
                    };
                    break; 
            }

            LeftTextureBinding.Invoke(LeftTexture);
            RightTextureBinding.Invoke(RightTexture);
            DepthTextureBinding.Invoke(DepthTexture);
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
            if (_enable && _fpc.IsConnect && _sc.IsConnect)
            {
                CameraMode = _fpc.GetCameraMode();
                SetFrameProviderBytes();
                if (IsQvrApi) SetStereoBytes();
                SetTexture();
                OnStart.Invoke();
                Running = true;
            }
            else
            {
                _enable = false;
            }
        }

        void OnDestroy()
        {
            Debug.Log($"FrameProvider OnDestroy");
            DisposeUtil.TryDispose(LeftTexture);
            DisposeUtil.TryDispose(RightTexture);
            DisposeUtil.TryDispose(DepthTexture);
            LeftTexture = null;
            RightTexture = null;
            DepthTexture = null;
        }
        
        void Update()
        {
            if (!Running) return;
            
            switch (CameraMode)
            {
                case LogicalCameraType.None: 
                    break; 
                case LogicalCameraType.RsColorDepth:
                    using (var mem = _fpc.GetFrameBuffer())
                    {
                        if (mem != null)
                        {
                            IntPtr raw1Buf = mem.ReadLock();
                            IntPtr raw2Buf = raw1Buf + _fpcBytes1;
                            IntPtr timeBuf = raw2Buf + _fpcBytes2;
                            Marshal.Copy(timeBuf, _timestamp, 0, _timestamp.Length);

                            if (_timestamp[0] > Timestamp)
                            {
                                Timestamp = _timestamp[0];

                                if (EnableColorTexture)
                                {
                                    CoreLibraryUtil.Flip(raw1Buf, LeftTexture, 3, 0);
                                    LeftTexture.Apply(false, false);
                                }
                                if (EnableDepthTexture)
                                {
                                    CoreLibraryUtil.Flip(raw2Buf, DepthTexture, 2, 0);
                                    DepthTexture.Apply(false, false); 
                                }
                            }
                            
                            mem.Unlock(raw1Buf);
                        }
                    }
                    break;
                case LogicalCameraType.RsStereoIr:
                    using (var mem = _fpc.GetFrameBuffer())
                    {
                        if (mem != null)
                        {
                            IntPtr raw1Buf = mem.ReadLock();
                            IntPtr raw2Buf = raw1Buf + _fpcBytes1;
                            IntPtr timeBuf = raw2Buf + _fpcBytes2;
                            Marshal.Copy(timeBuf, _timestamp, 0, _timestamp.Length);

                            if (_timestamp[0] > Timestamp)
                            {
                                Timestamp = _timestamp[0];
                                
                                if (EnableColorTexture)
                                {
                                    CoreLibraryUtil.Flip(raw1Buf, LeftTexture, 1, 0);
                                    LeftTexture.Apply(false, false);
                                    CoreLibraryUtil.Flip(raw2Buf, RightTexture, 1, 0);
                                    RightTexture.Apply(false, false);
                                }
                            }
                            
                            mem.Unlock(raw1Buf);
                        }
                    }
                    break;
                case LogicalCameraType.QvrStereoTracking:
                {
                    int frameIdx = _sc.GetCurrentFrameIndex();
                    var mem = _sc.GetFrameBuffer(frameIdx);
                    bool update = false;

                    if (mem != null)
                    {
                        IntPtr lBuf = mem.ReadLock();
                        IntPtr rBuf = lBuf + _scBytes1;
                        IntPtr dBuf = rBuf + _scBytes2;
                        IntPtr tBuf = dBuf + _scBytes3;
                        Marshal.Copy(tBuf, _timestamp, 0, _timestamp.Length);
                        update = _timestamp[0] > Timestamp;

                        if (update)
                        {
                            Timestamp = _timestamp[0];
                            if (UseRectifiedImage && EnableColorTexture)
                            {
                                CoreLibraryUtil.Flip(lBuf, LeftTexture, 1, 0);
                                LeftTexture.Apply(false, false);
                                CoreLibraryUtil.Flip(rBuf, RightTexture, 1, 0);
                                RightTexture.Apply(false, false);
                            }
                            if (EnableDepthTexture)
                            {
                                CoreLibraryUtil.Flip(dBuf, DepthTexture, 2, 0);
                                DepthTexture.Apply(false, false); 
                            }
                        }
                        
                        mem.Unlock(lBuf);
                    }
                    if (update && !UseRectifiedImage && EnableColorTexture)
                    {
                        mem = _fpc.GetFrameBuffer(frameIdx);

                        if (mem != null)
                        {
                            IntPtr raw1Buf = mem.ReadLock();
                            IntPtr raw2Buf = raw1Buf + _fpcBytes1;
                            CoreLibraryUtil.Flip(raw1Buf, LeftTexture, 1, 0);
                            LeftTexture.Apply(false, false);
                            CoreLibraryUtil.Flip(raw2Buf, RightTexture, 1, 0);
                            RightTexture.Apply(false, false);
                            mem.Unlock(raw1Buf);
                        }
                    }
                }
                    break;
                case LogicalCameraType.QvrStereoColor:
                {
                    int frameIdx = _sc.GetCurrentFrameIndex();
                    var mem = _sc.GetFrameBuffer(frameIdx);
                    bool update = false;

                    if (mem != null)
                    {
                        IntPtr lBuf = mem.ReadLock();
                        IntPtr rBuf = lBuf + _scBytes1;
                        IntPtr dBuf = rBuf + _scBytes2;
                        IntPtr tBuf = dBuf + _scBytes3;
                        Marshal.Copy(tBuf, _timestamp, 0, _timestamp.Length);
                        update = _timestamp[0] > Timestamp;

                        if (update)
                        {
                            Timestamp = _timestamp[0];
                            if (UseRectifiedImage && EnableColorTexture)
                            {
                                if (UseGrayImage)
                                {
                                    CoreLibraryUtil.Flip(lBuf, LeftTexture, 1, 0);
                                    CoreLibraryUtil.Flip(rBuf, RightTexture, 1, 0);
                                }
                                else
                                {
                                    CoreLibraryUtil.CovertNv12ToRGB(lBuf, LeftTexture);
                                    CoreLibraryUtil.Flip(LeftTexture, LeftTexture, 3, 0);
                                    CoreLibraryUtil.CovertNv12ToRGB(rBuf, RightTexture);
                                    CoreLibraryUtil.Flip(RightTexture, RightTexture, 3, 0);
                                }

                                LeftTexture.Apply(false, false);
                                RightTexture.Apply(false, false);
                            }
                            if (EnableDepthTexture)
                            {
                                CoreLibraryUtil.Flip(dBuf, DepthTexture, 2, 0);
                                DepthTexture.Apply(false, false); 
                            }
                        }
                        
                        mem.Unlock(lBuf);
                    }
                    if (update && !UseRectifiedImage && EnableColorTexture)
                    {
                        mem = _fpc.GetFrameBuffer(frameIdx);

                        if (mem != null)
                        {
                            IntPtr raw1Buf = mem.ReadLock();
                            IntPtr raw2Buf = raw1Buf + _fpcBytes1;

                            if (UseGrayImage)
                            {
                                CoreLibraryUtil.Flip(raw1Buf, LeftTexture, 1, 0);
                                CoreLibraryUtil.Flip(raw2Buf, RightTexture, 1, 0);
                            }
                            else
                            {
                                CoreLibraryUtil.CovertNv12ToRGB(raw1Buf, LeftTexture);
                                CoreLibraryUtil.Flip(LeftTexture, LeftTexture, 3, 0);
                                CoreLibraryUtil.CovertNv12ToRGB(raw2Buf, RightTexture);
                                CoreLibraryUtil.Flip(RightTexture, RightTexture, 3, 0);
                            }
                            
                            LeftTexture.Apply(false, false);
                            RightTexture.Apply(false, false);
                            mem.Unlock(raw1Buf);
                        }
                    }
                }
                    break;
            }

            OnFrames.Invoke();
        }
    }
}
