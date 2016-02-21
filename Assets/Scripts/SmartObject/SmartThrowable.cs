using UnityEngine;
using System.Collections;


[RequireComponent(typeof(Rigidbody))]
public class SmartThrowable : SmartObject {

    // Rigidbody attached to object
    private Rigidbody rb;

    // States
    private enum States { REST, PICKUP, THROW, FLYING };
    public new int startState = (int)States.REST;

    // Interaction ranges
    public float pickUpRange = 2.0f;
    public int pickUpLayer = 8;
    public float throwRange = 10.0f;
    public int throwLayer = 9;
    public int restingSurfaceLayer = 10;
    public float throwForce = 10.0f;

    // Follow the player when picked up
    private Vector3 offset;


    // Use this for initialization
    void Start () {
        // Get the object's information
        self = gameObject;
        rb = GetComponent<Rigidbody>();
        currState = startState;

        // Start interacting with the world
        StartCoroutine(Interact());
	}
	
	// Update is called once per frame
	void Update () {
	    
	}

    // LateUpdate is called once per frame after Update()
    void LateUpdate()
    {
        if (currState == (int)States.PICKUP)
        {
            // Follow the player if picked up
            transform.position = targets[0].transform.position + offset;
        }
    }


    /* Define how this object interacts with its surroundings
     */
    protected IEnumerator Interact()
    {
        Ray ray;
        RaycastHit hit;

        while (true)
        {
            switch (currState)
            {
                case (int)States.REST:
                    // Check if the object can be picked up
                    // Allow interaction if possible
                    ray = new Ray(targets[PLAYER].transform.position, targets[PLAYER].transform.forward);

                    if (Physics.Raycast(ray, out hit, pickUpRange, layerMask: 1 << pickUpLayer))
                    {
                        AnimatePickUp();
                    }

                    break;

                case (int)States.PICKUP:
                    // Check if the object can be thrown at something (in range and has a SmartTarget script)
                    // Allow interaction if possible
                    ray = new Ray(targets[PLAYER].transform.position, targets[PLAYER].transform.forward);

                    if (Physics.Raycast(ray, out hit, throwRange, layerMask: 1 << throwLayer))
                    {
                        SmartTarget st = hit.transform.gameObject.GetComponent<SmartTarget>();

                        if (st != null && st.CanHit(self))
                        {
                            // Indicate that the object can be hit

                            // Throw the object
                            AnimateThrow(hit);
                        }
                    }

                    break;
            }

            yield return null;
        }
    }


    /* Animations
     */
    private void AnimatePickUp()
    {
        // Move this object to the PLAYER's hand
        SmartPlayer sp = targets[0].GetComponent<SmartPlayer>();
        transform.position = targets[0].transform.position + (sp != null ? sp.GetHandPosition(true) : new Vector3(0.6f, 0, 0));

        // Set the state
        currState = (int)States.PICKUP;
        offset = transform.position - targets[0].transform.position;
    }

    private void AnimateThrow(RaycastHit throwTarget)
    {
        // Add a force to send the object towards the target
        SmartPlayer sp = targets[0].GetComponent<SmartPlayer>();
        if (sp != null)
            sp.ThrowAnimation(rb, throwTarget);
        else
            rb.AddForce((transform.forward + transform.up * .01f) * throwForce);

        // Set the state
        currState = (int)States.FLYING;
        offset = Vector3.zero;
    }


    /* Collision detection
     */
    void OnCollisionEnter(Collision collision)
    {
        // Reset state to rest when hitting a surface after flying
        if (((1 << collision.gameObject.layer) & (1 << restingSurfaceLayer)) > 0 && currState == (int)States.FLYING)
        {
            currState = (int)States.REST;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Change state directly to flying if it leaves rest
        if (((1 << collision.gameObject.layer) & (1 << restingSurfaceLayer)) > 0 && currState == (int)States.REST)
        {
            currState = (int)States.FLYING;
        }
    }
}
