using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Coretronic.Reality;
using TMPro;

using HandJoints = Coretronic.Reality.Hand.Joints;
using HandTypes = Coretronic.Reality.Hand.Types;

public class handsample : MonoBehaviour
{
    [SerializeField] CoreHand rightHand;
    [SerializeField] CoreHand leftHand;
    [SerializeField] RawImage cameraView;
    [SerializeField] bool showFPS = true;
    [SerializeField] bool showGUI = true;
    [SerializeField] bool showBone = true;
    [SerializeField] TextMeshProUGUI rightHandText;
    [SerializeField] TextMeshProUGUI leftHandText;
    [SerializeField] TextMeshProUGUI fpsText;

    CoreHMD coreHMD;
    PrimitiveDraw rightDraw;
    PrimitiveDraw leftDraw;

    void Awake()
    {
        coreHMD = FindObjectOfType<CoreHMD>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if (coreHMD.UsePerEyeCameras)
        {   
            rightDraw = new PrimitiveDraw(coreHMD.RightEyeCamera);
            leftDraw = new PrimitiveDraw(coreHMD.LeftEyeCamera);
        }
        else
        {
            rightDraw = new PrimitiveDraw();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (rightHandText) 
        {
            rightHandText.text = getHandString(rightHand);
        }
        if (leftHandText) 
        {
            leftHandText.text = getHandString(leftHand);
        }
        if (fpsText)
        {
            var str = CoreHandProvider.Instance.FPS.ToString("F2");
            fpsText.text = $"FPS: {str}";
        }
    }

    void OnDestroy()
    {
        rightDraw?.Dispose();
        rightDraw = null;

        if (coreHMD.UsePerEyeCameras)
        {
            leftDraw?.Dispose();
            leftDraw = null;
        }
    }

    void LateUpdate() 
    {
        if (showBone)
        {
            showHand(rightHand);
            showHand(leftHand);
        }
    }

    void OnGUI () 
    {
        GUIStyle fontStyle = new GUIStyle();
        fontStyle.normal.textColor = new Color(255,0,0);	//設置字體顏色
        fontStyle.fontSize = 40;	//字體大小
        if (showFPS)
        {
            GUI.Label(new UnityEngine.Rect(45, 40, 450, 200), $"FPS {CoreHandProvider.Instance.FPS}", fontStyle);
        }
        if (showGUI)
        {
            
            // Debug info for Gravity and Quaternion
            // GUI.Label(new UnityEngine.Rect(45, 40, 450, 200), $"Hand FPS {coreHMD.handProvider.fps.ToString("#.00")}", fontStyle);
            GUI.Label(new UnityEngine.Rect(45, 80, 450, 200), $"RH Gesture {getHandString(rightHand)}", fontStyle);
            GUI.Label(new UnityEngine.Rect(45, 120, 450, 200), $"LH Gesture {getHandString(leftHand)}", fontStyle);
        }
	}

    string getHandString(CoreHand hand) 
    {
        var str = hand.isActiveAndEnabled ? hand.gesture.strictType.ToString() : "no hand";
        if (str.Contains("OK")) str = "OK";
        return str;
    }

    void showHand(CoreHand handObject)
    {
        if (handObject.isActiveAndEnabled)
        {
            Vector3[] jarr = handObject.ToPositionArray();
            DrawProjJoints(jarr, handObject.HandType is HandTypes.Right ? Color.red : Color.blue);
        }
    }

    void DrawProjJoints(Vector3[] joints, Color color)
    {
        DrawColor(color);
        // Cube
        for (int i = 0; i < joints.Length; i++)
        {
            // draw.Cube(joints[i], 0.005f);
            DrawCube(joints[i], 0.005f);

        }

        for (int ii = 0; ii < joints.Length; ii++) {
            HandJoints.JointName parent = HandJoints.ParentJointArray[ii];

            if (parent != HandJoints.JointName.None)
            {
                // draw.Line3D(joints[(int) parent], joints[ii], 0.002f);
                DrawLine3D(joints[(int) parent], joints[ii], 0.002f);
                Debug.Log($"jarr[{ii}]: ({joints[ii].x}, {joints[ii].y}, {joints[ii].z})");

            }
        }
        DrawApply();
    }

    void DrawColor(Color color)
    {
        rightDraw.color = color;
        if (coreHMD.UsePerEyeCameras) leftDraw.color = color;
    }

    void DrawCube(Vector3 vec, float scale)
    {
        rightDraw.Cube(vec, scale);
        if (coreHMD.UsePerEyeCameras) leftDraw.Cube(vec, scale);
    }

    void DrawLine3D(Vector3 v1, Vector3 v2, float thickness)
    {
        rightDraw.Line3D(v1, v2, thickness);
        if (coreHMD.UsePerEyeCameras) leftDraw.Line3D(v1, v2, thickness);
    }

    void DrawApply()
    {
        rightDraw.Apply();
        if (coreHMD.UsePerEyeCameras) leftDraw.Apply();
    }
}
