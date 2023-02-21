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
    [SerializeField] TextMeshProUGUI layoutText;

    CoreHMD coreHMD;
    PrimitiveDraw rightDraw;
    PrimitiveDraw leftDraw;

    int LayoutPage1 = 0;
    int LayoutPage2 = 0;


    void Awake()
    {
        coreHMD = FindObjectOfType<CoreHMD>();
    }

    // Start is called before the first frame update
    void Start()
    {
        layoutText.text = "Layout-1";
        // ...
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
        if (layoutText)
        {
            int page = getLayoutPage(leftHand);
            if (page == 1)
            {
                layoutText.text = "Layout-1";
            }
            else if (page == 2)
            {
                layoutText.text = "Layout-2";
            }
        }
    }

    string getHandString(CoreHand hand) 
    {
        var str = hand.isActiveAndEnabled ? hand.gesture.strictType.ToString() : "no hand";
        if (str.Contains("OK")) str = "OK";
        return str;
    }

    int getLayoutPage(CoreHand hand)
    {
        if (hand.gesture.strictType.ToString() == "One")
        {
            LayoutPage1 += 1;
            LayoutPage2 = 0;
        }
        else if (hand.gesture.strictType.ToString() == "Two")
        {
            LayoutPage2 += 1;
            LayoutPage1 = 0;
        }
        if (LayoutPage1 == 60)
        {
            return 1;
        }
        else if (LayoutPage2 == 60)
        {
            return 2;
        }
        return 0;
    }
}
