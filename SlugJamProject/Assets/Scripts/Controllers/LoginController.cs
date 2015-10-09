using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System;

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
		ParseException
	}

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
		AsyncWriter.ClearWriting ();

		loginState = LoginState.Username;

		Writer.WriteTextInstant ("[Tap] to continue\n" +
		                         "[Hold] to cancel\n" +
		                         "Enter username");

		PasswordCanvasGroup.alpha = 0;
		PasswordCanvasGroup.blocksRaycasts = false;
		PasswordCanvasGroup.interactable = false;
		UsernameCanvasGroup.alpha = 1;
		UsernameCanvasGroup.blocksRaycasts = true;
		UsernameCanvasGroup.interactable = true;

		UsernameField.text = "";
		
		EventSystem.current.SetSelectedGameObject(UsernameField.gameObject, null);
	}
	
	private void PromptPassword()
	{
		AsyncWriter.ClearWriting ();

		loginState = LoginState.Password;

		Writer.WriteTextInstant ("Username: " + currUsernameStr + "\n" +
		                         "[Tap] to login\n" +
		                         "[Hold] to signup\n" +
		                         "Enter password");

		UsernameCanvasGroup.alpha = 0;
		UsernameCanvasGroup.blocksRaycasts = false;
		UsernameCanvasGroup.interactable = false;
		PasswordCanvasGroup.alpha = 1;
		PasswordCanvasGroup.blocksRaycasts = true;
		PasswordCanvasGroup.interactable = true;

		PasswordField.text = "";

		EventSystem.current.SetSelectedGameObject(PasswordField.gameObject, null);
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
		PasswordCanvasGroup.blocksRaycasts = false;
		PasswordCanvasGroup.interactable = false;

		Writer.ClearWriting ();
		AsyncWriter.RepeatText ("...");

		ParseUser.LogInAsync (currUsernameStr, passwordStr).ContinueWith (t => 
		{
			if (t.IsFaulted || t.IsCanceled)
			{
				using (IEnumerator<System.Exception> enumerator = t.Exception.InnerExceptions.GetEnumerator()) 
				{
					if (enumerator.MoveNext()) 
					{
						ParseException exception = (ParseException) enumerator.Current;
						currentErrorCode = exception.Code;
						currentErrorType = ErrorType.ParseException;
					}
					else
					{
						currentErrorType = ErrorType.ParseInternal;
					}
				}
				
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

		PasswordCanvasGroup.alpha = 0;
		PasswordCanvasGroup.blocksRaycasts = false;
		PasswordCanvasGroup.interactable = false;

		Writer.ClearWriting ();
		AsyncWriter.RepeatText ("...");

		IDictionary<string, object> userInfo = new Dictionary<string, object>
		{
			{ "username", currUsernameStr },
			{ "password", passwordStr }
		};
		ParseCloud.CallFunctionAsync<IDictionary<string, object>>("SignUpCloud", userInfo).ContinueWith(t =>
		{
			if (t.IsFaulted || t.IsCanceled)
			{
				using (IEnumerator<System.Exception> enumerator = t.Exception.InnerExceptions.GetEnumerator()) 
				{
					if (enumerator.MoveNext()) 
					{
						ParseException exception = (ParseException) enumerator.Current;
						currentErrorCode = exception.Code;
						currentErrorType = ErrorType.ParseException;
					}
					else
					{
						currentErrorType = ErrorType.ParseInternal;
					}
				}

				loginState = LoginState.Error;
			} else {
				IDictionary<string, object> result = t.Result;
				// Hack, check for errors
				object code;
				if (result.TryGetValue("code", out code)) 
				{
					int errorCodeInt = Convert.ToInt32(code);
					currentErrorCode = (ParseException.ErrorCode) errorCodeInt;
					currentErrorType = ErrorType.ParseException;
					loginState = LoginState.Error;
				}
				else 
				{
					object token;
					if(result.TryGetValue("token", out token))
					{
						ParseUser.BecomeAsync((string) token).ContinueWith(ti => 
						{
							if (ti.IsFaulted || ti.IsCanceled)
							{
								using (IEnumerator<System.Exception> enumerator = ti.Exception.InnerExceptions.GetEnumerator()) 
								{
									if (enumerator.MoveNext()) 
									{
										ParseException exception = (ParseException) enumerator.Current;
										currentErrorCode = exception.Code;
										currentErrorType = ErrorType.ParseException;
									}
									else
									{
										currentErrorType = ErrorType.ParseInternal;
									}
								}
								
								loginState = LoginState.Error;
							} 
							else 
							{
								loginState = LoginState.Ready;
							}
						});
					}
				}
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
					if(MessageBook.ParseExceptionMap.ContainsKey(currentErrorCode))
						errorStr = MessageBook.ParseExceptionMap[currentErrorCode];
					else
						errorStr = currentErrorCode + "";
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