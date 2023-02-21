using System;
using UnityEngine;

namespace Coretronic.Reality
{
    public class TransformManager
    {
        public static GlassStructure DefaultArgosStructure = new GlassStructure() 
        {
            TrackingSpace = new TransformComponent()
            {
                Position = Vector3.zero,
                Rotation = Quaternion.identity
            },
            Display = new TransformComponent() 
            {
                Position = Vector3.zero,
                Rotation = Quaternion.Euler(-5, 0, 0)
            },
            LeftEye = new TransformComponent() 
            {
                Position = new Vector3(-0.04f, -0.025f, -0.025f),
                Rotation = Quaternion.identity
            },
            RightEye = new TransformComponent() 
            {
                Position = new Vector3(0, -0.025f, -0.025f),
                Rotation = Quaternion.identity
            }
        };

        private static TransformManager _inst;

        public static TransformManager Instance
        {
            get
            {
                if (_inst == null) 
                {
                    _inst = new TransformManager();
                    _inst._argosStructure = DefaultArgosStructure;
                }
                
                return _inst;
            }
        }

        //define
        //------------------------------------------------------------------------------------//
        //class
        private class SystemProperties
        {
            public static string Get(string key)
            {
                string result = "";
                try
                {
                    AndroidJavaClass base_class = new AndroidJavaClass("android.os.SystemProperties");
                    result = base_class.CallStatic<string>("get", key);
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.Message);
                }

                // Debug.Log($"SystemProperties key {key} result {result}");
                return result;
            }

            public static void Set(string key, string value)
            {
                try
                {
                    AndroidJavaClass base_class = new AndroidJavaClass("android.os.SystemProperties");
                    base_class.CallStatic("set", key, value);
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.Message);
                }
            }
        }

        public struct GlassStructure
        {
            public TransformComponent TrackingSpace;
            public TransformComponent Display;
            public TransformComponent LeftEye;
            public TransformComponent RightEye;
        }

        public enum StateType
        {
            Running,
            Success,
            Fail,
            Timeout
        } 
        //------------------------------------------------------------------------------------//

        //member
        //------------------------------------------------------------------------------------//     
        //flag
        private GlassStructure _argosStructure;
        public GlassStructure ArgosStructure => _argosStructure;

        //time
        private DateTime _startTimeRead;
        private DateTime _startTimeWrite;

        private bool _isComplete = true;

        public StateType ReadState { get; private set; } = StateType.Success;
        public StateType WriteState { get; private set; } = StateType.Success;
        //------------------------------------------------------------------------------------//

        //operator
        //------------------------------------------------------------------------------------//
        public bool ReadFromGlass()
        {
            if (!_isComplete) return false;
            SystemProperties.Set("vendor.venus.nvm", "read");
            _startTimeRead = DateTime.Now;
            ReadState = StateType.Running;
            _isComplete = false;
            return true;
        }

        public void CheckResultRead()
        {
            if (!_isComplete)
            {
                if (SystemProperties.Get("vendor.venus.nvm") == "OK")
                {
                    ReadState = SetGlassStructure() ? StateType.Success : StateType.Fail;
                    _isComplete = true;
                }
                else if ((DateTime.Now - _startTimeRead).TotalMilliseconds > 15000)
                {
                    ReadState = StateType.Timeout;
                    _isComplete = true;
                }
            }
        }

        public bool WriteToGlass(GlassStructure structure)
        {
            if (!_isComplete) return false;
            //clear flag
            _isComplete = false;
            WriteState = StateType.Running;
            _argosStructure = structure;
            Debug.Log($"Argos TrackingSpace {_argosStructure.TrackingSpace.Rotation.eulerAngles.ToString("F1")} {_argosStructure.TrackingSpace.Position.ToString("F3")}");
            Debug.Log($"Argos Display {_argosStructure.Display.Rotation.eulerAngles.ToString("F1")} {_argosStructure.Display.Position.ToString("F3")}");
            Debug.Log($"Argos LeftEye {_argosStructure.LeftEye.Rotation.eulerAngles.ToString("F1")} {_argosStructure.LeftEye.Position.ToString("F3")}");
            Debug.Log($"Argos RightEye {_argosStructure.RightEye.Rotation.eulerAngles.ToString("F1")} {_argosStructure.RightEye.Position.ToString("F3")}");
            var pos = _argosStructure.TrackingSpace.Position;
            var rot = _argosStructure.TrackingSpace.Rotation.eulerAngles;
            SystemProperties.Set("vendor.venus.ts.pos.x", string.Format("{0:0.####}", pos.x));
            SystemProperties.Set("vendor.venus.ts.pos.y", string.Format("{0:0.####}", pos.y));
            SystemProperties.Set("vendor.venus.ts.pos.z", string.Format("{0:0.####}", pos.z));
            SystemProperties.Set("vendor.venus.ts.rot.x", string.Format("{0:0.#}", rot.x));
            SystemProperties.Set("vendor.venus.ts.rot.y", string.Format("{0:0.#}", rot.y));
            SystemProperties.Set("vendor.venus.ts.rot.z", string.Format("{0:0.#}", rot.z));

            pos = _argosStructure.Display.Position;
            rot = _argosStructure.Display.Rotation.eulerAngles;
            SystemProperties.Set("vendor.venus.camera.pos.x", string.Format("{0:0.####}", pos.x));
            SystemProperties.Set("vendor.venus.camera.pos.y", string.Format("{0:0.####}", pos.y));
            SystemProperties.Set("vendor.venus.camera.pos.z", string.Format("{0:0.####}", pos.z));
            SystemProperties.Set("vendor.venus.camera.rot.x", string.Format("{0:0.#}", rot.x));
            SystemProperties.Set("vendor.venus.camera.rot.y", string.Format("{0:0.#}", rot.y));
            SystemProperties.Set("vendor.venus.camera.rot.z", string.Format("{0:0.#}", rot.z));

            pos = _argosStructure.LeftEye.Position;
            rot = _argosStructure.LeftEye.Rotation.eulerAngles;
            SystemProperties.Set("vendor.venus.left.pos.x", string.Format("{0:0.####}", pos.x));
            SystemProperties.Set("vendor.venus.left.pos.y", string.Format("{0:0.####}", pos.y));
            SystemProperties.Set("vendor.venus.left.pos.z", string.Format("{0:0.####}", pos.z));
            SystemProperties.Set("vendor.venus.left.rot.x", string.Format("{0:0.#}", rot.x));
            SystemProperties.Set("vendor.venus.left.rot.y", string.Format("{0:0.#}", rot.y));
            SystemProperties.Set("vendor.venus.left.rot.z", string.Format("{0:0.#}", rot.z));

            pos = _argosStructure.RightEye.Position;
            rot = _argosStructure.RightEye.Rotation.eulerAngles;
            SystemProperties.Set("vendor.venus.right.pos.x", string.Format("{0:0.####}", pos.x));
            SystemProperties.Set("vendor.venus.right.pos.y", string.Format("{0:0.####}", pos.y));
            SystemProperties.Set("vendor.venus.right.pos.z", string.Format("{0:0.####}", pos.z));
            SystemProperties.Set("vendor.venus.right.rot.x", string.Format("{0:0.#}", rot.x));
            SystemProperties.Set("vendor.venus.right.rot.y", string.Format("{0:0.#}", rot.y));
            SystemProperties.Set("vendor.venus.right.rot.z", string.Format("{0:0.#}", rot.z));

            //send command
            SystemProperties.Set("vendor.venus.nvm", "write");

            //set time
            _startTimeWrite = DateTime.Now;
            return true;
        }

        public void CheckResultWrite()
        {
            if (!_isComplete)
            {
                if (SystemProperties.Get("vendor.venus.nvm") == "OK")
                {
                    WriteState = StateType.Success;
                    _isComplete = true;
                }
                else if ((DateTime.Now - _startTimeWrite).TotalMilliseconds > 15000)
                {
                    WriteState = StateType.Timeout;
                    _isComplete = true;
                }
            }
        }
        //------------------------------------------------------------------------------------//

        //raed
        //------------------------------------------------------------------------------------//
        private bool SetGlassStructure()
        {
            GlassStructure structure; 
            float posx, posy, posz, rotx, roty, rotz; 
            posx = ParseFloat("vendor.venus.ts.pos.x");
            posy = ParseFloat("vendor.venus.ts.pos.y");
            posz = ParseFloat("vendor.venus.ts.pos.z");
            rotx = ParseFloat("vendor.venus.ts.rot.x");
            roty = ParseFloat("vendor.venus.ts.rot.y");
            rotz = ParseFloat("vendor.venus.ts.rot.z");
            structure.TrackingSpace.Position = new Vector3(posx, posy, posz);
            structure.TrackingSpace.Rotation = Quaternion.Euler(rotx, roty, rotz);
            posx = ParseFloat("vendor.venus.camera.pos.x");
            posy = ParseFloat("vendor.venus.camera.pos.y");
            posz = ParseFloat("vendor.venus.camera.pos.z");
            rotx = ParseFloat("vendor.venus.camera.rot.x");
            roty = ParseFloat("vendor.venus.camera.rot.y");
            rotz = ParseFloat("vendor.venus.camera.rot.z");
            structure.Display.Position = new Vector3(posx, posy, posz);
            structure.Display.Rotation = Quaternion.Euler(rotx, roty, rotz);
            posx = ParseFloat("vendor.venus.left.pos.x");
            posy = ParseFloat("vendor.venus.left.pos.y");
            posz = ParseFloat("vendor.venus.left.pos.z");
            rotx = ParseFloat("vendor.venus.left.rot.x");
            roty = ParseFloat("vendor.venus.left.rot.y");
            rotz = ParseFloat("vendor.venus.left.rot.z");
            structure.LeftEye.Position = new Vector3(posx, posy, posz);
            structure.LeftEye.Rotation = Quaternion.Euler(rotx, roty, rotz);
            posx = ParseFloat("vendor.venus.right.pos.x");
            posy = ParseFloat("vendor.venus.right.pos.y");
            posz = ParseFloat("vendor.venus.right.pos.z");
            rotx = ParseFloat("vendor.venus.right.rot.x");
            roty = ParseFloat("vendor.venus.right.rot.y");
            rotz = ParseFloat("vendor.venus.right.rot.z");
            structure.RightEye.Position = new Vector3(posx, posy, posz);
            structure.RightEye.Rotation = Quaternion.Euler(rotx, roty, rotz);
            _argosStructure = structure;
            Debug.Log($"Argos TrackingSpace {structure.TrackingSpace.Rotation.eulerAngles.ToString("F1")} {structure.TrackingSpace.Position.ToString("F3")}");
            Debug.Log($"Argos Display {structure.Display.Rotation.eulerAngles.ToString("F1")} {structure.Display.Position.ToString("F3")}");
            Debug.Log($"Argos LeftEye {structure.LeftEye.Rotation.eulerAngles.ToString("F1")} {structure.LeftEye.Position.ToString("F3")}");
            Debug.Log($"Argos RightEye {structure.RightEye.Rotation.eulerAngles.ToString("F1")} {structure.RightEye.Position.ToString("F3")}");
            return true;
        }

        private float ParseFloat(string key) => float.TryParse(SystemProperties.Get(key), out float result) ? result : 0;
        //------------------------------------------------------------------------------------//
    }
}
