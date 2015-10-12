using UnityEngine;
using System.Collections;

using Parse;

public class EntryController : Controller, InputManager.InputListener
{
	private enum EntryState
	{
		Normal,
		LoggedIn,
		Offline
	}

	public TypeWriter Writer;

	public string GoLevelName;
	public string LoginLevelName;

	// internal data
	private EntryState entryState;

	protected override void Awake() 
	{
		base.Awake ();

		InputManager.Instance.SetInputListener (this);
	}

	private void Start()
	{
		if (ParseUser.CurrentUser != null) 
		{
			PromptLoggedIn();
		} 
		else 
		{
			PromptNormal();
		}
	}
	
	public void OnTouchBegin()
	{
	}

	public void OnTap()
	{
		if (!isActive)
			return;

		StartCoroutine(GoToLevelCoroutine(GoLevelName));
	}

	public void OnHold()
	{
		if (!isActive)
			return;

		switch (entryState) 
		{
			case EntryState.LoggedIn:
				ParseUser.LogOutAsync();
				PromptNormal();
				
				break;
			case EntryState.Normal:
				StartCoroutine (GoToLevelCoroutine(LoginLevelName));
			break;
		}
	}

	private void PromptLoggedIn()
	{
		entryState = EntryState.LoggedIn;
		Writer.WriteTextInstant ("Logged in as " + ParseUser.CurrentUser.Username + "\n"+
		                         "[Tap] to play\n" +
		                         "[Hold] to logout\n");
	}

	private void PromptNormal()
	{
		entryState = EntryState.Normal;
		Writer.WriteTextInstant ("[Tap] to play offline\n" +
		                         "[Hold] to login or signup\n");
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