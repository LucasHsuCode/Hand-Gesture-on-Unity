using UnityEditor;
using UnityEngine;
using UnityEditor.Events;
using HandJoints = Coretronic.Reality.Hand.Joints;
using HandTypes = Coretronic.Reality.Hand.Types;

namespace Coretronic.Reality
{
    public class CoreXR2GameObject : Editor
    {
        [MenuItem("GameObject/CoreXR2/CoreHMD", false, 13)]
        public static void AddCoreHMD()
        {
            //game object
            GameObject core_hmd = new GameObject("CoreHMD");
            GameObject tracking_space = new GameObject("TrackingSpace");
            GameObject display = CreateDisplay();

            //set parent
            display.transform.parent = tracking_space.transform;
            tracking_space.transform.parent = core_hmd.transform;

            //core hmd
            CoreHMD core_hmd_component = core_hmd.AddComponent<CoreHMD>();
            // core_hmd_component.useFixedUpdateForTracking = false;

            CoreKeypad keypad = core_hmd.AddComponent<CoreKeypad>();
            keypad.SuppoertedCode = new CoreKeypad.SupportedKeyState[8];
            keypad.SuppoertedCode[0].Code = KeyCode.LeftArrow;
            keypad.SuppoertedCode[0].LongPressTrigger = true;
            keypad.SuppoertedCode[0].ContinuousTrigger = true;
            keypad.SuppoertedCode[1].Code = KeyCode.RightArrow;
            keypad.SuppoertedCode[1].LongPressTrigger = false;
            keypad.SuppoertedCode[1].ContinuousTrigger = true;
            keypad.SuppoertedCode[2].Code = KeyCode.F6;
            keypad.SuppoertedCode[2].LongPressTrigger = false;
            keypad.SuppoertedCode[2].ContinuousTrigger = true;
            keypad.SuppoertedCode[3].Code = KeyCode.F7;
            keypad.SuppoertedCode[3].LongPressTrigger = false;
            keypad.SuppoertedCode[3].ContinuousTrigger = true;
            keypad.SuppoertedCode[4].Code = KeyCode.Menu;
            keypad.SuppoertedCode[4].LongPressTrigger = false;
            keypad.SuppoertedCode[4].ContinuousTrigger = true;
            keypad.SuppoertedCode[5].Code = KeyCode.Delete;
            keypad.SuppoertedCode[5].LongPressTrigger = false;
            keypad.SuppoertedCode[5].ContinuousTrigger = true;
            keypad.SuppoertedCode[6].Code = KeyCode.Backspace;
            keypad.SuppoertedCode[6].LongPressTrigger = false;
            keypad.SuppoertedCode[6].ContinuousTrigger = true;
            keypad.SuppoertedCode[7].Code = KeyCode.Escape;
            keypad.SuppoertedCode[7].LongPressTrigger = false;
            keypad.SuppoertedCode[7].ContinuousTrigger = true;
            UnityEventTools.AddPersistentListener(keypad.OnKeyLongPress, core_hmd_component.OnAppLeave);

            // ClientsManager
            ClientsManager clientsManager = core_hmd.AddComponent<ClientsManager>();

            // frame provider
            FrameProvider frame_provider = core_hmd.AddComponent<FrameProvider>();

            // slam handler
            SLAMHandler slam_handler = core_hmd.AddComponent<SLAMHandler>();
            UnityEventTools.AddPersistentListener(slam_handler.CameraMatrixBinding, core_hmd_component.SetCameraPose);
            
            //hand provider
            CoreHandProvider core_hand_provider = core_hmd.AddComponent<CoreHandProvider>();
            // core_hand_provider.handDetectorModel = "coretronic/model/IHPoseNetLite7_256.tflite";
            // core_hand_provider.handLandmarkModel = "coretronic/model/hand_landmark_v2.tflite";
        }

        [MenuItem("GameObject/CoreXR2/CoreHands", false, 13)]
        public static void AddCoreHands()
        {
            GameObject core_right_hand = AddCoreHand(HandTypes.Right);
            CoreHand core_right_hand_component = core_right_hand.AddComponent<CoreHand>();
            core_right_hand_component.HandType = HandTypes.Right;

            GameObject core_left_hand = AddCoreHand(HandTypes.Left);
            CoreHand core_left_hand_component = core_left_hand.AddComponent<CoreHand>();
            core_left_hand_component.HandType = HandTypes.Left;
        }

        private static GameObject CreateDisplay()
        {
            //argos camera
            GameObject display = new GameObject("Display");
            //left eye
            GameObject left_eye = new GameObject("LeftEye");
            CreateUnityCamera(left_eye);
            left_eye.transform.parent = display.transform;
            //right eye
            GameObject right_eye = new GameObject("RightEye");
            CreateUnityCamera(right_eye);
            right_eye.transform.parent = display.transform;
            display.AddComponent<AudioListener>();
            return display;
        }

        private static void CreateUnityCamera(GameObject camera_object)
        {
            //camera
            Camera camera = camera_object.AddComponent<Camera>();
            camera.enabled = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.usePhysicalProperties = true;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 2000;
            camera.depth = 0;
            camera.useOcclusionCulling = false;
            camera.allowHDR = false;
            //camera object
            camera_object.tag = "MainCamera";
            camera_object.AddComponent<FlareLayer>();
        }

        private static GameObject AddCoreHand(HandTypes type)
        {
            //result
            GameObject result = new GameObject(type == HandTypes.Left ? "CoreLeftHand" : (type == HandTypes.Right ? "CoreRightHand" : "CoreHand"));

            //create joint game object
            foreach (HandJoints.JointName joint_name in HandJoints.JointNameArray)
            {
                GameObject joint = new GameObject(joint_name.ToString());
                joint.transform.parent = result.transform;
            }

            //create ray start anchor
            GameObject ray_start_anchor = new GameObject("RayStartAnchor");
            ray_start_anchor.transform.parent = result.transform;

            return result;
        }
    }
}