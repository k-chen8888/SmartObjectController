﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/* An example sliding door
 */
[RequireComponent(typeof(Rigidbody))]
public class SmartDoor : SmartObject
{
    // RigidBody for the door
    private Rigidbody rb;

    // States
    private enum States { OPEN, AJAR, CLOSED, LOCKED };
    public int doorStartState = (int)States.LOCKED;

    // Identifying objects in the array
    private int KEY = 1;

    // Interaction ranges
    public float unlockDistance = 1.0f; // How far the key has to be to unlock the door
    public float openDistance = 2.0f; // How far the player has to be to open the door
    public bool automatic = false; // Does the door automatically open for the player?
    public float openSpeed = 1.0f;
    public Vector3 closedPosition;
    public Vector3 openPosition;
    private float percentTravelled = 1.0f;


    // Use this for initialization
    void Start()
    {
        // Reference to the door and its RigidBody
        self = gameObject;
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true; // Set to kinematic mode

        // Start on the starting state
        startState = doorStartState;
        currState = doorStartState;
        SetInitData();

        // Add new states and transitions
        AddNewStates();
        AddNewTransitions();

        // Set the door to its starting state
        IEnumerator next;
        StartCoroutine((states.TryGetValue(startState, out next) ? next : DoorLocked()));
    }

    // Update is called once per frame
    void Update()
    {

    }


    /* States
     */
    // The door is open
    private IEnumerator DoorOpen()
    {
        while (nextState == NOT_A_STATE)
        {
            if (automatic && Vector3.Distance(targets[PLAYER].transform.position, closedPosition) > openDistance)
            {
                // Automatic doors close when the player is too far away
                yield return StartCoroutine("ChangeStateTarget", (int)States.CLOSED);
                yield break;
            }
            else
            {
                yield return null;
            }
        }

        yield break;
    }

    // The door is ajar
    private IEnumerator DoorAjar()
    {
        float moveDistance = 0.0f;
        Vector3 startLocation = Vector3.zero,
                targetLocation = Vector3.zero;

        // Determine starting and ending points
        if (currState != (int)States.LOCKED)
        {
            if (nextState == (int)States.OPEN)
            {
                startLocation = closedPosition;
                targetLocation = openPosition;
            }
            else if (nextState != (int)States.AJAR)
            {
                startLocation = openPosition;
                targetLocation = closedPosition;
            }
        }

        // Compute move distance
        if (currState != (int)States.LOCKED)
        {
            moveDistance = Vector3.Distance(startLocation, targetLocation);
        }

        percentTravelled = 0.0f;
        while (percentTravelled < 1.0f && moveDistance > 0 && currState == (int)States.AJAR)
        {
            // Calculate easing between current and target locations
            percentTravelled += (Time.deltaTime * openSpeed) / moveDistance;
            percentTravelled = Mathf.Clamp01(percentTravelled);
            float easedPercent = Ease(percentTravelled);

            // Calculate new position based on easing
            Vector3 newPos = Vector3.Lerp(startLocation, targetLocation, easedPercent);

            // Move to the new position
            transform.position = newPos;

            if (percentTravelled >= 1.0f)
            {
                // What should happen next?
                if (nextState == (int)States.OPEN || nextState == (int)States.CLOSED)
                {
                    yield return StartCoroutine("ChangeState", nextState);
                    yield break;
                }
                else if (nextState == (int)States.LOCKED)
                {
                    yield return StartCoroutine("ChangeStateTarget", nextState);
                    yield break;
                }
            }

            // Go to the next iteration
            yield return null;
        }

        yield break;
    }

    // The door is closed
    private IEnumerator DoorClosed()
    {
        // The door is meant to be locked immediately afterwards
        // This can only happen when the co-routine is started up; otherwise, the lock method would be called
        if (nextState == (int)States.LOCKED)
        {
            nextState = (int)States.LOCKED;
            yield return StartCoroutine(ChangeState());
            yield break;
        }
        else
        {
            while (nextState == NOT_A_STATE)
            {
                if (automatic && Vector3.Distance(targets[PLAYER].transform.position, closedPosition) <= openDistance)
                {
                    // Automatic doors open when the player is close enough
                    nextState = (int)States.OPEN;
                    yield return StartCoroutine(ChangeState());
                    yield break;
                }
                else
                {
                    yield return null;
                }
            }
        }

        yield break;
    }
    

    // The door is locked
    // The only way to change this state is externally
    private IEnumerator DoorLocked()
    {
        while (nextState == NOT_A_STATE)
        {
            yield return null;
        }

        yield break;
    }

    // Gets out of bad states (default to going to INIT_STATE)
    protected override IEnumerator LeaveBadState()
    {
        StopAllCoroutines();
        currState = nextState;
        ResetObject();

        IEnumerator next;
        yield return StartCoroutine((states.TryGetValue(nextState, out next) ? next : DoorLocked()));

        nextState = NOT_A_STATE;
        yield break;
    }


    /* Transition co-routine
     */
    protected override IEnumerator ChangeState()
    {
        // Check for a valid transition
        if (ExistsTransition(nextState))
        {
            IEnumerator coroutine;

            // Start the next state
            if (states.TryGetValue(nextState, out coroutine))
            {
                // Complete change to state
                currState = nextState;
                yield return StartCoroutine(coroutine);
            }
        }

        // Whoops! Try to recover...
        nextState = startState;
        yield return StartCoroutine(LeaveBadState());
    }

    // Helper transition function that handles closing a door when it is open
    private IEnumerator OpenToClose()
    {
        IEnumerator coroutine = null;

        if (currState == (int)States.OPEN)
        {
            nextState = (int)States.AJAR;
            if (states.TryGetValue(nextState, out coroutine))
            {
                // Complete change to AJAR state
                currState = nextState;
                nextState = (int)States.CLOSED;
                yield return StartCoroutine(coroutine);

                // Do nothing until AJAR finishes
                while (percentTravelled < 1.0f) { }


            }
        }

        // Whoops! Try to recover...
        nextState = startState;
        yield return StartCoroutine(LeaveBadState());
        yield break;
    }

    // Helper for transition coroutine
    // Check if there's a sequence of actions that need to be performed
    protected IEnumerator ChangeStateTarget(int targetState)
    {
        IEnumerator coroutine;

        // OPEN -> AJAR -> CLOSED or CLOSED -> AJAR -> OPEN or OPEN -> AJAR -> CLOSED -> LOCKED
        // In AJAR, handle the following:
        //  The door switches to OPEN
        //  The door switches to CLOSED
        //  The door switches to LOCKED
        if ((currState == (int)States.OPEN && nextState == (int)States.CLOSED) ||
            (currState == (int)States.CLOSED && nextState == (int)States.OPEN) ||
            (currState == (int)States.OPEN && nextState == (int)States.LOCKED))
        {
            if (ExistsTransition((int)States.AJAR))
            {
                // Start the next state
                if (states.TryGetValue((int)States.AJAR, out coroutine))
                {
                    // Complete change to state
                    currState = (int)States.AJAR;
                    yield return StartCoroutine(coroutine);
                }
            }
        }
        else if (currState == (int)States.AJAR && nextState == (int)States.LOCKED)
        {
            if (ExistsTransition((int)States.CLOSED))
            {
                // Start the next state
                if (states.TryGetValue((int)States.CLOSED, out coroutine))
                {
                    // Complete change to state
                    currState = (int)States.CLOSED;
                    yield return StartCoroutine(coroutine);
                }
            }
        }

        // Whoops! Try to recover...
        nextState = startState;
        yield return StartCoroutine(LeaveBadState());
        yield break;
    }


    /* Utilities
     */
    // Override to add new states to the FSM
    protected override void AddNewStates()
    {
        // Remove the old default start state
        states.Remove(INIT_STATE);

        // Replace the old default LeaveBadState()
        states.Remove(BAD_STATE);
        states.Add(BAD_STATE, LeaveBadState());

        // Add the new states
        states.Add((int)States.OPEN, DoorOpen());
        states.Add((int)States.AJAR, DoorAjar());
        states.Add((int)States.CLOSED, DoorClosed());
        states.Add((int)States.LOCKED, DoorLocked());
    }

    // Override to add new transitions to the FSM
    protected override void AddNewTransitions()
    {
        /* All possible transitions:
         *  A closed door must be AJAR before it can be open
         *      CLOSED -> AJAR -> OPEN
         *      (CLOSED, AJAR)
         *      (AJAR, OPENED)
         *
         *  An open door must be AJAR before it can be closed
         *      OPEN -> AJAR -> CLOSED
         *      (OPEN, AJAR)
         *      (AJAR, CLOSED)
         *
         *  A locked door can be unlocked, but it won't open yet
         *      (LOCKED, CLOSED)
         *  
         *  A closed door can be re-locked
         *      (CLOSED, LOCKED)
         *  
         *  An open door must be closed first before it can be re-locked
         *      OPEN -> AJAR -> CLOSED -> LOCKED
         */

        transitions.Add((int)States.OPEN, new List<int> (new int[] {
            (int)States.AJAR
        }));

        transitions.Add((int)States.AJAR, new List<int>(new int[] {
            (int)States.OPEN,
            (int)States.CLOSED
        }));

        transitions.Add((int)States.CLOSED, new List<int>(new int[] {
            (int)States.AJAR,
            (int)States.LOCKED
        }));

        transitions.Add((int)States.LOCKED, new List<int>(new int[] {
            (int)States.CLOSED
        }));
    }

    // Toggle the door's state between OPEN and CLOSED
    public void ToggleDoor()
    {
        // Non-automatic doors can only open/close when interacted with
        if (!automatic)
        {
            nextState = (int)States.AJAR;
            StartCoroutine(ChangeState());
        }
    }

    // Lock or unlock door
    public void UseKey()
    {
        // Make sure the key has been brought in range of the door
        if (Vector3.Distance(targets[KEY].transform.position, transform.position) <= unlockDistance)
        {
            if (currState == (int)States.LOCKED)
            {
                nextState = (int)States.CLOSED;
                StartCoroutine(ChangeState());
            }
            else if (currState == (int)States.OPEN)
            {
                nextState = (int)States.LOCKED;
                StartCoroutine(ChangeStateTarget((int)States.LOCKED));
            }
            else if (currState == (int)States.CLOSED)
            {
                nextState = (int)States.LOCKED;
                StartCoroutine(ChangeState());
            }
        }
    }

    /*
    // Check if the player is in front of the door
    public bool IsInFront()
    {
        Ray ray = new Ray(targets[PLAYER].transform.position, targets[PLAYER].transform.forward);
        RaycastHit hit;

        return Physics.Raycast(ray, out hit);
    }

    // Check if the player is behind the door
    public bool IsBehind()
    {
        Ray ray = new Ray(targets[PLAYER].transform.position, -targets[PLAYER].transform.forward);
        RaycastHit hit;

        return Physics.Raycast(ray, out hit);
    }
    */
}
