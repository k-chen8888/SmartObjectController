using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour {

    private GameObject self;
    private Rigidbody rb;

    public float speed = 1.0f;

	// Use this for initialization
	void Start () {
        self = gameObject;
        rb = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
        // Move the object
        // Relative forces play better in the low-friction environment
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(moveHorizontal, 0, moveVertical);

        rb.AddRelativeForce(move * speed * Time.deltaTime);
    }
}
