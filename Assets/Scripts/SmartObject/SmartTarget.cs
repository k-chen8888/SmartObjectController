using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SmartTarget : SmartObject {
    // Hit information
    private Collider col;
    private int hitCount = 0;
    public float targetRange = 10.0f;
    private Dictionary<int, GameObject> collisionStay = new Dictionary<int, GameObject>();
    public int[] hitLayers = new int[1];


	// Use this for initialization
	void Start () {
        
	}
	
	// Update is called once per frame
	void Update () {

	}

    void OnCollisionEnter(Collision collision)
    {
        int key = collision.gameObject.GetHashCode();

        // Only register the first hit
        // Save a reference to the object until it leaves
        if (!collisionStay.ContainsKey(key) && CanHit(collision.gameObject))
        {
            collisionStay.Add(key, collision.gameObject);
            hitCount += 1;
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Take the object out of the dictionary once it leaves so that later hits can be detected
        int key = collision.gameObject.GetHashCode();
        collisionStay.Remove(key);
    }


    /* Utilities
     */
    // Query for whether or not an object is on the list of interactable objects
    public bool CanHit(GameObject projectile)
    {
        foreach (int l in hitLayers)
        {
            if (((1 << projectile.layer) & (1 << l)) > 0)
            {
                return true;
            }
        }

        return false;
    }
}
