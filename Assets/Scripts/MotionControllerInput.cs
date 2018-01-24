using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class MotionControllerInput : MonoBehaviour {
    public string triggerName;

    //public Animator anim;

    //from old controller
    public float grabRadius;
    public LayerMask grabMask;

    private GameObject grabbedObject;
    private bool grabbing;

    private Quaternion lastRotation, currentRotation;

    private XRTracker xrTracker;
    private XRNode node;
    private List<XRNodeState> nodeStates = new List<XRNodeState>();
    private XRNodeState nodeState;

    // Use this for initialization
    void Start () {
        xrTracker = GetComponent<XRTracker>();

        UnityEngine.XR.InputTracking.GetNodeStates(nodeStates);
        //find nodestate for current node and store for later use
        foreach (XRNodeState n in nodeStates)
        {
            if (n.nodeType == node)
            {
                nodeState = n;
            }
        }

        //Debug.Log(nodeState.nodeType);

    }
	
	// Update is called once per frame
	void Update () {
        /*
        if (Input.GetAxis(trigger) > 0)
        {
            Debug.Log("trigger press: " + trigger);
        }
        */

        if (!grabbing && Input.GetAxis(triggerName) == 1)
        {
            Debug.Log("Grabbing with: " + triggerName);
            //grab();
            grabbing = true;
            //anim.SetBool("gripping", true);
        }
        if (grabbing && Input.GetAxis(triggerName) < 1)
        {
            Debug.Log("Dropping with: " + triggerName);
            //drop();
            grabbing = false;
            //anim.SetBool("gripping", false);
        }


    }

    void grab()
    {
        grabbing = true;

        RaycastHit[] objs;

        objs = Physics.SphereCastAll(transform.position, grabRadius, transform.forward, 0f, grabMask);

        int objIndex = 0;

        if (objs.Length > 0)
        {
            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i].distance < objs[objIndex].distance)
                    objIndex = i;
            }

            grabbedObject = objs[objIndex].transform.gameObject;
            grabbedObject.GetComponent<Rigidbody>().isKinematic = true;
            grabbedObject.transform.position = transform.position;
            grabbedObject.transform.parent = transform;


        }

    }

    void drop()
    {
        grabbing = false;

        if (grabbedObject != null)
        {
            grabbedObject.GetComponent<Rigidbody>().isKinematic = false;
            grabbedObject.transform.parent = null;

            Vector3 accel = new Vector3(0, 0, 0);
            Vector3 angAccel = new Vector3(0, 0, 0);
            nodeState.TryGetVelocity(out accel);
            Debug.Log("accel" + accel.ToString());
            Debug.Log("angAccel" + angAccel.ToString());
            nodeState.TryGetAngularVelocity(out angAccel);
            grabbedObject.GetComponent<Rigidbody>().velocity = accel;
            grabbedObject.GetComponent<Rigidbody>().angularVelocity = angAccel;

            grabbedObject = null;
        }
    }

}
