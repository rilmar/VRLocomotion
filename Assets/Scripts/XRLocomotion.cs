using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Blink
{
    // Settings defining the movement of a blink action

    public Transform origin; // Position where to cast a ray from
    public float delay = 0.5f; // Time until user can blink again
    public GameObject blinkMarker; // Visual placeholder for new location
    public float blinkHeight = 1.0f; // Farthest a blink can go vertically 0 for none
    public float playerHeight = 2.5f; // Minimum vertical room that player needs in order to navigate a space
    public float maxAngle = 10;

    public bool validateBlink(Vector3 oldPos, Vector3 newPos, Vector3 normal)
    {

        if(Vector3.Angle(Vector3.up, normal) > maxAngle)
        {
            return false;
        }

        if(blinkHeight > 0)
        {
            Vector3 dist = newPos - oldPos;
         
            if (dist.y > blinkHeight)
            {
                Debug.Log("out of vertical range");
                return false;
            }

        }

        return true;
    }
}

[System.Serializable]
public class Arc
{
    // Settings defining the arc drawn on blink

    public int maxVertices = 40;
    public float distance = 0.05f;
    public float velocity = 1;
    public float gravity = 1.5f;
    public Color validColor = Color.cyan;
    public Color invalidColor = Color.red;

    public void setValid(LineRenderer arc)
    {
        arc.startColor = arc.endColor = validColor;
    }

    public void setInvalid(LineRenderer arc)
    {
        arc.startColor = arc.endColor = invalidColor;
    }
}

public class XRLocomotion : MonoBehaviour
{

    [SerializeField] private string verticalJoystick, horizontalJoystick; //, movementVertical, movementHorizontal;
    [SerializeField] private Blink blink;
    [SerializeField] private Arc arc;
    [SerializeField] private LayerMask lm;

    private GameObject playerView;
    private GameObject marker;
    private CharacterController controller;

    // Variables to facilitate blink movement
    private RaycastHit hit;
    private LineRenderer arcLine;
    private Vector3 newPosition, normal;
    private bool inBlink;
    private float blinkDelayTracker = 0;
    private bool blinked = false;

    private Vector3[] positions;



    // Rotation
    [SerializeField] private float rotationAngle = 45f;
    [SerializeField] private float rotationDelay = 0.5f;

    private float rotationDelayTracker = 0;
    private bool rotated = false;

    // Movement
    private Vector3 moveDirection = Vector3.zero;
    private float gravitySpeed;
    private Quaternion currentRotation;

    void Start()
    {

        inBlink = false;

        // Set up linerenderer
        arcLine = gameObject.AddComponent<LineRenderer>();
        arcLine.material = new Material(Shader.Find("Particles/Additive"));
        arcLine.enabled = false;
        arcLine.startWidth = arcLine.endWidth = 0.04f;

        controller = GetComponent<CharacterController>();
        playerView = GameObject.FindGameObjectWithTag("MainCamera");

    }

    void Update()
    {
        // Track all time variables
        if (rotated)
        {
            rotationDelayTracker += Time.deltaTime;
            if (rotationDelayTracker > rotationDelay)
            {
                rotated = false;
            }
        }

        if (blinked)
        {
            blinkDelayTracker += Time.deltaTime;
            if (blinkDelayTracker > blink.delay)
            {
                blinked = false;
            }
        }

        if (inBlink)
        {
            blinkPlayer();
        }
        else
        {
            // not in blink, check for rotation, reverse, and blink
            //Joystick -1 is up
            if ((Input.GetAxis(verticalJoystick) < -0.9) && !inBlink && !blinked)
            {
                inBlink = true;
                arcLine.enabled = true;
                arc.setValid(arcLine);
            }
            // Check for rotation movement - implement strafe later
            if (Input.GetAxis(horizontalJoystick) < -0.95)
            {
                rotatePlayer((-1 * rotationAngle));
            }
            else if (Input.GetAxis(horizontalJoystick) > 0.95)
            {
                rotatePlayer(rotationAngle);
            }

            if (Input.GetAxis(verticalJoystick) > 0.95)
            {
                Debug.Log("vertical back");
                rotatePlayer(180);
            }
        }
    }

    private void blinkPlayer()
    {
        if (marker != null)
        {
            Destroy(marker);
        }

        Vector3 currentPosition = playerView.transform.position;
        currentPosition.y = transform.position.y;

        if (Input.GetAxis(verticalJoystick) == 0 && inBlink && Input.GetAxis(horizontalJoystick) == 0)
        {
            //check for release of blink
            inBlink = false;
            arcLine.enabled = false;
            arcLine.positionCount = 0;

            
            if (blink.validateBlink(currentPosition, newPosition, normal))
            {
                Vector3 offset = transform.position - currentPosition;


                transform.position = newPosition + offset;
                blinked = true;
            }
        }
        else
        {
            //draw curve and update marker position


            if (drawArc(blink.origin.position, blink.origin.forward, out newPosition, out normal))
            {
                //returns a value
                
                if (blink.validateBlink(currentPosition, newPosition, normal))
                {
                    //valid spot
                    marker = Instantiate(blink.blinkMarker, newPosition, getJoystickRotation());
                    arc.setValid(arcLine);
                } else
                {
                    arc.setInvalid(arcLine);
                }

            }
            else
            {
                arc.setInvalid(arcLine);
            }



        }

    }
    

    private bool drawArc(Vector3 origin, Vector3 direction, out Vector3 position, out Vector3 normal)
    {

        //for each point, raycast, then return if hit
        //use linecast for this

        position = transform.position;
        normal = new Vector3();

        List<Vector3> pos = new List<Vector3>();
        bool hasDestination = false;

        RaycastHit hit;
        Vector3 last = origin;

        Vector3 vel = direction.normalized * arc.velocity;
        Vector3 accel = new Vector3(0, -1, 0) * arc.gravity;

        float t = 0;


        for (int i = 0; i < arc.maxVertices && !hasDestination; ++i)
        {

            pos.Add(last);
            t += arc.distance / ParabolicCurveDeriv3d(vel, accel, t).magnitude;
            Vector3 next = ParabolicCurve3d(last, vel, accel, t);

            if (Physics.Linecast(last, next, out hit))
            {
                pos.Add(hit.point);
                position = hit.point;
                normal = hit.normal;
                hasDestination = true;
            }

            last = next;
        }

        Vector3[] positions = pos.ToArray();
        arcLine.positionCount = positions.Length;
        arcLine.SetPositions(positions);

        return hasDestination;
    }


    // Parabolic motion equation, y = p0 + v0*t + 1/2at^2
    private static float ParabolicCurve(float p0, float v0, float a, float t)
    {
        return p0 + v0 * t + 0.5f * a * t * t;
    }

    // Derivative of parabolic motion equation
    private static float ParabolicCurveDeriv(float v0, float a, float t)
    {
        return v0 + a * t;
    }

    // Parabolic motion equation applied to 3 dimensions
    private static Vector3 ParabolicCurve3d(Vector3 p0, Vector3 v0, Vector3 a, float t)
    {
        Vector3 ret = new Vector3();
        for (int x = 0; x < 3; x++)
            ret[x] = ParabolicCurve(p0[x], v0[x], a[x], t);
        return ret;
    }

    // Parabolic motion derivative applied to 3 dimensions
    private static Vector3 ParabolicCurveDeriv3d(Vector3 v0, Vector3 a, float t)
    {
        Vector3 ret = new Vector3();
        for (int x = 0; x < 3; x++)
            ret[x] = ParabolicCurveDeriv(v0[x], a[x], t);
        return ret;
    }

    private Quaternion getJoystickRotation()
    {
        return Quaternion.Euler(Vector3.forward);
    }

    private void rotatePlayer(float angle)
    {

        Debug.Log("rotate");
        if (!rotated)
        {

            Vector3 rotation = new Vector3(0, angle, 0);
            Vector3 pivot = playerView.transform.position;

            transform.RotateAround(pivot, Vector3.up, angle);
            currentRotation = transform.rotation;
            rotationDelayTracker = 0;
            rotated = true;
        }
    }

}
