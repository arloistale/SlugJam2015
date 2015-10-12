using UnityEngine;
using System.Collections;

public class Controller : MonoBehaviour 
{
	protected bool isActive;

	protected virtual void Awake() 
	{
		isActive = true;
	}

	public bool IsActive()
	{
		return isActive;
	}
}