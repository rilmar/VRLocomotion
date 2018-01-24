using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class XRManager : MonoBehaviour {

    private GameObject playerView;

    bool oculusPresent = false;

    // Use this for initialization
    void Start () {
        Debug.Log("XR Manager Startup");
        playerView = GameObject.FindGameObjectWithTag("MainCamera");
        if (CheckOculusPresence()) {

            Debug.Log("Oculus is present");
            //check if oculus is present, add offset to rig
            
        }
        
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    private bool CheckOculusPresence()
    {
        return OVRManager.isHmdPresent;
    }

}
