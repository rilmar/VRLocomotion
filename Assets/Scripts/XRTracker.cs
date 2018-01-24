using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class XRTracker : MonoBehaviour {

    public XRNode node;
    public float xAngleAdjust = 30; //30 to move WMR controllers to flat instead of angled

    private Quaternion rotation;



    private void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        transform.localPosition = InputTracking.GetLocalPosition(node);
        rotation = InputTracking.GetLocalRotation(node);
        rotation *= Quaternion.Euler(Vector3.left * (-1) * xAngleAdjust);
        transform.localRotation = rotation;

    }

    public void setAdjustAngle(float a)
    {
        xAngleAdjust = a;
    }

    public XRNode getXRNode()
    {
        return node;
    }
}
