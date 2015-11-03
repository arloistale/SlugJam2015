using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parse;
using UnityEngine;

using UnityEngine.EventSystems;

using UnityEngine.UI;

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
	private ErrorInfo errorInfo;

	private string currUsernameStr;
	
	protected override void Awake()
	{
		base.Awake ();
		
		InputManager.Instance.SetInputListener (this);
	}
	
	private void Start()
	{
		StartCoroutine(PromptCoroutine ());
	}
	
	private IEnumerator PromptCoroutine()
	{
		yield return null;
		
		PromptUsername();
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
		
		currUsernameStr = "";
		UsernameField.text = "";
		
		UsernameField.ActivateInputField();
		UsernameField.Select();
	}
	
	private void PromptPassword()
	{
		AsyncWriter.ClearWriting ();
		
		loginState = LoginState.Password;
		
		Writer.WriteTextInstant ("[Tap] to login\n" +
		                         "[Hold] to signup\n" +
		                         "Enter password");
		
		UsernameCanvasGroup.alpha = 0;
		UsernameCanvasGroup.blocksRaycasts = false;
		UsernameCanvasGroup.interactable = false;
		PasswordCanvasGroup.alpha = 1;
		PasswordCanvasGroup.blocksRaycasts = true;
		PasswordCanvasGroup.interactable = true;
		
		PasswordField.text = "";
		
		PasswordField.ActivateInputField();
		PasswordField.Select();
	}
	
	private void SubmitUsername(string usernameStr)
	{
		if (usernameStr.Length == 0) 
		{
			PromptUsername();
			return;
		}
		
		currUsernameStr = usernameStr;
		PromptPassword();
	}
	
	private void SubmitPasswordAuth(string passwordStr)
	{
		if (currUsernameStr.Length == 0 || passwordStr.Length == 0) 
		{
			PromptPassword();
			return;
		}
		
		loginState = LoginState.Authing;
		
		PasswordCanvasGroup.alpha = 0;
		PasswordCanvasGroup.blocksRaycasts = false;
		PasswordCanvasGroup.interactable = false;
		
		Writer.ClearWriting ();
		AsyncWriter.RepeatText ("...");
		
		StartCoroutine (AuthCoroutine (currUsernameStr, passwordStr));
	}
	
	private void SubmitPasswordRegister(string passwordStr)
	{
		if (currUsernameStr.Length == 0 || passwordStr.Length == 0) 
		{
			PromptPassword();
			return;
		}
		
		loginState = LoginState.Registering;
		
		PasswordCanvasGroup.alpha = 0;
		PasswordCanvasGroup.blocksRaycasts = false;
		PasswordCanvasGroup.interactable = false;
		
		Writer.ClearWriting ();
		AsyncWriter.RepeatText ("...");
		
		StartCoroutine (RegisterCoroutine (currUsernameStr, passwordStr));
	}
	
	private IEnumerator AuthCoroutine(string usernameStr, string passwordStr)
	{
		Task<ParseUser> authTask = ParseUser.LogInAsync (usernameStr, passwordStr);
		
		DateTime startTime = DateTime.UtcNow;
		TimeSpan waitDuration = TimeSpan.FromSeconds(TimeUtils.TIMEOUT_DURATION);
		while (!authTask.IsCompleted) 
		{
			if(DateTime.UtcNow - startTime >= waitDuration) 
				break;
			
			yield return null;
		}
		
		if(!authTask.IsCompleted)
		{
			errorInfo = new ErrorInfo(ErrorType.Timeout);
			loginState = LoginState.Error;
		}
		else if (authTask.IsFaulted)
		{
			using (IEnumerator<System.Exception> enumerator = authTask.Exception.InnerExceptions.GetEnumerator()) 
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
			
			loginState = LoginState.Error;
		} else {
			loginState = LoginState.Ready;
		}
		
		// we're done, what did we get?
		if (loginState == LoginState.Ready) 
		{
			GameManager.Instance.IsOnline = true;
			GoToLevel (GoLevelName);
		}
		else if (loginState == LoginState.Error)
		{
			AsyncWriter.WriteTextInstant(errorInfo.GetErrorStr() + "\n" +
			                             "[Tap] to return\n");
		}
	}
	
	private IEnumerator RegisterCoroutine(string usernameStr, string passwordStr)
	{
		IDictionary<string, object> userInfo = new Dictionary<string, object>
		{
			{ "username", usernameStr },
			{ "password", passwordStr }
		};
		Task<IDictionary<string, object>> registerTask = 
			ParseCloud.CallFunctionAsync<IDictionary<string, object>> ("SignUpCloud", userInfo);
		
		DateTime startTime = DateTime.UtcNow;
		TimeSpan waitDuration = TimeSpan.FromSeconds(TimeUtils.TIMEOUT_DURATION);
		while (!registerTask.IsCompleted) 
		{
			if(DateTime.UtcNow - startTime >= waitDuration) 
				break;
			
			yield return null;
		}
		
		if(!registerTask.IsCompleted)
		{
			errorInfo = new ErrorInfo(ErrorType.Timeout);
			loginState = LoginState.Error;
		}
		else if (registerTask.IsFaulted)
		{
			using (IEnumerator<System.Exception> enumerator = registerTask.Exception.InnerExceptions.GetEnumerator()) 
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
			
			loginState = LoginState.Error;
		}
		else
		{
			IDictionary<string, object> result = registerTask.Result;
			// Hack, check for errors
			object code;
			if (result.TryGetValue("code", out code)) 
			{
				int errorCodeInt = Convert.ToInt32(code);
				errorInfo = new ErrorInfo(ErrorType.ParseException, (ParseException.ErrorCode) errorCodeInt);
				loginState = LoginState.Error;
			}
			else 
			{
				// we need to sync the local instance of the current user with the one that was just logged in
				// based on the session token we are given
				object token;
				if(result.TryGetValue("token", out token))
				{
					Task<ParseUser> becomeTask = ParseUser.BecomeAsync((string) token);
					
					while(!becomeTask.IsCompleted)
					{
						yield return null;
					}
					
					if (becomeTask.IsFaulted || becomeTask.IsCanceled)
					{
						using (IEnumerator<System.Exception> enumerator = becomeTask.Exception.InnerExceptions.GetEnumerator()) 
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
						
						loginState = LoginState.Error;
					} 
					else 
					{
						loginState = LoginState.Ready;
					}
				}
			}
		}
		
		// we're done, what did we get?
		if (loginState == LoginState.Ready) 
		{
			GameManager.Instance.IsOnline = true;
			GoToLevel (GoLevelName);
		}
		else if (loginState == LoginState.Error)
		{
			AsyncWriter.WriteTextInstant(errorInfo.GetErrorStr() + "\n" +
			                             "[Tap] to return\n");
		}
	}
}