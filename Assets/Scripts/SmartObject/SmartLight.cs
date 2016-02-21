using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* An example SmartObject light that turns off when the player moves too far away from it
 *  At an intermediate distance, the light flickers
 *
 * The player can also force the light into other states
 */
public class SmartLight : SmartObject {

    // States
    private enum States { ON, OFF, FLICKER, TURN_ON, TURN_OFF, DISABLE };
    public int lightStartState = (int)States.ON;

    // Interaction distances
    public float onDistance = 10.0f;
    public float offDistance = 25.0f;
    public float disableTimer = 1.0f;

    // Maximum wait time before flickering the light
    public float maxFlickerWait = 1.0f;

    // Light object
    public Light mainLight;
    

	// Use this for initialization
	void Start () {
        // Grab the light
        mainLight = GetComponent<Light>();

        // Start on the starting state
        startState = lightStartState;
        currState = startState;
        SetInitData();

        // Add new states and transitions
        AddNewStates();
        AddNewTransitions();

        // Set the light to its starting state
        string next = null;
        states.TryGetValue(startState, out next);
        StartCoroutine((next != null ? next : "LightOn"));
    }
	
	// Update is called once per frame
	void Update () {
        
	}


    /* States
     */
    // Describes the state "The light is on"
    private IEnumerator LightOn()
    {
        // Turns the light on when started
        mainLight.enabled = true;

        while (!changingState)
        {
            float distToPlayer = Vector3.Distance(mainLight.transform.position, targets[PLAYER].transform.position);
            
            if (distToPlayer >= offDistance)
            {
                // Should I turn off?
                changingState = true;
                yield return StartCoroutine(ChangeState((int)States.OFF));
            }
            else if (distToPlayer < offDistance && distToPlayer > onDistance)
            {
                // Eerie flickering effect
                changingState = true;
                yield return StartCoroutine(ChangeState((int)States.FLICKER));
            }
            else
            {
                // Just keep on keeping on...
                yield return null;
            }
        }

        yield return null;
    }

    // Describes the state "The light is off"
    private IEnumerator LightOff()
    {
        // Turns the light off when started
        mainLight.enabled = false;

        while (!changingState)
        {
            float distToPlayer = Vector3.Distance(mainLight.transform.position, targets[PLAYER].transform.position);

            if (distToPlayer < offDistance && distToPlayer > onDistance)
            {
                // Eerie flickering effect
                changingState = true;
                yield return StartCoroutine(ChangeState((int)States.FLICKER));
            }
            else if (distToPlayer <= onDistance)
            {
                // Should I turn on?
                changingState = true;
                yield return StartCoroutine(ChangeState((int)States.ON));
            }
            else
            {
                // Just keep on keeping on...
                yield return null;
            }
        }

        yield return null;
    }

    // Describes the state "The light is flickering"
    private IEnumerator LightFlicker()
    {
        float nextChange = Time.time;

        while (!changingState)
        {
            float distToPlayer = Vector3.Distance(mainLight.transform.position, targets[PLAYER].transform.position);

            if (distToPlayer >= offDistance)
            {
                // Should I turn off?
                changingState = true;
                yield return StartCoroutine(ChangeState((int)States.OFF));
            }
            else if (distToPlayer <= onDistance)
            {
                // Should I turn on?
                changingState = true;
                yield return StartCoroutine(ChangeState((int)States.ON));
            }
            else
            {
                // Just keep on keeping on...
                if (Time.time > nextChange)
                {
                    mainLight.enabled = !mainLight.enabled;
                    nextChange += Random.Range(0, maxFlickerWait);
                }
                yield return null;
            }
        }

        yield return null;
    }

    // Describes the state "The light has been forcibly turned on"
    private IEnumerator LightForceOn()
    {
        while (!changingState)
        {
            mainLight.enabled = true;
            yield return null;
        }

        yield return null;
    }

    // Describes the state "The light has been forcibly turned off"
    private IEnumerator LightForceOff()
    {
        while (!changingState)
        {
            mainLight.enabled = false;
            yield return null;
        }

        yield return null;
    }

    // Describes the state "The light has been forcibly disabled"
    private IEnumerator LightDisable()
    {
        while (!changingState)
        {
            print("Disabled... I cannot recover from this state");
            yield return new WaitForSeconds(disableTimer);
        }

        yield return null;
    }
    public void DisableLight()
    {
        // Call this from the outside to disable the light for good
        if (currState != (int)States.DISABLE)
        {
            changingState = true;
            StartCoroutine(ChangeState((int)States.DISABLE));
        }
    }

    // Because a custom starting configuration was defined, need to override LeaveBadState()
    protected override IEnumerator LeaveBadState(int nextState = 0)
    {
        StopAllCoroutines();
        changingState = false;
        currState = nextState;
        ResetObject();

        string next = null;
        states.TryGetValue(nextState, out next);
        yield return StartCoroutine((next != null ? next : "LightOn"));
    }


    /* Transition co-routine
     */
    protected override IEnumerator ChangeState(int nextState = 0)
    {
        // Check for a valid transition
        if (ExistsTransition(nextState))
        {
            string coroutine = null;

            // Stop the current state
            states.TryGetValue(currState, out coroutine);
            if (coroutine != null)
                StopCoroutine(coroutine);

            // Start the next state
            changingState = false;
            states.TryGetValue(nextState, out coroutine);
            if (coroutine != null)
            {
                // Complete change to state
                currState = nextState;
                yield return StartCoroutine(coroutine);
            }
        }

        // Whoops! Try to recover...
        yield return StartCoroutine(LeaveBadState(currState));
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
        states.Add(BAD_STATE, "LeaveBadState");

        // Add the new states
        states.Add((int)States.ON, "LightOn");
        states.Add((int)States.OFF, "LightOff");
        states.Add((int)States.FLICKER, "LightFlicker");
        states.Add((int)States.TURN_ON, "LightForceOn");
        states.Add((int)States.TURN_OFF, "LightForceOff");
        states.Add((int)States.DISABLE, "LightDisable");
    }

    // Override to add new transitions to the FSM
    protected override void AddNewTransitions()
    {
        /* All possible transitions:
         *  Automatically turn ON/OFF
         *      (ON, OFF)
         *      (OFF, ON)
         * 
         *  If the player does not mess with the light, it can flicker
         *      (ON, FLICKER)
         *      (OFF, FLICKER)
         *      (FLICKER, ON)
         *      (FLICKER, OFF)
         * 
         *  If the player messes with the light, it doesn't do things automatically anymore
         *      (ON, TURN_OFF)
         *      (ON, TURN_ON)
         *      (FLICKER, TURN_ON)
         *      (FLICKER, TURN_OFF)
         * 
         *  The player continues messing with the light
         *      (TURN_ON, TURN_OFF)
         *      (TURN_OFF, TURN_ON)
         * 
         *  The light can be disabled at any other state
         *      (ON, DISABLE)
         *      (OFF, DISABLE)
         *      (FLICKER, DISABLE)
         *      (TURN_ON, DISABLE)
         *      (TURN_OFF, DISABLE)
         */

        transitions.Add((int)States.ON, new List<int> (new int[] {
            (int)States.OFF,
            (int)States.FLICKER,
            (int)States.TURN_OFF,
            (int)States.DISABLE
        }));

        transitions.Add((int)States.OFF, new List<int>(new int[] {
            (int)States.ON,
            (int)States.FLICKER,
            (int)States.TURN_ON,
            (int)States.DISABLE
        }));

        transitions.Add((int)States.FLICKER, new List<int>(new int[] {
            (int)States.ON,
            (int)States.OFF,
            (int)States.TURN_ON,
            (int)States.TURN_OFF,
            (int)States.DISABLE
        }));

        transitions.Add((int)States.TURN_ON, new List<int>(new int[] {
            (int)States.TURN_OFF,
            (int)States.DISABLE
        }));

        transitions.Add((int)States.TURN_OFF, new List<int>(new int[] {
            (int)States.TURN_ON,
            (int)States.DISABLE
        }));
    }

    // Override the reset function to also turn the light back on
    protected override void SetInitData()
    {
        base.SetInitData();
        mainLight.enabled = true;
    }

    // Allows the player to externally toggle the light between an on/off state
    public void ToggleLight()
    {
        if (currState != (int)States.DISABLE)
        {
            if (currState != (int)States.TURN_ON && currState != (int)States.ON)
            {
                changingState = true;
                StartCoroutine(ChangeState((int)States.TURN_ON));
            }
            else if (currState != (int)States.TURN_OFF && currState != (int)States.OFF)
            {
                changingState = true;
                StartCoroutine(ChangeState((int)States.TURN_OFF));
            }
        }
    }
}
