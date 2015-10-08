using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

using System.Threading.Tasks;
using Parse;

public class LoginController : Controller, InputManager.InputListener
{
	private enum LoginState
	{
		Username,
		Password,
		Authing,
		Registering,
		Error,
		Ready
	}

	private enum ErrorType
	{
		Unknown,
		ParseInternal,
		ParseException,
		LoginCancel
	}

	private Dictionary<ParseException.ErrorCode, string> parseExceptionMap = new Dictionary<ParseException.ErrorCode, string>() 
	{
		{ ParseException.ErrorCode.ConnectionFailed, "Bad connection" },
		{ ParseException.ErrorCode.InternalServerError, "Server error" },
		{ ParseException.ErrorCode.InvalidACL, "Server error" },
		{ ParseException.ErrorCode.InvalidSessionToken, "Invalid session" },
		{ ParseException.ErrorCode.MissingObjectId, "User error" },
		{ ParseException.ErrorCode.NotInitialized, "User error" },
		{ ParseException.ErrorCode.ObjectNotFound, "Wrong credentials" },
		{ ParseException.ErrorCode.OperationForbidden, "Server error" },
		{ ParseException.ErrorCode.OtherCause, "Unknown server error" },
		{ ParseException.ErrorCode.RequestLimitExceeded, "Server limits reached" },
		{ ParseException.ErrorCode.SessionMissing, "User error" },
		{ ParseException.ErrorCode.Timeout, "Bad connection" },
		{ ParseException.ErrorCode.UsernameTaken, "Username already taken" },
		{ ParseException.ErrorCode.ValidationFailed, "Wrong credentials" }
	};

	// type data
	public TypeWriter Writer;
	public TypeWriter AsyncWriter;

	// level data
	public string CancelLevelName;
	public string GoLevelName;

	// input data
	public CanvasGroup UsernameCanvasGroup;
	public CanvasGroup PasswordCanvasGroup;
	public InputField UsernameField;
	public InputField PasswordField;

	// internal
	private LoginState loginState;
	private ErrorType currentErrorType;
	private ParseException.ErrorCode currentErrorCode;

	private string currUsernameStr;

	protected override void Awake()
	{
		base.Awake ();

		InputManager.Instance.SetInputListener (this);
	}

	private void Start()
	{
		PromptUsername ();
	}

	public void OnTouchBegin()
	{
	}

	public void OnTap()
	{
		if (!isActive)
			return;

		switch (loginState)
		{
			case LoginState.Username:
				SubmitUsername(UsernameField.text);
				break;
			case LoginState.Password:
				SubmitPasswordAuth(PasswordField.text);
				break;
			case LoginState.Error:
				PromptUsername ();
				break;
		}
	}
	
	public void OnHold()
	{
		if (!isActive)
			return;

		switch (loginState)
		{
			case LoginState.Username:
				GoToLevel(CancelLevelName);
				break;
			case LoginState.Password:
				SubmitPasswordRegister(PasswordField.text);
				break;
		}
	}

	private void PromptUsername()
	{
		loginState = LoginState.Username;
		Writer.WriteTextInstant ("[Tap] to continue\n" +
		                         "[Hold] to cancel\n" +
		                         "Enter username");

		AsyncWriter.ClearWriting ();
		
		EventSystem.current.SetSelectedGameObject(UsernameField.gameObject, null);
	}
	
	private void PromptPassword()
	{
		AsyncWriter.ClearWriting ();

		loginState = LoginState.Password;
		UsernameCanvasGroup.alpha = 0;
		UsernameCanvasGroup.blocksRaycasts = false;
		UsernameCanvasGroup.interactable = false;
		PasswordCanvasGroup.alpha = 1;
		PasswordCanvasGroup.blocksRaycasts = true;
		PasswordCanvasGroup.interactable = true;
		
		EventSystem.current.SetSelectedGameObject(PasswordField.gameObject, null);
		
		Writer.WriteTextInstant ("Username: " + currUsernameStr + "\n" +
		                         "[Tap] to login\n" +
		                         "[Hold] to signup\n" +
		                         "Enter password");
	}

	private void SubmitUsername(string usernameStr)
	{
		if (usernameStr.Length > 0) 
		{
			currUsernameStr = usernameStr;

			PromptPassword();
		}
	}

	private void SubmitPasswordAuth(string passwordStr)
	{
		loginState = LoginState.Authing;

		PasswordCanvasGroup.alpha = 0;
		UsernameCanvasGroup.blocksRaycasts = false;
		UsernameCanvasGroup.interactable = false;

		Writer.ClearWriting ();
		AsyncWriter.RepeatText ("...");

		ParseUser.LogInAsync (currUsernameStr, passwordStr).ContinueWith (t => {
			if (t.IsFaulted)
			{
				if (t.Exception.InnerExceptions.Count > 0 && t.Exception.InnerExceptions [0] is ParseException) 
				{
					ParseException parseException = (ParseException) t.Exception.InnerExceptions [0];
					currentErrorCode = parseException.Code;
					currentErrorType = ErrorType.ParseException;
				}
				else
				{
					currentErrorType = ErrorType.ParseInternal;
				}
				
				loginState = LoginState.Error;
			}
			else if(t.IsCanceled)
			{
				currentErrorType = ErrorType.LoginCancel;
				loginState = LoginState.Error;
			}
			else
			{
				loginState = LoginState.Ready;
			}
		});

		StartCoroutine (AuthCoroutine ());
	}

	private void SubmitPasswordRegister(string passwordStr)
	{
		loginState = LoginState.Registering;

		ParseUser newUser = new ParseUser ();
		newUser.Username = currUsernameStr;
		newUser.Password = passwordStr;

		PasswordCanvasGroup.alpha = 0;
		UsernameCanvasGroup.blocksRaycasts = false;
		UsernameCanvasGroup.interactable = false;

		Writer.ClearWriting ();
		AsyncWriter.RepeatText ("...");

		newUser.SignUpAsync ().ContinueWith (t => {

			if (t.IsFaulted)
			{
				if (t.Exception.InnerExceptions.Count > 0 && t.Exception.InnerExceptions [0] is ParseException) 
				{
					ParseException parseException = (ParseException)t.Exception.InnerExceptions [0];
					currentErrorCode = parseException.Code;
					currentErrorType = ErrorType.ParseException;
				}
				else
				{
					currentErrorType = ErrorType.ParseInternal;
				}

				loginState = LoginState.Error;
			}
			else if(t.IsCanceled)
			{
				currentErrorType = ErrorType.LoginCancel;
				loginState = LoginState.Error;
			}
			else
			{
				loginState = LoginState.Ready;
			}
		});

		StartCoroutine (AuthCoroutine ());
	}

	private IEnumerator AuthCoroutine()
	{
		while (loginState != LoginState.Ready && loginState != LoginState.Error) 
		{
			yield return null;
		}

		if (loginState == LoginState.Ready)
			GoToLevel (GoLevelName);
		else if (loginState == LoginState.Error)
		{
			string errorStr = "Unknown error";

			switch(currentErrorType)
			{
				case ErrorType.ParseInternal:
					errorStr = "Server error";
					break;
				case ErrorType.ParseException:
					if(parseExceptionMap.ContainsKey(currentErrorCode))
						errorStr = parseExceptionMap[currentErrorCode];
					break;
				case ErrorType.LoginCancel:
					errorStr = "Cancelled";
					break;
			}

			AsyncWriter.WriteTextInstant(errorStr + "\n" +
			                             "[Tap] to return\n");
		}
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
		SetActive (false);

		if (!string.IsNullOrEmpty(levelName))
			Application.LoadLevel(levelName);
		
		GameManager.Instance.Reset ();
		
		yield return null;
	}
}