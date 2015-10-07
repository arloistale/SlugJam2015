using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

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
		Ready
	}

	// type data
	public TypeWriter Writer;

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
		submitEventPassword.AddListener (SubmitPasswordRegister);
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
		
		EventSystem.current.SetSelectedGameObject(UsernameField.gameObject, null);
	}
	
	private void PromptPassword()
	{
		loginState = LoginState.Password;
		UsernameCanvasGroup.alpha = 0;
		UsernameCanvasGroup.blocksRaycasts = false;
		UsernameCanvasGroup.interactable = false;
		PasswordCanvasGroup.alpha = 1;
		PasswordCanvasGroup.blocksRaycasts = true;
		PasswordCanvasGroup.interactable = true;
		
		EventSystem.current.SetSelectedGameObject(PasswordField.gameObject, null);
		
		Writer.WriteTextInstant (currUsernameStr + "\n" +
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

		ParseUser.LogInAsync (currUsernameStr, passwordStr).ContinueWith (t => {
			Debug.Log (t.Exception.InnerExceptions.Count);
			if(t.IsFaulted || t.IsCanceled)
			{
				if(t.Exception.InnerExceptions.Count > 0 && t.Exception.InnerExceptions[0] is ParseException)
				{
					ParseException parseException = (ParseException) t.Exception.InnerExceptions[0];
					switch(parseException.Code)
					{
						case ParseException.ErrorCode.ConnectionFailed:
						case ParseException.ErrorCode.InternalServerError:
						case ParseException.ErrorCode.Timeout:
							// there was a general connection problem
							Debug.Log ("There was a problem!");
							break;
						case ParseException.ErrorCode.ValidationFailed:
							Debug.Log ("Validation failed");
							break;
						default:
							Debug.Log (parseException.Message);
							break;
					}
				}
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

		newUser.SignUpAsync ().ContinueWith (t => {
			if (t.IsFaulted || t.IsCanceled)
			{
				if (t.Exception.InnerExceptions.Count > 0 && t.Exception.InnerExceptions [0] is ParseException) {
					ParseException parseException = (ParseException)t.Exception.InnerExceptions [0];
					switch (parseException.Code) {
					case ParseException.ErrorCode.ConnectionFailed:
					case ParseException.ErrorCode.InternalServerError:
					case ParseException.ErrorCode.Timeout:
						// there was a general connection problem
						Debug.Log ("There was a problem!");
						break;
					case ParseException.ErrorCode.ValidationFailed:
						Debug.Log ("Validation failed");
						break;
					default:
						Debug.Log (parseException.Message);
						break;
					}
				}
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
		while (loginState != LoginState.Ready) 
		{
			yield return null;
		}

		GoToLevel (GoLevelName);
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