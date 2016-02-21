using UnityEngine;
using System.Collections;


/* Moves with the player
 *  Either repels (moves away) or attracts (moves towards)
 */
[RequireComponent(typeof(Rigidbody))]
public class SmartMove : SmartObject {
    
    // Rigidbody for this object
    private Rigidbody rb;

    // States
    public bool attract = false; // Move away or towards?

    // Offset from the thing that it's following
    public float followDist = 25.0f;
    public float gapDist = 15.0f;
    public float scaling = 1.0f;
    

	// Use this for initialization
	void Start () {
        self = gameObject;
        rb = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    // LateUpdate is called after Update()
    void LateUpdate ()
    {
        
        float dist = Vector3.Distance(transform.position, targets[PLAYER].transform.position);
        if (dist <= followDist && dist > gapDist)
        {
            Vector3 force = Vector3.Normalize(transform.position - targets[PLAYER].transform.position) * scaling;
            rb.AddForce((attract ? -force : -force));
        }
        else if (dist <= gapDist)
        {
            Vector3 force = Vector3.Normalize(transform.position - targets[PLAYER].transform.position) * 2 * scaling;
            rb.AddForce((attract ? -force : force));
        }
    }
}
