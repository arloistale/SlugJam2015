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

	private TouchScreenKeyboard keyboard;

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
			entryState = EntryState.LoggedIn;
		} 
		else 
		{
			entryState = EntryState.Normal;
		}

		switch(entryState)
		{
			case EntryState.LoggedIn:
				Writer.WriteTextInstant ("Logged in as " + ParseUser.CurrentUser.Username + "\n"+
				                         "[Tap] to play\n" +
				                         "[Hold] to logout\n");
				break;
			case EntryState.Normal:
				Writer.WriteTextInstant ("[Tap] to play offline\n" +
				                         "[Hold] to login or signup\n");
				break;
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
				ParseUser.LogOut();
				Debug.Log (ParseUser.CurrentUser);
				break;
			case EntryState.Normal:
				StartCoroutine (GoToLevelCoroutine(LoginLevelName));
			break;
		}
	}

	/// <summary>
	/// Waits for a short time and then loads the specified level
	/// </summary>
	private IEnumerator GoToLevelCoroutine(string levelName)
	{
		SetActive (false);

		//yield return new WaitForSeconds(OutroFadeDuration);
		
		if (!string.IsNullOrEmpty(levelName))
			Application.LoadLevel(levelName);
		
		GameManager.Instance.Reset ();
		
		yield return null;
	}
}