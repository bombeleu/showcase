// Steve Yeager
// 11/29/2012
// Gemini

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#region Requirements
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(PlanetAlign))]
[RequireComponent(typeof(Score_Manager))]
[RequireComponent(typeof(MecanimEventEmitter))]
[RequireComponent(typeof(MecanimEventSetupHelper))]
#endregion


/// <summary>
/// Controls player input for Gemini.
/// </summary>
public class Gemini_Controller : MonoBehaviour
{
	#region Variables

	#region References
	public static Gemini_Controller main;
	Transform myTransform;
	Animator myAnimator;
	Camera myCamera;
	Transform myCameraTransform;
	public Weapon SwordWeapon;
	public Weapon GunWeapon;
	PlanetAlign myAlign;
	public Button cosmicButton;

	HUD_Manager myHUD;
	#endregion

	#region Input
	Vector2 joystickDirection; // Current joystick direction.
	int stickFingerID = -1; // The finger ID of the touch controlling the joystick. -1 if there is none.
	Touch currentStickTouch; // Current touch controlling the joystick.
	int combatFingerID = -1; // The finger ID of the touch controlling combat. -1 if there is none.
	Touch currentCombatTouch; // Current touch controlling combat.
	public float minSwipeDistance = 50f; // Minimum distance touch needs to move to be considered a swipe.
	public float maxSwipeTime = 0.7f; // Longest a touch can be active and still be a swipe.
	public float turnBuffer = 50f; // Movement on the joystick needed to stop just turning and start moving.

	int W = Screen.width;
	int	H = Screen.height;
	public Rect HUDRectPercent;
	Rect HUDRect;
	#endregion

	#region Stats
	Stat_Manager Stats = new Stat_Manager("Gemini");
	public AttackMeter_Manager AttackMeter;
	public Score_Manager ScoreManager;
	[Flags]
	public enum States
	{
		Spawning = 0,
		Idle = 1,
		Moving = 2,
		Attacking = 4,
		Hit = 8,
		Dead = 16,
		Cinema = 32,
	}
	public States state;
	bool invincible;
	public int health {get; private set;}
	public int maxHealth;
	public int score{get; private set;}
	public int cores{get; private set;}
	float timeLasted; // How long the player lasted in the level.
	#endregion

	#region Events
	public delegate void FinishingMoveHandler(string move);
	public static event FinishingMoveHandler FinishingMoveEvent;
	#endregion

	#region Combat
	public TextAsset ComboList; // List of all the combos and their info.
	Dictionary<string, Combo> combos = new Dictionary<string, Combo>(); // All of Gemini's combos.
	public string currentComboString; // Current combo being executed.
	public string nextAttack; // Attack on-deck.
	public float comboLocked = .15f; // Window after a move is finished that the combo can be continued. Can't move.
	public float comboFree = 0.3f; // Window after comboLocked that the combo can be continued.
	public int currentDamage;
	public float hitDuration = 0.5f; // How long till Gemini can be hit again.
	public enum AttackStates
	{
		None,
		Attacking,
		OpenLocked,
		OpenFree
	}
	public AttackStates attackState;
	public float attackRotateSpeed = 5f;
	public Texture[]
			cosmicPower1,
			cosmicPower2,
			cosmicPower3;
	#endregion

	#region Audio
	public AudioClip
			slashLeftSE,
			slashUpSE,
			slashRightSE,
			slashDownSE,
			shootSE;
	#endregion

	#region Settings
	int joystickBitMask;
	Vector2 joystickCenter;
	#endregion

	#endregion



	#region Mono Functions

	void Awake()
	{
		// get references
		main = this;
		myTransform = transform;
		myAnimator = GetComponent<Animator>();
		myCamera = GameObject.Find("Gemini Camera").transform.Find("Camera").camera;
		myCameraTransform = GameObject.Find("Gemini Camera").transform;
		ScoreManager = GetComponent<Score_Manager>();
		myHUD = myCameraTransform.GetComponent<HUD_Manager>();
		myAlign = GetComponent<PlanetAlign>();

		// settings
		joystickBitMask = 1<<LayerMask.NameToLayer("Joystick");
		joystickCenter = myCamera.WorldToScreenPoint(GameObject.Find("Joystick").transform.position);

		// set up
		CreateCombos();
		Texture[][] cosmicTextures = new Texture[3][];
		cosmicTextures[0] = cosmicPower1;
		cosmicTextures[1] = cosmicPower2;
		cosmicTextures[2] = cosmicPower3;
		AttackMeter = new AttackMeter_Manager(this, cosmicButton, cosmicTextures, new int[] {50, 100, 150}, new string[] {"SpeedBoost", "KillAll", "HealthBoost"});
	} // End Awake


	void Start()
	{
		// get references
		SwordWeapon = myTransform.FindInChildren("Cosmic Katana").GetComponent<Weapon>();
		SwordWeapon.Toggle(false);
		GunWeapon = myTransform.FindInChildren("Shotgun Blast").GetComponent<Weapon>();
		GunWeapon.Toggle(false);

		W = Screen.width;
		H = Screen.height-__Game_Settings.main.barHeight;
		HUDRect = new Rect(W*HUDRectPercent.x, H*HUDRectPercent.y, W*HUDRectPercent.width, H*HUDRectPercent.height);

		// set events
		Level_Manager.main.GameOverEvent += EndGame;

		Spawn();
	} // End Start


	void Update()
	{
		if (state == States.Dead) return;

		myAlign.Align();

		// reset to idle
		if (state != States.Spawning && state != States.Cinema && state != States.Dead && attackState == AttackStates.None && CanGetHit())
		{
			state = States.Idle;
		}

		if (state == States.Idle || state == States.Moving || state == States.Attacking)
		{
			GetInput();
		}

		#region Testing
		if (__Game_Settings.main.TestMode)
		{
			Move(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")));
			myHUD.MoveJoystick(new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")));

			if (Input.GetKeyDown(KeyCode.Space))
			{
				CalculateAttack('T');
			}

			if (Input.GetKeyDown(KeyCode.LeftArrow))
			{
				CalculateAttack('L');
			}
			if (Input.GetKeyDown(KeyCode.UpArrow))
			{
				CalculateAttack('U');
			}
			if (Input.GetKeyDown(KeyCode.RightArrow))
			{
				CalculateAttack('R');
			}
			if (Input.GetKeyDown(KeyCode.DownArrow))
			{
				CalculateAttack('D');
			}

			if (Input.GetKeyDown(KeyCode.R))
			{
				AttackMeter.UnleashAttack();
			}
		}
		#endregion
	} // End Update


	void OnTriggerEnter(Collider colliderInfo)
	{
		// get hurt by enemy
		if (colliderInfo.tag == "Enemy Attack" && CanGetHit())
		{
			CalculateHit(colliderInfo.GetComponent<Weapon>().damage);
			colliderInfo.enabled = false;
		}
	} // End OnTriggerEnter

	#endregion

	#region State Functions

	void Spawn()
	{
		state = States.Spawning;
		health = maxHealth;

		timeLasted = Time.time;

		state = States.Idle;
	} // End Spawn


	void Die()
	{
		health = 0;
		state = States.Dead;
		myAnimator.SetBool("hit", false);
		myAnimator.SetBool("dead", true);
	} // End Die


	bool CanGetHit()
	{
		return !state.ContainsValue(States.Spawning) && !state.ContainsValue(States.Hit) && !state.ContainsValue(States.Dead) && !state.ContainsValue(States.Cinema) && !invincible;
	} // End CanGetHit


	void EndGame(bool won)
	{		
		timeLasted = Time.time-timeLasted;

		Stats.UpdateScore(Level_Manager.main.level, score);
		Dictionary<string, int[]> finalKills = new Dictionary<string,int[]>();
		finalKills.Add("Pests Killed", new int[]{Stats.killedPests, 0});
		finalKills.Add("Grunts Killed", new int[]{Stats.killedGrunts, 0});
		finalKills.Add("Mammoths Killed", new int[]{Stats.killedMammoths, 0});

		Stats.Save();


		__Game_Settings.main.SetScore(finalKills, ScoreManager.CalculateFinalScore(timeLasted));
	} // End EndGame

	#endregion

	#region Input Functions

	void GetInput()
	{
		for (int i = 0; i < Input.touchCount; i++)
		{
			switch (Input.GetTouch(i).phase)
			{
				case TouchPhase.Began:
					if (stickFingerID == -1 && Physics.Raycast(myCamera.ScreenPointToRay(new Vector3(Input.GetTouch(i).position.x, Input.GetTouch(i).position.y, 0f)), 1f, joystickBitMask))
					{
						stickFingerID = Input.GetTouch(i).fingerId;
						currentStickTouch = Input.GetTouch(i);
						Move(currentStickTouch.position-joystickCenter);
						myHUD.MoveJoystick(currentStickTouch.position-joystickCenter);
					}
					else if (combatFingerID == -1)
					{
						combatFingerID = Input.GetTouch(i).fingerId;
						currentCombatTouch = Input.GetTouch(i);
						StartCoroutine(CombatInput());
					}
				break;

				case TouchPhase.Stationary:
				case TouchPhase.Moved:
					if (Input.GetTouch(i).fingerId == stickFingerID)
					{
						currentStickTouch = Input.GetTouch(i);
						Move(currentStickTouch.position-joystickCenter);
						myHUD.MoveJoystick(currentStickTouch.position-joystickCenter);
					}
					else if (Input.GetTouch(i).fingerId == combatFingerID)
					{
						currentCombatTouch = Input.GetTouch(i);
					}
				break;
					
				case TouchPhase.Ended:
				case TouchPhase.Canceled:
					if (Input.GetTouch(i).fingerId == stickFingerID)
					{
						stickFingerID = -1;
						state = States.Idle;
						myAnimator.SetBool("moving", false);
						myHUD.MoveJoystick(Vector2.zero);
					}
					else if (Input.GetTouch(i).fingerId == combatFingerID)
					{
						combatFingerID = -1;
						currentCombatTouch = Input.GetTouch(i);
					}
				break;
			}
		}
	} // End GetInput


	/// <summary>
	/// Interpret the touch input as a swipe and what direction.
	/// </summary>
	IEnumerator CombatInput()
	{
		Vector2 initialPosition = currentCombatTouch.position;
		if (!OutsideGUI(initialPosition)) yield break;

		float initialTime = Time.time;
		bool possibleSwipe = true;
		bool possibleTap = true;


		// wait until touch ends or stationary
		while (currentCombatTouch.phase != TouchPhase.Ended && currentCombatTouch.phase != TouchPhase.Canceled)
		{
			if (Time.time - initialTime > maxSwipeTime)
			{
				possibleSwipe = false;
				break;
			}

			yield return new WaitForEndOfFrame();
		}

		// outside tap zone
		float xDistance = currentCombatTouch.position.x - initialPosition.x;
		float yDistance = currentCombatTouch.position.y - initialPosition.y;

		if (Mathf.Abs(xDistance) < minSwipeDistance && Mathf.Abs(yDistance) < minSwipeDistance)
		{
			possibleSwipe = false;
		}
		else
		{
			possibleTap = false;
		}

		// swipe
		if (possibleSwipe)
		{
			if (Mathf.Abs(xDistance) >= Mathf.Abs(yDistance))
			{
				if (Mathf.Sign(xDistance) > 0)
				{
					CalculateAttack('R');
				}
				else
				{
					CalculateAttack('L');
				}
			}
			else
			{
				if (Mathf.Sign(yDistance) > 0)
				{
					CalculateAttack('U');
				}
				else
				{
					CalculateAttack('D');
				}
			}
		}
		// tap
		else if (possibleTap)
		{
			CalculateAttack('T');
		}
	} // End CombatInput


	bool OutsideGUI(Vector2 point)
	{
		point.y = H-point.y;
		return !HUDRect.Contains(point);
	} // End OutsideGUI

	#endregion

	#region Movement Functions

	/// <summary>
	/// Rotate Gemini and move forward.
	/// </summary>
	/// <param name="direction">direction to move from camera transform.</param>
	void Move(Vector2 direction)
	{
		if (state == States.Hit) return;

		// don't rotate or move on finishing attack
		if (state == States.Attacking)
		{
			if (!combos[currentComboString].isFinishingMove)
			{
				AttackRotate(direction);
			}
			return;
		}
		else
		{
			Rotate(direction);
		}

		// move
		if ((direction.magnitude >= turnBuffer || (__Game_Settings.main.TestMode && direction.magnitude > 0)) && (attackState == AttackStates.None || attackState == AttackStates.OpenFree))
		{
			state = States.Moving;
			myAnimator.SetBool("moving", true);
			EndCombo();
		}
		else
		{
			myAnimator.SetBool("moving", false);
		}
	} // End Turn


	/// <summary>
	/// Rotate Gemini around his y axis.
	/// </summary>
	/// <param name="direction">Direction to face.</param>
	void Rotate(Vector2 direction)
	{
		direction.Normalize();
		Vector3 target = myTransform.position;
		target += myCameraTransform.right*direction.x;
		target += myCameraTransform.forward*direction.y;
		myTransform.LookAt(target, myTransform.up);
	} // End Rotate


	/// <summary>
	/// Slowly rotate Gemini to face direction.
	/// </summary>
	/// <param name="direction">direction to move from camera transform.</param>
	void AttackRotate(Vector2 direction)
	{
		if (direction.magnitude == 0) return;

		direction.Normalize();

		Vector3 target = myTransform.position;
		target += myCameraTransform.right*direction.x;
		target += myCameraTransform.forward*direction.y;

		Vector3 difference = target-myTransform.position;
		Quaternion rotation = Quaternion.LookRotation(difference, myTransform.up);
		myTransform.rotation = Quaternion.Slerp(myTransform.rotation, rotation, attackRotateSpeed*Time.deltaTime);
	} // End AttackRotate

	#endregion

	#region Combat Functions

	/// <summary>
	/// Create combos from the ComboList.
	/// </summary>
	void CreateCombos()
	{
		combos.Clear();

		string[] comboLines = ComboList.text.Split('\n');

		for (int i = 2; i < comboLines.Length; i++)
		{
			// get next
			string curCombo = comboLines[i];

			// create attack
			curCombo.Trim().ToUpper();
			string[] info = curCombo.Split(',');

			// create attack number
			string comboNumber = "";
			for (int j = 0; j < info[0].Length; j++)
			{
				switch (info[0][j])
				{
					case 'L':
						comboNumber += "1";
					break;
					case 'U':
						comboNumber += "2";
					break;
					case 'R':
						comboNumber += "3";
					break;
					case 'D':
						comboNumber += "4";
					break;
					case 'T':
						comboNumber += "5";
					break;
				}
			}
			// combo, number, name, damage, function
			Combo curComboInfo = new Combo(info[0], int.Parse(comboNumber), info[1], int.Parse(info[2]), info[3]);
			combos.Add(info[0], curComboInfo);
		}
	} // End CreateCombos


	/// <summary>
	/// Determine what attack should be executed.
	/// </summary>
	/// <param name="currentAttack">Direction of the currect swipe.</param>
	void CalculateAttack(char currentAttack)
	{
		if (attackState == AttackStates.Attacking)
		{
			if (!combos[currentComboString].isFinishingMove)
			{
				nextAttack = currentAttack.ToString();
			}
			return;
		}
		StopCoroutine("ComboCoolDown");
		state = States.Attacking;
		myAnimator.SetBool("moving", false);

		// possible combo
		if (combos.ContainsKey(currentComboString+currentAttack))
		{
			currentComboString += currentAttack;
			Attack(combos[currentComboString], currentAttack == 'T');
		}
		else
		{
			myAnimator.SetBool("attacking", false);
			currentComboString = currentAttack.ToString();
			StartCoroutine("DelayedAttack", currentAttack == 'T');
		}

		
	} // End CalculateAttack


	IEnumerator DelayedAttack(bool shot)
	{
		yield return new WaitForEndOfFrame();
		Attack(combos[currentComboString], shot);
	} // End DelayedAttack


	/// <summary>
	/// Execute an attack.
	/// </summary>
	/// <param name="attack">What combo to execute.</param>
	void Attack(string attack, bool gun)
	{
		Attack(combos[attack], gun);
	} // End Attack

	/// <summary>
	/// Execute an attack.
	/// </summary>
	/// <param name="attack">What combo to execute.</param>
	void Attack(Combo attack, bool gun)
	{
		attackState = AttackStates.Attacking;
		myAnimator.SetBool("attacking", true);
		myAnimator.SetInteger("attackNum", attack.number);
		currentDamage = attack.damage;

		if (gun)
		{
			GunWeapon.Toggle(true, currentDamage);
			AudioSource.PlayClipAtPoint(shootSE, myTransform.position, __Game_Settings.main.seVolume);
		}
		else
		{
			SwordWeapon.Toggle(true, currentDamage);
			AudioSource.PlayClipAtPoint(slashLeftSE, myTransform.position, __Game_Settings.main.seVolume);
		}

		if (attack.isFinishingMove)
		{
			ComboExecuted(attack.name);
		}
	} // End Attack


	/// <summary>
	/// Gemini starts/stops dealing damage.
	/// </summary>
	/// <param name="on">Start or stop attacking.</param>
	[Obsolete("Currently the whole attack deals damage", true)]
	public void ToggleAttack(int onNum)
	{
		bool on = (onNum == 1);

		//SwordWeapon.damage = currentDamage;
		//SwordCollider.enabled = on;

		if (on)
		{
			attackState = AttackStates.Attacking;
		}
		else
		{
			myAnimator.SetInteger("attackNum", 0);
			//attackState = AttackStates.Finishing;
		}
	} // End ToggleAttack


	/// <summary>
	/// Ends the current attack animation.
	/// </summary>
	void EndAttack(string weapon)
	{
		myAnimator.SetInteger("attackNum", 0);
		currentDamage = 0;

		if (weapon == "sword")
		{
			SwordWeapon.Toggle(false);
		}
		else
		{
			GunWeapon.Toggle(false);
		}

		if (String.IsNullOrEmpty(nextAttack))
		{
			attackState = AttackStates.OpenLocked;
			StartCoroutine("ComboCoolDown");
			state = States.Idle;
		}
		else
		{
			attackState = AttackStates.None;
			CalculateAttack(nextAttack[0]);
			nextAttack = "";
		}
	} // End EndAttack


	/// <summary>
	/// Combo window after the attack stops dealing damage to advance the combo.
	/// </summary>
	IEnumerator ComboCoolDown()
	{
		yield return new WaitForSeconds(comboLocked);
		attackState = AttackStates.OpenFree;
		yield return new WaitForSeconds(comboFree);
		EndCombo();
	} // End ComboCoolDown


	/// <summary>
	/// Ends the current combo and resets the values.
	/// </summary>
	void EndCombo()
	{
		attackState = AttackStates.None;
		myAnimator.SetBool("attacking", false);
		nextAttack = null;
		currentComboString = "";
	} // End EndCombo


	/// <summary>
	/// Calculate outcome of recieving a hit from an enemy.
	/// </summary>
	/// <param name="damage">Damage recieved.</param>
	void CalculateHit(int damage)
	{
		if (!CanGetHit())return;
		myAnimator.SetBool("moving", false);
		health -= damage;

		if (health <= 0)
		{
			Die();
		}
		else
		{
			Hit();
		}
	} // End CalculateHit


	/// <summary>
	/// Stop hit animation from looping.
	/// </summary>
	void EndHit()
	{
		myAnimator.SetInteger("hitDirection", 0);
		myAnimator.SetBool("hit", false);
		StartCoroutine(Invincible());
		state = States.Moving;
	} // End EndHit


	void Hit()
	{
		myAnimator.SetInteger("hitDirection", 2);
		myAnimator.SetBool("hit", true);
		StartCoroutine(DelayedHit());
		state = States.Hit;
		EndCombo();
	} // End Hit


	IEnumerator DelayedHit()
	{
		yield return new WaitForEndOfFrame();
		myAnimator.SetBool("invincible", true);
	} // End DelayedHit


	IEnumerator Invincible()
	{
		invincible = true;
		yield return new WaitForSeconds(1);
		invincible = false;
		myAnimator.SetBool("invincible", false);
	} // End Invincible


	public void RetrieveHit(Enemy.EnemyTypes enemy, Vector3 enemyPosition)
	{
		ScoreManager.RetrieveHit(enemyPosition);
	} // End RetrieveHit


	/// <summary>
	/// After an enemy is killed update correct stats.
	/// </summary>
	/// <param name="enemy">Type of enemy killed.</param>
	/// <param name="exp">How much experience gained.</param>
	public void RetrieveKill(Enemy.EnemyTypes enemy, int points)
	{
		Stats.UpdateKills(enemy);
		AttackMeter.IncreaseMeter(points);
		ScoreManager.RetrieveKill(enemy, points);
	} // End RetrieveKill


	/// <summary>
	/// A finishing move has been made.
	/// </summary>
	/// <param name="comboName">Name of the combo.</param>
	void ComboExecuted(string comboName)
	{
		if (FinishingMoveEvent != null) FinishingMoveEvent(comboName);

		ScoreManager.ExecutedFinishingMove(comboName);
	} // End ComboExecuted

	#endregion

	#region Cosmic Attack Functions

	public void ActivatePower()
	{
		AttackMeter.UnleashAttack();
	} // End ActivatePower


	/// <summary>
	/// Increase health to max.
	/// </summary>
	void HealthBoost()
	{
		health += 50;
		if (health > maxHealth) health = maxHealth;
	} // End HealthBoost


	/// <summary>
	/// Increase Gemini run speed.
	/// </summary>
	[Obsolete("Would need to have another run animation and state.")]
	void SpeedBoost()
	{
		myAnimator.speed = 2f;
		Invoke("CancelSpeedBoost", 10f);
	} // End SpeedBoost


	/// <summary>
	/// Return Gemini to normal speed.
	/// </summary>
	void CancelSpeedBoost()
	{
		myAnimator.speed = 1f;
	} // End CancelSpeedBoost


	/// <summary>
	/// Kill all enemies currenlty spawned.
	/// </summary>
	void KillAll()
	{
		GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
		for (int i = 0; i < enemies.Length; i++)
		{
			enemies[i].GetComponent<Enemy>().Die(false);
		}
	} // End KillAll

	#endregion

	#region Interaction Functions

	/// <summary>
	/// Increase the amount of cores.
	/// </summary>
	/// <param name="amount">Amount to increase by.</param>
	public void CollectCore(int amount)
	{
		cores += amount;
	} // End CollectCore

	#endregion

} // End Gemini_Controller Class




/// <summary>
/// Has all the info needed for an attack in a combo.
/// </summary>
public struct Combo
{	
	public string combo; // The combo string needed to execute this move.
	public string name; // The name of the combo. Blank unless it is the final move.
	public int number; // Number associated with combo string. L=1, U=2, R=3, D=4, T=5
	public int damage; // How much damage this attack does.
	public string attackFunction; // Function called for a special attack.

	// Returns true if the move is the last move in a combo.
	public bool isFinishingMove
	{
		get { return !String.IsNullOrEmpty(name); }
	}


	public Combo(string combo, int number, string name, int damage, string attackFunction)
	{
		this.combo = combo;
		this.number = number;
		this.name = name;
		this.damage = damage;
		this.attackFunction = attackFunction;
	} // End ComboInfo


	public override string ToString()
	{
		return combo;
	} // End ToString

} // End Combo Struct