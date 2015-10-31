using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;

using Parse;

public class EntryController : Controller, InputManager.InputListener
{
	private enum EntryState
	{
		Normal,
		LoggedIn,
		Offline,
		Error
	}

	public TypeWriter Writer;

	public string GoLevelName;
	public string LoginLevelName;

	// internal data
	private EntryState entryState;

	private ErrorInfo errorInfo;

	protected override void Awake() 
	{
		base.Awake ();

		InputManager.Instance.SetInputListener (this);
	}

	private void Start()
	{
		PromptStart ();
	}

	public void OnTouchBegin()
	{
	}

	public void OnTap()
	{
		if (!isActive)
			return;

		switch (entryState) 
		{
		case EntryState.Normal:
			GameManager.Instance.IsOnline = false;
			GoToLevel(GoLevelName);
			break;
		case EntryState.LoggedIn:
			GameManager.Instance.IsOnline = true;
			GoToLevel(GoLevelName);
			break;
		case EntryState.Error:
			PromptStart();
			break;
		}
	}

	public void OnHold()
	{
		if (!isActive)
			return;

		switch (entryState) 
		{
			case EntryState.LoggedIn:
				PlayerPrefs.SetString(ParseUserUtils.KEY_CACHED_ID, "");
				PlayerPrefs.SetInt(ParseUserUtils.KEY_STREAK, 0);
				PlayerPrefs.SetInt (ParseUserUtils.KEY_DAILY_STREAK, 0);
				PlayerPrefs.SetString(ParseUserUtils.KEY_DAILY_TIMESTAMP, System.DateTime.UtcNow.ToBinary().ToString());
				PlayerPrefs.Save();
				ParseUser.LogOutAsync();
				PromptNormal();
				
				break;
			case EntryState.Normal:
				GoToLevel(LoginLevelName);
			break;
		}
	}

	private void PromptStart()
	{
		if (ParseUser.CurrentUser != null) 
		{
			StartCoroutine(FetchCoroutine());
		} 
		else 
		{
			PromptNormal();
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

	private IEnumerator FetchCoroutine()
	{
		Writer.WriteTextInstant ("Fetching...");
		
		Task<ParseUser> fetchTask = ParseUser.CurrentUser.FetchIfNeededAsync ();
		
		while (!fetchTask.IsCompleted) yield return null;
		
		if (fetchTask.IsFaulted || fetchTask.IsCanceled)
		{
			using (IEnumerator<System.Exception> enumerator = fetchTask.Exception.InnerExceptions.GetEnumerator()) 
			{
				if (enumerator.MoveNext()) 
				{
					ParseException exception = (ParseException) enumerator.Current;
					errorInfo = new ErrorInfo(ErrorType.ParseException, exception.Code);
				}
				else
				{
					errorInfo = new ErrorInfo(ErrorType.ParseInternal);
				}
			}
			
			entryState = EntryState.Error;
		}
		
		// we're done, what did we get?
		if (entryState != EntryState.Error) 
		{
			PromptLoggedIn();
		}
		else
		{	
			Writer.WriteTextInstant(errorInfo.GetErrorStr() + "\n" +
			                        "[Tap] to refresh\n");
		}
	}
}