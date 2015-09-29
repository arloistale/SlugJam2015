using UnityEngine;
using System.Collections;

public class EntryController : Controller, InputManager.InputListener
{
	public string GoLevelName;

	protected override void Awake() 
	{
		base.Awake ();

		InputManager.Instance.SetInputListener (this);
	}

	public void OnTapBegin()
	{
	}

	public void OnTapEnd()
	{
		if (!isActive)
			return;

		Go (GoLevelName);
	}

	public void OnTapLong()
	{
	}

	/// <summary>
	/// Go the specified levelName.
	/// </summary>
	private void Go(string levelName)
	{		
		//GUIManager.Instance.FaderOn(true,OutroFadeDuration);
		StartCoroutine(GotoLevelCo(levelName));
	}
	
	/// <summary>
	/// Waits for a short time and then loads the specified level
	/// </summary>
	private IEnumerator GotoLevelCo(string levelName)
	{
		SetActive (false);

		//yield return new WaitForSeconds(OutroFadeDuration);
		
		if (string.IsNullOrEmpty(levelName))
			Application.LoadLevel("Entry");
		else
			Application.LoadLevel(levelName);
		
		GameManager.Instance.Reset ();
		
		yield return null;
	}
}