// Steve Yeager
// 11/4/2012
// Samurai Zac

using System;
using UnityEngine;
using System.Collections;


/// <summary>
/// Base enemy class.
/// </summary>
[RequireComponent(typeof(PlanetAlign))]
[RequireComponent(typeof(NavAgent))]
public class Enemy : MonoBehaviour
{
	#region Variables

	#region References
	protected GameObject myGameObject;
	protected Transform myTransform;
	protected PlanetAlign myGravity;
	protected NavAgent myNavAgent;
	protected PlanetAlign myAlign;
	protected Animation myAnimation;
	Renderer myRenderer;

	protected Transform PlayerTransform;
	static Object_Recycler CoreStash;
	public GameObject RobotCore_Prefab;
	#endregion

	#region Stats
	public enum EnemyTypes
	{
		Pest,
		Grunt,
		Mammoth
	}
	protected EnemyTypes enemyType;
	[Flags]
	public enum States
	{
		Spawning = 0,
		Idle = 1,
		Moving = 2,
		Attacking = 4,
		Hit = 8,
		Dead = 16,
		Cinema = 32
	}
	public States state;	
	public float maxHealth;
	public float health {get; private set;}
	public int points;
	public int cores;
	public Vector3 coreExplosion;
	public float attentionRadius;
	#endregion

	#region Combat
	public enum AttackStates
	{
		Ready,
		Waiting,
		Attacking
	}
	public AttackStates attackState;
	public float hitDuration;
	public float attackRadius = 0.5f;
	#endregion

	#region Movement
	public float moveSpeed = 5f;
	public float lookSpeed = 1f;
	public float FOV = 50f; // Field of view.
	public float nodeDistanceNeeded = 1f; // How close the enemy has to be to the next node to consider it there.
	#endregion

	#region Audio
	public AudioClip[] hitSE;
	public AudioClip[] deathSE;
	#endregion

	#endregion



	#region Mono Functions

	void Awake()
	{
		// get references
		myGameObject = gameObject;
		myTransform = transform;
		myGravity = GetComponent<PlanetAlign>();
		myNavAgent = GetComponent<NavAgent>();
		myRenderer = GetComponentInChildren<Renderer>();
		myRenderer.enabled = false;
		myAnimation = animation;
		myAlign = GetComponent<PlanetAlign>();

		if (CoreStash == null)
		{
			CoreStash = new Object_Recycler(RobotCore_Prefab);
		}

		// set events
		Level_Manager.main.GameOverEvent += GameOver;

		PlayerTransform = GameObject.Find("Gemini").transform;
		myAnimation["Punch"].normalizedSpeed = 1.5f;
	} // End Awake


	protected virtual void Update()
	{
		switch (state)
		{
			case States.Idle:
				Idle();
			break;
			case States.Moving:
				Moving();
			break;
			case States.Attacking:
				Attacking();
			break;
		}
	} // End Update


	void OnTriggerEnter(Collider colliderInfo)
	{
		if (colliderInfo.tag == "Weapon" && colliderInfo.enabled)
		{
			CalculateHit(colliderInfo.GetComponent<Weapon>().damage);
		}
	} // End OnTriggerEnter


	void OnDestroy()
	{
		Level_Manager.main.GameOverEvent -= GameOver;
	} // End OnDestroy

	#endregion


	#region State Functions

	protected virtual void Idle()
	{
		if (Vector3.Distance(myTransform.position, PlayerTransform.position) <= attentionRadius)
		{
			StartCoroutine("Navigate", PlayerTransform);
			state = States.Moving;
		}
		else
		{
			myAnimation.CrossFade("Idle");
		}
	} // End Idle


	protected virtual void Moving()
	{
		if (myNavAgent.hasPath)
		{
			MoveTo(myNavAgent.next);

			if (Vector3.Distance(myTransform.position, myNavAgent.next) <= nodeDistanceNeeded)
			{
				myNavAgent.path.RemoveAt(0);
			}
		}
		else
		{
			MoveTo(PlayerTransform.position);
		}

		if (Vector3.Distance(myTransform.position, PlayerTransform.position) > attentionRadius)
		{
			StopCoroutine("Navigate");
			state = States.Idle;
			return;
		}

		if (Vector3.Distance(myTransform.position, PlayerTransform.position) <= attackRadius)
		{
			StopCoroutine("Navigate");
			state = States.Attacking;
			return;
		}
	} // End Moving


	protected virtual void Attacking()
	{
		if (Vector3.Distance(myTransform.position, PlayerTransform.position) > attackRadius)
		{
			EndAttack();
			StartCoroutine("Navigate", PlayerTransform);
			state = States.Moving;
			return;
		}

		Look(PlayerTransform.position);

		switch (attackState)
		{
			case AttackStates.Ready:
				Attack();
			break;
			case AttackStates.Waiting:
				myAnimation.CrossFade("Idle");
			break;
		}
	} // End Attacking


	public virtual void Spawn(Vector3 position)
	{
		myTransform.position = position;
		myAlign.Align();

		myRenderer.enabled = true;
		state = States.Spawning;
		attackState = AttackStates.Ready;
		health = maxHealth;
		myNavAgent.FindPath(PlayerTransform.position);		
		state = States.Idle; // no spawning animation yet
	} // End Spawn


	public virtual void Die(bool killed)
	{
		state = States.Dead;

		Level_Manager.main.RetrieveSpawnPoints(enemyType);
		LoseCores();
		if (killed)
		{
			Gemini_Controller.main.RetrieveKill(enemyType, points);
		}

		AudioSource.PlayClipAtPoint(deathSE[0], myTransform.position, __Game_Settings.main.seVolume);

		myRenderer.enabled = false;
		myGameObject.SetActive(false);
	} // End Die


	protected virtual void GameOver(bool won)
	{

	} // End GameOver

	#endregion


	#region Movement Functions

	protected void Look(Vector3 target)
	{
		// get vector between target and position
		// get rotation needed to look at the target
		myTransform.rotation = Quaternion.Slerp(myTransform.rotation, Quaternion.FromToRotation(myTransform.forward, target-myTransform.position) * myTransform.rotation, lookSpeed*Time.deltaTime);

		// fix rotation
		myTransform.rotation = Quaternion.FromToRotation(myTransform.up, myGravity.lastNormal) * myTransform.rotation;
	} // End Look


	protected void MoveTo(Vector3 targetPosition)
	{
		Look(targetPosition);

		myAlign.Align();

		// only move if target is insight
		if (Vector3.Angle(targetPosition-myTransform.position, myTransform.forward) <= FOV)
		{
			myAnimation.CrossFade("Run");
			myTransform.position += myTransform.forward*moveSpeed*Time.deltaTime;
		}
	} // End MoveTo


	protected IEnumerator Navigate(Transform target)
	{
		while (true)
		{
			myNavAgent.FindPath(target.position);
			yield return new WaitForSeconds(1f);
		}
	} // End Navigate

	#endregion


	#region Combat Functions

	protected virtual void Attack()
	{

	} // End Attack


	protected IEnumerator AttackCoolDown(float time)
	{
		attackState = AttackStates.Waiting;
		yield return new WaitForSeconds(time);
		attackState = AttackStates.Ready;
	} // End AttackCoolDown


	protected virtual void EndAttack()
	{

	} // End EndAttack


	void CalculateHit(float damage)
	{
		if (state == States.Hit || state == States.Dead) return;

		health -= damage;

		if (health <= 0)
		{
			Hit();
			Die(true);
		}
		else
		{
			Hit();
		}
	} // End CalculateHit


	protected virtual void Hit()
	{
		state = States.Hit;
		EndAttack();
		myAnimation.CrossFade("GetHit1");
		Gemini_Controller.main.RetrieveHit(enemyType, myTransform.position);
		AudioSource.PlayClipAtPoint(hitSE[0], myTransform.position, __Game_Settings.main.seVolume);
		StartCoroutine(HitBuffer());
	} // End Hit


	IEnumerator HitBuffer()
	{
		yield return new WaitForSeconds(hitDuration);
		state = States.Idle;
	} // End HitBuffer


	void LoseCores()
	{
		for (int i = 0; i < cores; i++)
		{
			GameObject core = CoreStash.nextFree;
			core.transform.position = myTransform.position;
			core.rigidbody.AddForce(UnityEngine.Random.Range(-coreExplosion.x, coreExplosion.x), UnityEngine.Random.Range(0, coreExplosion.y), UnityEngine.Random.Range(-coreExplosion.z, coreExplosion.z), ForceMode.Impulse);
		}
	} // End LoseCores

	#endregion

} // End Enemy Class