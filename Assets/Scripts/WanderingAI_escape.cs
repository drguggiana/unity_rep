using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class WanderingAI_escape : MonoBehaviour
{
    // --- Public Variables -- //

    // Variables for the animator, set by user in GUI
    public Animator anim;
    public Transform rootNode;
    public float wanderRadius;
    public float wanderTimer;

    // Get hash values for the animation parameters for faster execution
    private readonly int speedHash = Animator.StringToHash("speed");
    private readonly int angleHash = Animator.StringToHash("angle");
    private readonly int stateHash = Animator.StringToHash("state_selector");
    private readonly int motionHash = Animator.StringToHash("motion_selector");
    private readonly int escapeHash = Animator.StringToHash("escape_selector");
    private readonly int jumpWalkHash = Animator.StringToHash("jump_walk_transition");
    private readonly int jumpStartHash = Animator.StringToHash("start_jump");
    private readonly int jumpFinishHash = Animator.StringToHash("finish_jump");
    private readonly int encounterHash = Animator.StringToHash("encounter");

    // Initialize variables for transforms
    private UnityEngine.AI.NavMeshAgent agent;
    private Transform player;
    private Transform startTransform;
    private Quaternion lookRotation;

    // Variables for motion
    private float multiplyBy = 0.4f;
    private float timer;
    private float new_speed = 0;
    private float delta_angle = 0;
    private float old_angle = 0;

    private Vector3 local_up = new Vector3(0.0f, 1.0f, 0.0f);

    private bool start_jump;
    private bool finish_jump;

    Vector2 smoothDeltaPosition = Vector2.zero;


    // Use this for initialization
    void OnEnable()
    {
        // initialize the timer for wandering
        timer = wanderTimer;

        // get the object of the player to avoid
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // get the navigation agent of the cricket
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        // prevent the agent to change the position and rotation of the cricket
        agent.updatePosition = false;
        agent.updateRotation = false;

        // get initial rotation
        lookRotation = transform.rotation;

    }

    // Update is called once per frame
    void Update()
    {
        // update the timer
        timer += Time.deltaTime;

        // if over the wander timer threshold
        if (timer >= wanderTimer)
        {
            // get a random position based on the wander radius and then navigation mesh
            Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
            // set the destination for the agent
            agent.SetDestination(newPos);
            // change the speed to normal (since after avoidance speed it up)
            agent.speed = 0.1f;
            // reset the timer
            timer = 0;
        }


        // get vector to new position in global space
        Vector3 worldDeltaPosition = agent.nextPosition - transform.position;

        // Map 'worldDeltaPosition' to local space
        float dx = Vector3.Dot(transform.right, worldDeltaPosition);
        float dy = Vector3.Dot(transform.forward, worldDeltaPosition);
        Vector2 deltaPosition = new Vector2(dx, dy);

        // Low-pass filter the deltaMove
        float smooth = Mathf.Min(1.0f, Time.deltaTime / 0.15f);
        smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);

        // Map local rotation of new position
        //Quaternion qLook = Quaternion.LookRotation(worldDeltaPosition, Vector3.up);
        //Quaternion qNorm = Quaternion.FromToRotation(Vector3.up, local_up);
        //lookRotation = qNorm * qLook;
        //transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, smooth);

        // get the speed of the agent to pass to the animator
        new_speed = Mathf.Abs(agent.velocity.magnitude);

        // calculate the new vector angle
        delta_angle = Vector3.Angle(transform.position, agent.destination) - old_angle;

        // update the angle
        old_angle = transform.localRotation.eulerAngles.y;


        // Send the variables to the animator
        anim.SetFloat(speedHash, new_speed);
        anim.SetFloat(angleHash, delta_angle);

        // Send variables to the animator
        anim.SetInteger(stateHash, Random.Range(1, 4));
        anim.SetInteger(motionHash, Random.Range(0, 2));


    }

    private void OnAnimatorMove()
    {
        // Evaluate jump status
        EvaluateJump();

    }


    // -----
    // Helper Functions
    // -----

    // Computes the escape location for the cricket when hitbox is entered by the mouse
    public void Escape()
    {
        // Set parameters to enter escape sequence
        anim.SetInteger(escapeHash, Random.Range(0, 2));
        anim.SetTrigger(encounterHash);

        // reset timer so we don't accidentally set a new position on the next Update()
        timer = 0;

        // Assume manual control of agent to prevent temporary movement
        agent.updatePosition = false;
        agent.updateRotation = false;

        // store the starting transform
        startTransform = transform;

        // temporarily point the object to look away from the player
        transform.rotation = Quaternion.LookRotation(transform.position - player.position);

        // Then we'll get the position on that rotation that's multiplyBy down the path (you could set a Random.range
        // for this if you want variable results) and store it in a new Vector3 called runTo
        multiplyBy = Random.Range(0.2f, 0.5f);
        Vector3 runTo = transform.position + transform.forward * multiplyBy;

        // So now we've got a Vector3 to run to and we can transfer that to a location on the NavMesh with samplePosition.
        UnityEngine.AI.NavMeshHit hit;    // stores the output in a variable called hit

        // 5 is the distance to check, assumes you use default for the NavMesh Layer name
        UnityEngine.AI.NavMesh.SamplePosition(runTo, out hit, 5, -1);

        // reset the transform back to our start transform
        transform.position = startTransform.position;
        transform.rotation = startTransform.rotation;

        // And get it to head towards the found NavMesh position
        agent.SetDestination(hit.position);
        agent.speed = 8.0f;
    }


    // evaluate boolean jump states and update rootNode position
    private void EvaluateJump()
    {
        // get the value of the booleans that sense jumping
        start_jump = anim.GetBool(jumpStartHash);
        finish_jump = anim.GetBool(jumpFinishHash);

        // if the jump just finished
        if (finish_jump & start_jump)
        {

            // move animation and agent transform to the end of the jump
            transform.position = rootNode.position;
            agent.Warp(rootNode.position);

            // change the speed to normal (since after avoidance speed it up)
            agent.speed = 0.1f;

            // If the cricket is to keep walking after the jump, set the next position
            if (anim.GetInteger(jumpWalkHash) == 1)
            {
                ContinueJump();
            }

            // reset the booleans
            anim.SetBool(jumpStartHash, false);
            anim.SetBool(jumpFinishHash, false);

        }

        else if (start_jump & !finish_jump)
        {
            // if mid jump, do nothing
        }

        else
        {
            // if done with jumping, move based on the agent

            // Get local normal vector based on agent
            local_up = agent.transform.up;
            // Rotate the transform to look at the new target
            transform.LookAt(agent.nextPosition, local_up);
            // Update position to agent position
            transform.position = agent.nextPosition;


            // reset the booleans
            anim.SetBool(jumpStartHash, false);
            anim.SetBool(jumpFinishHash, false);
        }
    }


    // After a jump our next position should be in the same direction as the jump
    private void ContinueJump()
    {
        // Reset the timer
        timer = 0;

        // Assume manual control of agent to prevent temporary movement
        agent.updatePosition = false;
        agent.updateRotation = false;

        // Then we'll get the position on that rotation that's multiplyBy down the path 
        Vector3 runTo = transform.position + transform.forward * 0.1f;

        // So now we've got a Vector3 to run to and we can transfer that to a location on the NavMesh with samplePosition.
        UnityEngine.AI.NavMeshHit hit;    // stores the output in a variable called hit

        // 5 is the distance to check, assumes you use default for the NavMesh Layer name
        UnityEngine.AI.NavMesh.SamplePosition(runTo, out hit, 5, 1 << UnityEngine.AI.NavMesh.GetAreaFromName("Walkable"));

        // And get it to head towards the found NavMesh position
        agent.SetDestination(hit.position);
    }


    // generate the random position of the agent within a sphere
    private static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        // get the random direction from a random number generator 
        Vector3 randDirection = Random.insideUnitSphere * dist;
        // add it to the original position
        randDirection += origin;
        // initialize the variable to store the new agent position
        UnityEngine.AI.NavMeshHit navHit;
        // get the position from the navigation agent
        UnityEngine.AI.NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);

        // return it
        return navHit.position;
    }

}