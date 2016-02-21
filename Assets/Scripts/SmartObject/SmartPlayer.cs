using UnityEngine;
using System.Collections;

public class SmartPlayer : SmartObject {

    // States
    private enum States { REST, MOVING, PERFORM_ACTION, HOLD, GRAB, JUMP };
    public new int startState = (int)States.REST;

    // Positioning of player's body parts relative to player
    public Vector3 lHandPos;
    public Vector3 rHandPos;
    public float strength = 10.0f;


	// Use this for initialization
	void Start ()
    {
        // Reference to the player object
        self = gameObject;

        // Interact with the world
        StartCoroutine(Interact());
	}
	
	// Update is called once per frame
	void Update ()
    {
	    
	}


    /* Define how this object interacts with its surroundings
     */
    protected IEnumerator Interact()
    {
        while (true)
        {
            yield return null;
        }
    }


    /* Animations
     */
    // Throw at a target
    public void ThrowAnimation(Rigidbody throwableRB, RaycastHit throwTarget)
    {
        // Play an animation

        // After the animation ends, release the object
        throwableRB.AddForce((transform.forward + transform.up * .01f) * strength);
    }


    /* Utilities
     */
    // Get hand position (player position + offset)
    public Vector3 GetHandPosition(bool right)
    {
        return self.transform.position + (right ? rHandPos : lHandPos);
    }
}
