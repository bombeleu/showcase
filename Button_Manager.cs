// Steve Yeager
// 1/22/2013

using UnityEngine;
using System.Collections;


/// <summary>
/// Manages button interactions.
/// </summary>
public class Button_Manager : MonoBehaviour
{
	#region Variables
	/// <summary>Reference to script with button functions.</summary>
	public MonoBehaviour Manager;
	Camera mainCamera;
	/// <summary>How far to raycast.</summary>
	public float maxButtonDistance = 10f;
	/// <summary>Active button being held down.</summary>
	Button activeButton;
	/// <summary>Button physics layer.</summary>
	int buttonLayer;
	int touchID = -1;
	RaycastHit rayInfo;
	#endregion



	void Awake()
	{
		mainCamera = Camera.main;
		buttonLayer = 1 << LayerMask.NameToLayer("Button");
	} // End Awake


	void Update()
	{
		if (touchID != -1)
		{
			if (Input.GetTouch(touchID).phase == TouchPhase.Ended)
			{
				if (Physics.Raycast(mainCamera.ScreenPointToRay(Input.GetTouch(touchID).position), out rayInfo, maxButtonDistance, buttonLayer))
				{
					if (rayInfo.collider == activeButton.collider)
					{
						Manager.SendMessage(activeButton.activeFunction, activeButton.functionParameter);
					}
				}
				activeButton.Activate(false);
				activeButton = null;
				touchID = -1;
			}
		}
		else
		{
			for (int i = 0; i < Input.touchCount; i++)
			{
				if (Input.GetTouch(i).phase == TouchPhase.Began)
				{
					if (Physics.Raycast(mainCamera.ScreenPointToRay(Input.GetTouch(i).position), out rayInfo, maxButtonDistance, buttonLayer))
					{
						if (rayInfo.collider.GetComponent<Button>().state == Button.States.Normal)
						{
							touchID = Input.GetTouch(i).fingerId;
							activeButton = rayInfo.collider.GetComponent<Button>();
							activeButton.Activate(true);
						}
					}
				}
			}
		}
	} // End Update

} // End Button_Manager Class