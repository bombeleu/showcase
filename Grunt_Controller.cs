// Steve Yeager
// 1/7/2013
// Gemini

using UnityEngine;
using System.Collections;


/// <summary>
/// AI for Grunt.
/// </summary>
public class Grunt_Controller : Enemy
{
	#region Variables

	#region Combat
	public Weapon
			BlasterRight;
	public int
			damagePunch,
			damageBlast;
	#endregion

	#endregion



	public override void Spawn(Vector3 position)
	{
		base.Spawn(position);
		enemyType = EnemyTypes.Grunt;
		BlasterRight.enabled = false;
	} // End Spawn


	protected override void Attack()
	{
		myAnimation.Play("Punch");
	} // End Attack


	void Punch()
	{
		BlasterRight.Toggle(true, damagePunch);
	} // End Punch


	protected override void EndAttack()
	{
		if (myAnimation.IsPlaying("Punch")) myAnimation.Stop();
		BlasterRight.Toggle(false);
	} // End EndAttack


	void FinishAttack()
	{
		EndAttack();
		StartCoroutine(AttackCoolDown(2f));
	} // End FinishAttack
	
} // End Grunt_Controller Class