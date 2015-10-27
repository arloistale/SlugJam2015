using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Parse;
using UnityEngine;

using UnityEngine.EventSystems;

using UnityEngine.UI;

public class OptionsController : Controller, InputManager.InputListener
{
	#region Enums


	private enum OptionsState
	{
		ChoiceSelection,
		ChangeUsername,
		ChangePassword,
		Syncing,
		Error
	}

	private enum ErrorType
	{
		Unknown,
		ParseInternal,
		ParseException
	}


	#endregion

	// const data
	private const int SELECTION_CHANGE_USERNAME = 0;
	private const int SELECTION_CHANGE_PASSWORD = 1;
	private const int SELECTION_LOGOUT = 2;
	private const int SELECTION_RETURN = 3;

	// modular data
	public TypeWriter SelectionWriter;
	public TypeWriter HeaderWriter;
	public TypeWriter AsyncWriter;

	// ui data
	public CanvasGroup UsernameCanvasGroup;
	public CanvasGroup PasswordCanvasGroup;
	public InputField UsernameField;
	public InputField PasswordField;

	// internal data
	private SelectionHandler selectionHandler;
	private OptionsListener optionsListener;

	private ErrorType currentErrorType;
	private ParseException.ErrorCode currentErrorCode;
	
	private OptionsState optionsState;

	protected override void Awake()
	{
		base.Awake ();
		selectionHandler = new SelectionHandler (new List<string> () {"Change Username", "Change Password", "Logout: " + ParseUser.CurrentUser.Username, "Main Menu"});
		isActive = false;
	}

	public void Activate()
	{
		isActive = true;
		
		InputManager.Instance.SetInputListener (this);

		PromptOptions ();
		
		if(optionsListener != null) optionsListener.OnOptionsActivate ();
	}

	public void End()
	{
		isActive = false;
		
		AsyncWriter.ClearWriting ();
		SelectionWriter.ClearWriting ();
		HeaderWriter.ClearWriting ();
		
		if(optionsListener != null) optionsListener.OnOptionsEnd ();
	}
	
	public void OnTouchBegin()
	{
		if (!isActive)
			return;
	}
	
	public void OnTap()
	{
		if (!isActive)
			return;

		switch(optionsState)
		{
			case OptionsState.ChoiceSelection:
				selectionHandler.Next ();
				PromptOptions ();
				break;
			case OptionsState.ChangeUsername:
				if(!TouchScreenKeyboard.visible)
					SubmitChangeUsername(UsernameField.text);
				break;
			case OptionsState.ChangePassword:
				if(!TouchScreenKeyboard.visible)
					SubmitChangePassword(PasswordField.text);
				break;
			case OptionsState.Error:
				PromptOptions();
				break;
		}
	}
	
	public void OnHold()
	{
		if (!isActive)
			return;

		switch(selectionHandler.GetSelectedIndex())
		{
			case SELECTION_CHANGE_USERNAME:
				PromptChangeUsername();
				break;
			case SELECTION_CHANGE_PASSWORD:
				PromptChangePassword();
				break;
			case SELECTION_LOGOUT:
				Logout();
				break;
			case SELECTION_RETURN:
				End();
				break;
		}
	}
	
	public void SetListener(OptionsListener listener)
	{
		optionsListener = listener;
	}

	private void PromptOptions()
	{
		optionsState = OptionsState.ChoiceSelection;

		AsyncWriter.ClearWriting();

		string choiceMessage = "[Tap] to cycle \n[Hold] to select\n-----------------\n" + selectionHandler.GetOptionListString();
		
		SelectionWriter.WriteTextInstant (choiceMessage);
	}

	private void PromptChangeUsername()
	{
		SelectionWriter.ClearWriting ();
		AsyncWriter.ClearWriting();
		
		optionsState = OptionsState.ChangeUsername;
		
		HeaderWriter.WriteTextInstant ("[Tap] to change\n" +
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

	private void PromptChangePassword()
	{
		SelectionWriter.ClearWriting ();
		AsyncWriter.ClearWriting ();
		
		optionsState = OptionsState.ChangePassword;
		
		HeaderWriter.WriteTextInstant ("[Tap] to change\n" +
		                               "[Hold] to cancel\n" +
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

	private void Logout()
	{
		ParseUser.LogOutAsync();
		Application.LoadLevel ("Bootstrap");
	}

	private void SubmitChangeUsername(string usernameStr)
	{
		if (usernameStr.Length == 0) 
		{
			PromptChangeUsername();
			return;
		}

		optionsState = OptionsState.Syncing;

		UsernameCanvasGroup.alpha = 0;
		UsernameCanvasGroup.blocksRaycasts = false;
		UsernameCanvasGroup.interactable = false;

		HeaderWriter.ClearWriting ();
		AsyncWriter.RepeatText ("...");

		StartCoroutine (ChangeUsernameCoroutine (usernameStr));
	}
	
	private void SubmitChangePassword(string passwordStr)
	{
		if (passwordStr.Length == 0) 
		{
			PromptChangePassword();
			return;
		}
		
		optionsState = OptionsState.Syncing;
		
		PasswordCanvasGroup.alpha = 0;
		PasswordCanvasGroup.blocksRaycasts = false;
		PasswordCanvasGroup.interactable = false;
		
		HeaderWriter.ClearWriting ();
		AsyncWriter.RepeatText ("...");
		
		StartCoroutine (ChangePasswordCoroutine (passwordStr));
	}

	private IEnumerator ChangeUsernameCoroutine(string usernameStr)
	{
		ParseUser.CurrentUser.Username = usernameStr;
		Task changeTask = ParseUser.CurrentUser.SaveAsync ();
		
		while (!changeTask.IsCompleted) 
		{
			yield return null;
		}
		
		if (changeTask.IsFaulted || changeTask.IsCanceled)
		{
			using (IEnumerator<System.Exception> enumerator = changeTask.Exception.InnerExceptions.GetEnumerator()) 
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
			
			optionsState = OptionsState.Error;
		}

		if (optionsState != OptionsState.Error) 
		{
			AsyncWriter.StopWriting();
			selectionHandler = new SelectionHandler (new List<string> () {"Change Username", "Change Password", "Logout: " + ParseUser.CurrentUser.Username, "Main Menu"});
			PromptOptions();
		} 
		else 
		{
			string errorStr = "Unknown error";
			
			switch (currentErrorType) {
			case ErrorType.ParseInternal:
				errorStr = "Server error";
				break;
			case ErrorType.ParseException:
				if (MessageBook.ParseExceptionMap.ContainsKey (currentErrorCode))
					errorStr = MessageBook.ParseExceptionMap [currentErrorCode];
				else
					errorStr = currentErrorCode + "";
				break;
			}
			
			AsyncWriter.WriteTextInstant (errorStr + "\n" +
				"[Tap] to return\n");
		}
	}

	private IEnumerator ChangePasswordCoroutine(string passwordStr)
	{
		ParseUser.CurrentUser.Password = passwordStr;
		Task changeTask = ParseUser.CurrentUser.SaveAsync ();
		
		while (!changeTask.IsCompleted) 
		{
			yield return null;
		}
		
		if (changeTask.IsFaulted || changeTask.IsCanceled)
		{
			using (IEnumerator<System.Exception> enumerator = changeTask.Exception.InnerExceptions.GetEnumerator()) 
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
			
			optionsState = OptionsState.Error;
		}

		if (optionsState != OptionsState.Error) 
		{
			AsyncWriter.StopWriting();
			selectionHandler = new SelectionHandler (new List<string> () {"Change Username", "Change Password", "Logout: " + ParseUser.CurrentUser.Username, "Main Menu"});
			PromptOptions();
		} 
		else 
		{
			string errorStr = "Unknown error";
			
			switch (currentErrorType) {
			case ErrorType.ParseInternal:
				errorStr = "Server error";
				break;
			case ErrorType.ParseException:
				if (MessageBook.ParseExceptionMap.ContainsKey (currentErrorCode))
					errorStr = MessageBook.ParseExceptionMap [currentErrorCode];
				else
					errorStr = currentErrorCode + "";
				break;
			}
			
			AsyncWriter.WriteTextInstant (errorStr + "\n" +
				"[Tap] to return\n");
		}
	}

	public interface OptionsListener
	{
		void OnOptionsActivate();
		void OnOptionsEnd();
	}
}
