using UnityEngine;
using System.Collections;

public class WanderingAI_escape : MonoBehaviour {

	public float wanderRadius;
	public float wanderTimer;
	public Animator anim;
	public float multiplyBy;

	private Transform target;
	private UnityEngine.AI.NavMeshAgent agent;
	private float timer;
	private float new_speed = 0;
	private float delta_angle = 0;
	private float old_angle = 0;
	private Transform player;
	private Transform startTransform;
	private Vector3 local_up = new Vector3 (0.0f, 1.0f, 0.0f);

	Vector2 smoothDeltaPosition = Vector2.zero;
	Vector2 velocity = Vector2.zero;


	// Use this for initialization
	void OnEnable () {
		// get the navigation agent
		agent = GetComponent<UnityEngine.AI.NavMeshAgent> ();
		// initialize the timer for wandering
		timer = wanderTimer;
		// get the object of the player to avoid
		player = GameObject.FindGameObjectWithTag("Player").transform;
		// prevent the agent to change the position and rotation of the cricket
		agent.updatePosition = false;
		agent.updateRotation = false;

	}

	// Update is called once per frame
	void Update () {
		// update the timer
		timer += Time.deltaTime;

		// if over the threshold
		if (timer >= wanderTimer) {
			// get a random position based on the radius and then navigation mesh
			Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
			// set the destination for the agent
			agent.SetDestination(newPos);
			// change the speed to normal (since after avoidance speed it up)
			agent.speed = 0.1f;

			// reset the timer
			timer = 0;

		}

		Vector3 worldDeltaPosition = agent.nextPosition - transform.position;

		// Map 'worldDeltaPosition' to local space
		float dx = Vector3.Dot (transform.right, worldDeltaPosition);
		float dy = Vector3.Dot (transform.forward, worldDeltaPosition);
		Vector2 deltaPosition = new Vector2 (dx, dy);

		// Low-pass filter the deltaMove
		float smooth = Mathf.Min(1.0f, Time.deltaTime/0.15f);
		smoothDeltaPosition = Vector2.Lerp (smoothDeltaPosition, deltaPosition, smooth);
		// Update velocity if time advances
		if (Time.deltaTime > 1e-5f) {
			velocity = smoothDeltaPosition / Time.deltaTime;
		}

		// get the speed of the agent to pass to the animator
		new_speed = Mathf.Abs(agent.velocity.magnitude);


		// calculate the new vector angle
		delta_angle = Vector3.Angle (transform.position, agent.destination) - old_angle;

//		// update the angle
		old_angle = transform.localRotation.eulerAngles.y;



//		Debug.Log (delta_angle);


		// Send the variables to the animator
		anim.SetFloat ("Speed", new_speed);

		anim.SetInteger("State_selector", Random.Range(1,4));

		anim.SetInteger ("Jump_selector", Random.Range(2,3));

		anim.SetFloat ("Angle", delta_angle);


	}	


	void OnAnimatorMove ()
	{
		// Rotate the transform to look at the new target
		transform.LookAt(agent.nextPosition, local_up);


		// Update position to agent position
		transform.position = agent.nextPosition;

	}
	// function to generate the random position of the agent within a sphere
	public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask) {
		// get the random direction from a random number generator 
		Vector3 randDirection = Random.insideUnitSphere * dist;
//		Debug.Log (randDirection);
//		randDirection = (new Vector3(0.3f, 0.0f, 0.2f) - randDirection);
		// add it to the original position
		randDirection += origin;
//		Debug.Log (randDirection);
		// initialize the variable to store the new agent position
		UnityEngine.AI.NavMeshHit navHit;
		// get the position from the navigation agent
		UnityEngine.AI.NavMesh.SamplePosition (randDirection, out navHit, dist, layermask);
		// return it
		return navHit.position;
	}

	// function triggered when the collider of the cricket hits another collider
	private void OnTriggerEnter(Collider other)
//	private void OnCollisionEnter(Collision dataFromCollision)
	{
		// get the local up vector to fix the rotation when on walls
		if (other.gameObject.name != "Mouse") {
			local_up = other.transform.up;
		}
		// if it's the mouse, trigger the escape
		if (other.gameObject.name == "Mouse") {
			agent.updatePosition = false;
			agent.updateRotation = false;
			Debug.Log ("trigger!");

			// store the starting transform
			startTransform = transform;

			//temporarily point the object to look away from the player
			transform.rotation = Quaternion.LookRotation (transform.position - player.position);

			//Then we'll get the position on that rotation that's multiplyBy down the path (you could set a Random.range
			// for this if you want variable results) and store it in a new Vector3 called runTo
			Vector3 runTo = transform.position + transform.forward * multiplyBy;
			//Debug.Log("runTo = " + runTo);

			//So now we've got a Vector3 to run to and we can transfer that to a location on the NavMesh with samplePosition.

			UnityEngine.AI.NavMeshHit hit;    // stores the output in a variable called hit

			// 5 is the distance to check, assumes you use default for the NavMesh Layer name
			UnityEngine.AI.NavMesh.SamplePosition (runTo, out hit, 5, 1 << UnityEngine.AI.NavMesh.GetAreaFromName ("Walkable")); 
			//Debug.Log("hit = " + hit + " hit.position = " + hit.position);

//		// just used for testing - safe to ignore
//		nextTurnTime = Time.time + 5;

			// reset the transform back to our start transform
			transform.position = startTransform.position;
			transform.rotation = startTransform.rotation;

			// And get it to head towards the found NavMesh position
			agent.SetDestination (hit.position);
			agent.speed = 1.0f;
//			agent.updatePosition = true;
//			agent.updateRotation = true;
//		}elif(other.gameObject.name == "Arena"){
		}

	}

}
