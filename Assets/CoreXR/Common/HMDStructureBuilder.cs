using System;
using UnityEngine;

namespace Coretronic.Reality
{
    public class HMDStructureBuilder
    {
        [System.Serializable]
        public struct DefinedStructure
        {
            public Vector3 LeftRot;
            public Vector3 LeftPos;
            public Vector3 RightRot;
            public Vector3 RightPos;
            public Vector3 DisplayRot;
            public Vector3 DisplayPos;
        }

        public static DefinedStructure DefaultArgosStructure { get; private set; } = new DefinedStructure() 
        {
            LeftRot = new Vector3(0, 1.4f, 0),
            LeftPos = new Vector3(-0.032f, -0.024f, -0.030f),
            RightRot = new Vector3(0, -1.4f, 0),
            RightPos = new Vector3(0.032f, -0.024f, -0.030f),
            DisplayRot = new Vector3(0, 0, 0),
            DisplayPos = new Vector3(0, 0, 0),
        };

        public static DefinedStructure Read(string jsonPath)
        {
            if (System.IO.File.Exists(jsonPath))
            {
                var jsonStr = System.IO.File.ReadAllText(jsonPath);
                return JsonUtility.FromJson<DefinedStructure>(jsonStr);
            }
            
            return DefaultArgosStructure;
        }

        public static void Write(string jsonPath, Transform left, Transform right, Transform display)
        {
            DefinedStructure data = new DefinedStructure();
            data.LeftRot = left.localRotation.eulerAngles;
            data.LeftPos = left.localPosition;
            data.RightRot = right.localRotation.eulerAngles;
            data.RightPos = right.localPosition;
            data.DisplayRot = display.localRotation.eulerAngles;
            data.DisplayPos = display.localPosition;
            System.IO.File.WriteAllText(jsonPath, JsonUtility.ToJson(data));
        }
    }  
}