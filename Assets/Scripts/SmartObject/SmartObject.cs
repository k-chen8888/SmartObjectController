using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public abstract class SmartObject : MonoBehaviour
{
    // Information about this behavior
    public string behaviorName = "SmartObject";

    // Variables that control how the object interacts with its surroundings
    public float interactDist = 10.0f; // Interact with the target if it comes within this distance
    public float transitionWait = 0.0f; // Wait time between state transitions

    // Objects that this SmartObject can interact with
    protected static int PLAYER = 0;
    public GameObject[] targets;

    // States and transitions
    protected static int NOT_A_STATE = -2; // Constant value that means "not an actual state in the machine"
    protected static int BAD_STATE = -1; // A transition that wasn't supposed to happen leads to a bad state
    protected static int INIT_STATE = 0; // All objects, by default, start out on this state
    public int startState;
    protected int currState;
    protected IEnumerator prevRoutine, currRoutine;
    protected int nextState = NOT_A_STATE;
    protected Dictionary<int, IEnumerator> states = new Dictionary<int, IEnumerator>();
    protected Dictionary<int, List<int>> transitions = new Dictionary<int, List<int>>
    {
        { BAD_STATE, new List<int> (new int[] { INIT_STATE }) }
    };
    protected Dictionary<int, string> animations;

    // Information about the SmartObject itself
    protected GameObject self;
    protected InitData id;

    // Easing function
    [Range(0, 2)]
    public float easeFactor = 1;


    // Always runs on startup
    void Awake()
    {
        states.Add(BAD_STATE, LeaveBadState());
        states.Add(INIT_STATE, InitialState());
    }


    /* Struct that defines basic information about the object's initial state
     */
    protected struct InitData
    {
        Vector3 startPos;
        Quaternion startOrientation;
        int startState;

        // Constructor
        public InitData(Vector3 position, Quaternion rotation, int startState) : this()
        {
            this.startPos = position;
            this.startOrientation = rotation;
            this.startState = startState;
        }

        // Get fields
        public Vector3 GetStartPos()
        {
            return this.startPos;
        }
        public Quaternion GetStartOrientation()
        {
            return this.startOrientation;
        }
        public int GetStartState()
        {
            return this.startState;
        }
    }

    
    /* Default states
     * 
     * These states are included by default
     */
    // A generic state that doesn't really do anything...
    // Override this with an actual initial state
    protected virtual IEnumerator InitialState()
    {
        while (true)
        {
            print("Ready to do something...");
            yield return null;
        }
    }

    // Gets out of bad states (default to going to INIT_STATE)
    protected virtual IEnumerator LeaveBadState()
    {
        StopAllCoroutines();
        currState = nextState;
        ResetObject();

        IEnumerator next = null;
        if (states.TryGetValue(nextState, out next))
            yield return StartCoroutine(next);
        else
            yield return StartCoroutine(InitialState());

        nextState = NOT_A_STATE;
        yield break;
    }


    /* Transition co-routine (default to going to INIT_STATE)
     * At the very least, override this to change which error-handling co-routine gets called
     */
    // Single action
    protected virtual IEnumerator ChangeState()
    {
        IEnumerator next = null;
        if (states.TryGetValue(nextState, out next))
            yield return StartCoroutine(next);
        else
            yield return StartCoroutine(InitialState());

        nextState = NOT_A_STATE;
        yield break;
    }


    /* Common operations
     */
    // On startup, call this to get basic information on the object's initial state
    protected virtual void SetInitData()
    {
        id = new InitData(transform.position, transform.rotation, startState);
    }

    // Find out if a transition is possible
    protected virtual bool ExistsTransition(int nextState)
    {
        // Grab all possible values
        List<int> possible = null;
        transitions.TryGetValue(currState, out possible);

        // Nowhere to go
        if (possible == null)
            return false;

        // Try to find nextState in list
        foreach (int state in possible)
        {
            // Exit on find
            if (state == nextState)
                return true;
        }

        // Not in dictionary
        return false;
    }
    
    // Name of this behavior
    public string GetName()
    {
        return behaviorName;
    }

    // Resets to defaults, if any are specified
    // Happens when leaving bad state
    public virtual void ResetObject()
    {
        transform.position = id.GetStartPos();
        transform.rotation = id.GetStartOrientation();
        currState = id.GetStartState();
    }

    // Add new states to the machine
    // Must be overridden to have any effect
    protected virtual void AddNewStates()
    {

    }

    // Add new transitions to the machine
    // Must be overridden to have any effect
    protected virtual void AddNewTransitions()
    {

    }

    // Safely starts a coroutine
    // Can be done using (1) an IEnumerator or (2) an index in the dictionary and a new IEnumerator instance to replace it
    // Note: IEnumerator coroutines can only be started once, which means that the IEnumerator needs to be replaced
    protected void SafeStartCoroutine(IEnumerator next)
    {
        prevRoutine = currRoutine;
        currRoutine = next;
        StartCoroutine(currRoutine);

        if (prevRoutine != null)
        {
            StopCoroutine(prevRoutine);
            prevRoutine = null;
        }
    }
    protected bool SafeStartCoroutine(int key)
    {
        IEnumerator next;
        
        if ((states.TryGetValue(key, out next)))
        {
            prevRoutine = currRoutine;
            currRoutine = next;
            StartCoroutine(currRoutine);

            if (prevRoutine != null)
            {
                StopCoroutine(prevRoutine);
                prevRoutine = null;
            }

            return true;
        }

        return false;
    }


    // Movement Easing equation: y = x^a / (x^a + (1-x)^a)
    //
    // Takes x values between 0 and 1 and maps them to y values also between 0 and 1
    //  a = 1 -> straight line
    //  This is a logistic function; as a increases, y increases faster for values of x near .5 and slower for values of x near 0 or 1
    //
    // For animation, 1 < a < 3 is pretty good
    protected float Ease(float x)
    {
        float a = easeFactor + 1.0f;
        return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
    }

    // Compare behaviors
    public override bool Equals(object obj)
    {
        // Check references
        if (ReferenceEquals(this, obj))
            return true;

        // Check null
        if ((object)this == null ^ obj == null)
            return false;
        if ((object)this == null && obj == null)
            return true;

        // Do the names match?
        if (this.GetName() == ((SmartObject)obj).GetName())
            return true;

        return base.Equals(obj);
    }

    public static bool operator ==(SmartObject a, SmartObject b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(SmartObject a, SmartObject b)
    {
        return !a.Equals(b);
    }

    // Hash code
    public override int GetHashCode()
    {
        return behaviorName.GetHashCode() ^ self.GetHashCode();
    }
}
