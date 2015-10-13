using UnityEngine;
using System;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

using System.Threading.Tasks;

using Parse;

public class MainController : Controller, InputManager.InputListener
{
	private enum MainState
	{
		Intro,
		Instructions,
		Game,
		End,
		SyncError
	}

	private enum ErrorType
	{
		Unknown,
		ParseInternal,
		ParseException
	}

	// leaderboad data
	public LeaderboardController LeaderboardController;

	// type data
	public TypeWriter Writer;
	public Phrase[] Phrases = new Phrase[] {};
	public AudioClip errorSound;
	public AudioClip successSound;

	// internal data
	private MainState lastState;
	private MainState mainState;
	private ErrorType currentErrorType;
	private ParseException.ErrorCode currentErrorCode;

	private bool isSyncing;

	private bool didTap;

	protected override void Awake()
	{
		base.Awake ();

		InputManager.Instance.SetInputListener (this);
	}

	private void Start()
	{
		// we always store the highscore in player prefs even when online
		// here are the steps
		// we first make sure we're online, then if we're online we check to make sure the
		// locally cached user ID is the same as the current one
		// if they're not the same that means a different account is logged in compared to the last cached one
		// so we change the locally cached score to whatever the current user's score is
		// and we also update the locally cached user ID to the new logged in user
		//
		// with this once a user logs in online then plays offline somehow the score
		// will still be saved locally even offline so the user can still progress offline
		// and the score will persist when the user logs back in
		// but when a new account logs in the local score will be set to the new users score
		int prefsOverallStreak = PlayerPrefs.GetInt (ParseUserUtils.KEY_STREAK, 0);

		if (GameManager.Instance.IsOnline)
		{
			if(PlayerPrefs.GetString(ParseUserUtils.KEY_CACHED_ID) != ParseUser.CurrentUser.ObjectId)
			{
				int userOverallStreak = ParseUser.CurrentUser.Get<int> (ParseUserUtils.KEY_STREAK);
				prefsOverallStreak = userOverallStreak;
				PlayerPrefs.SetInt(ParseUserUtils.KEY_STREAK, prefsOverallStreak);
				PlayerPrefs.SetString(ParseUserUtils.KEY_CACHED_ID, ParseUser.CurrentUser.ObjectId);
				PlayerPrefs.Save();
			}
			// if the user is the same as the before then the local score should be aligned
			// with the user score
		}

		GameManager.Instance.SetPointsThreshold(prefsOverallStreak);
		
		GameManager.Instance.SetPoints (0);

		PromptIntro ();
	}

	public void OnTouchBegin()
	{
		if (!isActive && !isSyncing && !LeaderboardController.IsActive())
			return;

		if (Writer.GetMode () == TypeWriter.WriterMode.CullSpaces && Writer.isWriting) 
		{
			Writer.SetTextStatusColor (TypeWriter.TypeStatus.Success);
			Writer.AddSpace ();
		}
	}

	public void OnTap()
	{
		if (!isActive && !isSyncing && !LeaderboardController.IsActive())
			return;

		didTap = true;

		switch (mainState) 
		{
		case MainState.Intro:
			PromptInstructions();
			break;
		case MainState.End:
			PromptGame();
			break;
		case MainState.SyncError:
			if(lastState == MainState.End)
				PromptEnd();
			else
				PromptIntro();
			break;
		}
	}

	public void OnHold()
	{
		if (!isActive && !isSyncing && !LeaderboardController.IsActive())
			return;

		switch (mainState) 
		{
		case MainState.Intro:
			StartCoroutine(LeaderboardCoroutine());
			break;
		case MainState.End:
			StartCoroutine(LeaderboardCoroutine());
			break;
		}
	}

	private void PromptIntro()
	{
		lastState = mainState;
		mainState = MainState.Intro;

		string greetingMessage = (GameManager.Instance.PointsThreshold > 0) ? 
			("Highest: " + GameManager.Instance.PointsThreshold) : ("Hello.");

		Writer.WriteTextInstant (greetingMessage + "\n[Tap] to continue\n[Hold] for leaderboard");
	}

	private void PromptInstructions()
	{
		lastState = mainState;
		mainState = MainState.Instructions;

		Writer.WriteTextInstant (MessageBook.InstructionsMessage);

		StartCoroutine (InstructionsCoroutine ());
	}

	private void PromptGame()
	{
		lastState = mainState;
		mainState = MainState.Game;

		StartCoroutine (GameCoroutine ());
	}

	private void PromptEnd()
	{
		lastState = mainState;
		mainState = MainState.End;
		
		Writer.WriteTextInstant("Streak: " + GameManager.Instance.Points +
		                        "\nHighest: " + GameManager.Instance.PointsThreshold + 
		                        "\n[Tap] to retry" +
		                        "\n[Hold] for leaderboard");
		
		GameManager.Instance.SetPoints(0);
	}

	private IEnumerator InstructionsCoroutine()
	{
		yield return StartCoroutine (WaitForSecondsOrTap (4f));
		
		PromptGame ();
	}

	private IEnumerator GameCoroutine()
	{
		while (true) 
		{
			// get a random phrase and generate a raw message from the phrase
			int phraseIndex = (GameManager.Instance.PointsThreshold > 0) ? UnityEngine.Random.Range (1, Phrases.Length) : 0;
			Phrase randomPhrase = Phrases[phraseIndex];
			string rawMessage = Regex.Replace(randomPhrase.correctMessage, @"\s+", "");
			string correctMessage = randomPhrase.correctMessage;
			int wordCount = randomPhrase.correctMessage.Split(' ').Length;

			Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_SHORT);
			Writer.SetMode(TypeWriter.WriterMode.Normal);
			Writer.WriteTextInstant (rawMessage + "\n" + 
			                  wordCount + " words");

			yield return StartCoroutine(WaitForSecondsOrTap(3f));
		
			Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_MEDIUM);
			
			// start writing raw message
			Writer.SetMode(TypeWriter.WriterMode.CullSpaces);
			Writer.WriteText (correctMessage);

			bool writeResult = true;

			// here we check the written message against the correct message
			while(Writer.GetMode() == TypeWriter.WriterMode.CullSpaces && Writer.isWriting)
			{
				//Writer.setTextToStatusColor(1);
				string writtenText = Writer.GetWrittenText();

				if(writtenText == correctMessage)
				{
					break;
				}

				if(writtenText != correctMessage.Substring(0, Mathf.Min(correctMessage.Length, writtenText.Length)))
				{
					if(errorSound != null)
						SoundManager.Instance.PlaySound(errorSound, transform.position);

					Writer.SetTextStatusColor(TypeWriter.TypeStatus.Error);
					Writer.StopWriting();
					writeResult = false;
				}
				
				yield return null;
			}

			yield return new WaitForSeconds(1f);

			Writer.SetTextStatusColor(TypeWriter.TypeStatus.Normal);
			Writer.SetTypeDuration(TypeWriter.TYPE_DURATION_SHORT);
			Writer.SetMode(TypeWriter.WriterMode.Normal);

			if(writeResult)
			{
				if(successSound != null)
					SoundManager.Instance.PlaySoundModulated(successSound, transform.position);
				
				GameManager.Instance.AddPoints(1);

				if(GameManager.Instance.Points > GameManager.Instance.PointsThreshold) 
				{
					GameManager.Instance.SetPointsThreshold(GameManager.Instance.Points);
					
					PlayerPrefs.SetInt(ParseUserUtils.KEY_STREAK, GameManager.Instance.PointsThreshold);
					PlayerPrefs.Save();
				}

				if(GameManager.Instance.PointsThreshold > 0)
					Writer.WriteTextInstant("Streak: " + GameManager.Instance.Points + "\nHighest: " + GameManager.Instance.PointsThreshold);
				else
					Writer.WriteTextInstant("Streak: " + GameManager.Instance.Points);

				yield return StartCoroutine(WaitForSecondsOrTap(3f));
			}
			else
			{
				PromptEnd();
				break;
			}
		}
	}

	private IEnumerator LeaderboardCoroutine()
	{
		if (ShouldSyncOverall())
		{
			yield return StartCoroutine (SyncCoroutine ());

			if (mainState == MainState.SyncError)
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
				
				Writer.WriteTextInstant("Sync error\n" +
				                        errorStr + "\n" +
				                        "[Tap] to return\n");

				yield break;
			}
		}

		Writer.ClearWriting ();
		LeaderboardController.Activate ();
	}

	private IEnumerator SyncCoroutine()
	{
		isSyncing = true;

		Writer.WriteTextInstant("Syncing...");

		int streakValue = GameManager.Instance.PointsThreshold;

		ParseUser currentUser = ParseUser.CurrentUser;
		currentUser[ParseUserUtils.KEY_STREAK] = streakValue;

		Task saveTask = currentUser.SaveAsync ();

		while (!saveTask.IsCompleted) 
		{
			yield return null;
		}

		if (saveTask.IsFaulted || saveTask.IsCanceled)
		{
			using (IEnumerator<System.Exception> enumerator = saveTask.Exception.InnerExceptions.GetEnumerator()) 
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

			lastState = mainState;
			mainState = MainState.SyncError;
		}

		isSyncing = false;
	}

	private IEnumerator WaitForTap()
	{
		didTap = false;
		
		while (!didTap) 
		{	
			yield return null;
		}
		
		didTap = false;
	}

	private IEnumerator WaitForSecondsOrTap(float duration)
	{
		didTap = false;

		DateTime startTime = System.DateTime.UtcNow;
		TimeSpan waitDuration = TimeSpan.FromSeconds(duration);
		
		while (!didTap) 
		{
			if(System.DateTime.UtcNow - startTime >= waitDuration)
			{
				break;
			}
			
			yield return null;
		}

		didTap = false;
	}

	private bool ShouldSyncOverall()
	{
		return ((GameManager.Instance.IsOnline) ? 
		        (GameManager.Instance.PointsThreshold > ParseUser.CurrentUser.Get<int>(ParseUserUtils.KEY_STREAK)) : false);
	}
}