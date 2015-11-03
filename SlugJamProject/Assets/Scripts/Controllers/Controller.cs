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
	
	public void GoToLevel(string levelName)
	{
		StartCoroutine(GoToLevelCoroutine(levelName));
	}
	
	/// <summary>
	/// Waits for a short time and then loads the specified level
	/// </summary>
	private IEnumerator GoToLevelCoroutine(string levelName)
	{
		isActive = false;
		
		if (!string.IsNullOrEmpty(levelName))
			Application.LoadLevel(levelName);
		
		GameManager.Instance.Reset ();
		
		yield return null;
	}
}