using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Coretronic.Reality;
using Coretronic.Reality.Hand;
using Coretronic.Reality.Clients;
using HandType = Coretronic.Reality.Hand.Types;
using HandJoints = Coretronic.Reality.Hand.Joints;

namespace Coretronic.Reality
{
    /** \brief CoreHandProvider provides APIs for hand tracking. */
    public class CoreHandProvider : MonoBehaviour
    {
        private const int _meshBufferSize = 778 * 3 * 4;
        private const int _jointsBufferSize = 21 * 3 * 4;
        private const int _poseBufferSize = 16 * 3 * 4;
        private const int _rectBufferSize = 2 * 5 * 4;
        private const int _gestureBufferSize = 2 * 4;
        private const int _stateBufferSize = 4;
        private const int _timestampBufferSize = 8;

        private const int _showState = 2;

        /** \brief Get the static class instance of CoreHandProvider created on Awake state. */
        public static CoreHandProvider Instance { get; private set; }

        /** \brief HandEvent is a class inherited from UnityEvent. */
        [Serializable]
        public class HandEvent : UnityEvent { }
        
        /** \brief Notify the new hand result after hand tracking. */
        public HandEvent HandBinding = new HandEvent();

        private bool _enable = false;
        private HandTrackingClient _client => HandTrackingClient.Instance;
        private double[] _timestamp = new double[1];
        private int[] _leftState = new int[1];
        private int[] _leftGesture = new int[2];   // 0: Rule, 1: Type
        private float[] _leftRect = new float[10];   // 0~4: left frame, 5~9: right frame
        private float[] _leftJoints = new float[21 * 3];
        private float[] _leftMesh = new float[778 * 3];
        private float[] _leftPose = new float[16 * 3];
        private int[] _rightState = new int[1];
        private int[] _rightGesture = new int[2];  // 0: Rule, 1: Type
        private float[] _rightRect = new float[10];  // 0~4: left frame, 5~9: right frame
        private float[] _rightJoints = new float[21 * 3];
        private float[] _rightMesh = new float[778 * 3];
        private float[] _rightPose = new float[16 * 3];
        
        /** \brief The current activation state of the left hand. */
        public bool LeftShow => _leftState[0] == _showState;

        /** \brief The current joints of the left hand. */
        public Joints LeftJoints { get; private set; } = null;

        /** \brief The current activation state of the right hand. */
        public bool RightShow => _rightState[0] == _showState;

        /** \brief The current joints of the right hand. */
        public Joints RightJoints { get; private set; } = null;
        
        /** \brief Frame per Second. */
        public double FPS => _client.GetFramePerSecond();

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

            LeftJoints = new Joints(_leftJoints);

            RightJoints = new Joints(_rightJoints);
        }

        void OnEnable()
        {
            if (!_enable) 
            {
                _enable = true;
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
            Debug.Log($"CoreHandProvider OnDestroy");
        }

        void Update()
        {
            if (!_client.IsConnect) return;
            var mem = _client.GetResultMemory();
            if (mem == null) return;
            IntPtr t1Buf = mem.ReadLock();
            IntPtr lStateBuf = t1Buf + _timestampBufferSize;
            IntPtr lGestureBuf = lStateBuf + _stateBufferSize; 
            IntPtr lRectBuf = lGestureBuf + _gestureBufferSize;
            IntPtr lWorldJointsBuf = lRectBuf + _rectBufferSize;
            IntPtr lMeshBuf = lWorldJointsBuf + _jointsBufferSize;
            IntPtr lPoseBuf = lMeshBuf + _meshBufferSize;
            IntPtr rStateBuf = lPoseBuf + _poseBufferSize;
            IntPtr rGestureBuf = rStateBuf + _stateBufferSize; 
            IntPtr rRectBuf = rGestureBuf + _gestureBufferSize;
            IntPtr rWorldJointsBuf = rRectBuf + _rectBufferSize;
            IntPtr rMeshBuf = rWorldJointsBuf + _jointsBufferSize;
            IntPtr rPoseBuf = rMeshBuf + _meshBufferSize;
            Marshal.Copy(t1Buf, _timestamp, 0, _timestamp.Length);
            Marshal.Copy(lStateBuf, _leftState, 0, _leftState.Length);
            Marshal.Copy(lGestureBuf, _leftGesture, 0, _leftGesture.Length);
            Marshal.Copy(lRectBuf, _leftRect, 0, _leftRect.Length);
            Marshal.Copy(lWorldJointsBuf, _leftJoints, 0, _leftJoints.Length);
            Marshal.Copy(lMeshBuf, _leftMesh, 0, _leftMesh.Length);
            Marshal.Copy(lPoseBuf, _leftPose, 0, _leftPose.Length);
            Marshal.Copy(rStateBuf, _rightState, 0, _rightState.Length);
            Marshal.Copy(rGestureBuf, _rightGesture, 0, _rightGesture.Length);
            Marshal.Copy(rRectBuf, _rightRect, 0, _rightRect.Length);
            Marshal.Copy(rWorldJointsBuf, _rightJoints, 0, _rightJoints.Length);
            Marshal.Copy(rMeshBuf, _rightMesh, 0, _rightMesh.Length);
            Marshal.Copy(rPoseBuf, _rightPose, 0, _rightPose.Length);
            mem.Unlock(t1Buf);
            AlignCVToUnity(_leftJoints);
            AlignCVToUnity(_rightJoints);
            AlignCVToUnity(_leftMesh);
            AlignCVToUnity(_rightMesh);
            HandBinding?.Invoke();
        }

        private static void AlignCVToUnity(float[] out3d)
        {
            for (int i = 1; i < out3d.Length; i += 3) out3d[i] = -out3d[i];
            for (int i = 0; i < out3d.Length; i++) out3d[i] *= 0.001f;
        }
    }
}