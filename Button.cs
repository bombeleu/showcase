// Steve Yeager
// 12/31/2012

using UnityEngine;
using System.Collections;


/// <summary>
/// Basic GUI button functionality.
/// </summary>
public class Button : MonoBehaviour
{
	#region Variables
	public enum States
	{
		Disabled = -1,
		Normal = 0,
		Active = 1
	}
	public States state;
	public string activeFunction; // What function to call when clicked.
	public string functionParameter; // Optional parameter to pass when clicked.
	public Texture
			disabledTex,
			activeTex,
			normalTex;
	#endregion



	/// <summary>
	/// Activate or deactivate the button.
	/// </summary>
	/// <param name="on">Button active status.</param>
	public virtual void Activate(bool on)
	{
		if (state == States.Disabled) return;

		if (on)
		{
			state = States.Active;
			renderer.material.mainTexture = activeTex;
		}
		else
		{
			state = States.Normal;
			renderer.material.mainTexture = normalTex;
		}
	} // End Activate


	/// <summary>
	/// Toggle button disabled status.
	/// </summary>
	/// <param name="disable">Button disabled status.</param>
	public virtual void Disable(bool disable)
	{
		if (disable)
		{
			state = States.Disabled;
			renderer.material.mainTexture = disabledTex;
		}
		else
		{
			state = States.Normal;
			renderer.material.mainTexture = normalTex;
		}
	} // End Disable

} // End Button Class
