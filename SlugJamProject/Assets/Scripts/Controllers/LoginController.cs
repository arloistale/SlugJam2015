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
		Connection,
		LoginCancel,
		LoginValidation
	}
	
	private static Dictionary<ErrorType, string> errorMap = new Dictionary<ErrorType, string>() 
	{
		{ ErrorType.Unknown, "Unknown error" },
		{ ErrorType.ParseInternal, "Server error" },
		{ ErrorType.Connection, "Bad connection" },
		{ ErrorType.LoginCancel, "Cancelled" },
		{ ErrorType.LoginValidation, "Wrong credentials" }
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
	private ParseException currentUnknownException;

	private string currUsernameStr;

	protected override void Awake()
	{
		base.Awake ();

		InputManager.Instance.SetInputListener (this);
	}

	private void Start()
	{
		InputField.SubmitEvent submitEventUsername = new InputField.SubmitEvent ();
		submitEventUsername.AddListener (SubmitUsername);
		UsernameField.onEndEdit = submitEventUsername;

		InputField.SubmitEvent submitEventPassword = new InputField.SubmitEvent ();
		submitEventPassword.AddListener (SubmitPasswordAuth);
		PasswordField.onEndEdit = submitEventPassword;

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
		AsyncWriter.WriteTextInstant (currUsernameStr + " | " + passwordStr);

		ParseUser.LogInAsync (currUsernameStr, passwordStr).ContinueWith (t => {
			if (t.IsFaulted)
			{
				if (t.Exception.InnerExceptions.Count > 0 && t.Exception.InnerExceptions [0] is ParseException) 
				{
					ParseException parseException = (ParseException) t.Exception.InnerExceptions [0];
					switch (parseException.Code) 
					{
						case ParseException.ErrorCode.ConnectionFailed:
						case ParseException.ErrorCode.Timeout:
							currentErrorType = ErrorType.Connection;
							break;
						case ParseException.ErrorCode.InternalServerError:
							currentErrorType = ErrorType.ParseInternal;
							break;
						case ParseException.ErrorCode.ValidationFailed:
							currentErrorType = ErrorType.LoginValidation;
							break;
						default:
							currentErrorType = ErrorType.Unknown;
							currentUnknownException = parseException;
							break;
					}
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
					switch (parseException.Code) 
					{
					case ParseException.ErrorCode.ConnectionFailed:
					case ParseException.ErrorCode.Timeout:
						currentErrorType = ErrorType.Connection;
						break;
					case ParseException.ErrorCode.InternalServerError:
						currentErrorType = ErrorType.ParseInternal;
						break;
					case ParseException.ErrorCode.ValidationFailed:
						currentErrorType = ErrorType.LoginValidation;
						break;
					default:
						currentErrorType = ErrorType.Unknown;
						currentUnknownException = parseException;
						break;
					}
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
			if(currentErrorType != ErrorType.Unknown)
			{
				AsyncWriter.WriteTextInstant(errorMap[currentErrorType] + "\n" +
				                             "[Tap] to return\n");
			}
			else
			{
				AsyncWriter.WriteTextInstant(errorMap[currentErrorType] + "\n" + 
				                             currentUnknownException.Code + ": " + currentUnknownException.Message + "\n" +
				                             "[Tap] to return\n");
			}
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