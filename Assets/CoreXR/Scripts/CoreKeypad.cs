using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Coretronic.Reality.Tools;

namespace Coretronic.Reality
{
    /// <summary>
    /// Listen to key down event
    /// </summary>
    public class CoreKeypad : MonoBehaviour
    {
        /// <summary>
        /// Supported Key State struct
        /// </summary>
        [Serializable]
        public struct SupportedKeyState
        {
            public KeyCode Code;
            public bool ContinuousTrigger;
            public bool LongPressTrigger;
        }

        //define
        //------------------------------------------------------------------------------------//
        /// <summary>
        /// Key event class
        /// </summary>
        [Serializable]
        public class OnKeyEvent : UnityEvent<KeyCode> { }

        //key state
        private class KeyState
        {
            public DateTime Timestamp;
            public DateTime StartTime;
            public bool IsDown = false;
            public bool ToLongPressCallback = false;
            public bool UseContinuousState;
            public bool UseLongPressState;
        }
        //------------------------------------------------------------------------------------//

        //inspector
        //------------------------------------------------------------------------------------//
        /// <summary>
        /// Allow CoreKeypad to support user-defined key code and its state
        /// </summary>
        [SerializeField]
        public SupportedKeyState[] SuppoertedCode;

        /// <summary>
        /// Key down event
        /// </summary>
        public OnKeyEvent OnKeyDown = new OnKeyEvent();

        /// <summary>
        /// Key up event
        /// </summary>
        public OnKeyEvent OnKeyUp = new OnKeyEvent();

        /// <summary>
        /// Key long press event
        /// </summary>
        public OnKeyEvent OnKeyLongPress = new OnKeyEvent();

        /// <summary>
        /// Continuous sending OnKeyDown event period (unit millisecond)
        /// </summary>
        [Header("Continuous State")]
        public int RepeatedTime = 100;

        /// <summary>
        /// Long press trigger time (unit millisecond)
        /// </summary>
        [Header("Long Press State")]
        public int LongPressTime = 3000;
        
        //------------------------------------------------------------------------------------//

        //member
        //------------------------------------------------------------------------------------//
        private Dictionary<KeyCode, KeyState> _codeMap = new Dictionary<KeyCode, KeyState>();
        //------------------------------------------------------------------------------------//

        //life cycle
        //------------------------------------------------------------------------------------//
        private void Start()
        {
            _codeMap.Clear();

            foreach (var val in SuppoertedCode)
            {
                var code = val.Code;

                if (_codeMap.ContainsKey(code)) {
                    Debug.LogWarning($"KeyCode {code} is repeated");
                }

                _codeMap[code] = new KeyState();
                _codeMap[code].UseContinuousState = val.ContinuousTrigger;
                _codeMap[code].UseLongPressState = val.LongPressTrigger;
            }
        }

        private void Update()
        {
            foreach(var item in _codeMap)
            {
                if (item.Value.IsDown)
                {
                    if (item.Value.UseContinuousState && 
                        (DateTime.Now - item.Value.Timestamp).TotalMilliseconds > RepeatedTime) {
                        item.Value.Timestamp = DateTime.Now;
                        OnKeyDown.Invoke(item.Key);
                    }
                    if (item.Value.UseLongPressState && item.Value.ToLongPressCallback &&
                        (DateTime.Now - item.Value.StartTime).TotalMilliseconds > LongPressTime) {
                        item.Value.ToLongPressCallback = false;
                        OnKeyLongPress.Invoke(item.Key);
                    }
                    if (Input.GetKeyUp(item.Key))
                    {
                        item.Value.IsDown = false;
                        OnKeyUp.Invoke(item.Key);
                    }
                }
                else if (Input.GetKeyDown(item.Key))
                {
                    item.Value.IsDown = true;
                    item.Value.ToLongPressCallback = true;
                    item.Value.StartTime = DateTime.Now;
                    item.Value.Timestamp = item.Value.StartTime;
                    // OnKeyDown.Invoke(item.Key);
                }
            }
        }
        //------------------------------------------------------------------------------------//
    }
}
