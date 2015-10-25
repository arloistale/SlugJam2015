using UnityEngine;
using System;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

using System.Threading.Tasks;

using Parse;

public class MainController : Controller, InputManager.InputListener, LeaderboardController.LeaderboardListener
{
	private enum MainState
	{
		Intro,
		Instructions,
		Game,
		End,
		SyncError,
		Options
	}
	
	private enum ErrorType
	{
		Unknown,
		ParseInternal,
		ParseException
	}
	
	// data data
	public PhraseBook PhraseBook;
	public TierBook TierBook;
	
	// modular data
	public LeaderboardController LeaderboardController;
	public SharingHandler SharingHandler;
	
	// type data
	public TypeWriter Writer;
	public AudioClip errorSound;
	public AudioClip successSound;
	
	// internal data
	private int SCORE_MAX = 8192;
	
	private MainState lastState;
	private MainState mainState;
	private ErrorType currentErrorType;
	private OptionsHandler OptionsHandler;
	private ParseException.ErrorCode currentErrorCode;
	
	private bool isSyncing;
	
	private bool didTap;
	
	protected override void Awake()
	{
		base.Awake ();
		
		InputManager.Instance.SetInputListener (this);
		LeaderboardController.SetListener (this);
		
		OptionsHandler = new OptionsHandler (new List<string> () {"Continue", "Leaderboard", "Options"});
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
		int prefsStreak = PlayerPrefs.GetInt (ParseUserUtils.KEY_STREAK, 0);
		int prefsDailyStreak = PlayerPrefs.GetInt (ParseUserUtils.KEY_DAILY_STREAK, 0);
		string prefsDailyTimestampRaw = PlayerPrefs.GetString (ParseUserUtils.KEY_DAILY_TIMESTAMP, "");
		DateTime prefsDailyTimestamp = (prefsDailyTimestampRaw.Length > 0) ? 
			DateTime.FromBinary (Convert.ToInt64 (prefsDailyTimestampRaw)) : DateTime.UtcNow;
		
		if (GameManager.Instance.IsOnline)
		{
			int userStreak = ParseUser.CurrentUser.Get<int> (ParseUserUtils.KEY_STREAK);
			int userDailyStreak = ParseUser.CurrentUser.Get<int>(ParseUserUtils.KEY_DAILY_STREAK);
			DateTime userDailyTimestamp = ParseUser.CurrentUser.Get<DateTime>(ParseUserUtils.KEY_DAILY_TIMESTAMP).ToUniversalTime();
			if(PlayerPrefs.GetString(ParseUserUtils.KEY_CACHED_ID) != ParseUser.CurrentUser.ObjectId)
			{
				prefsStreak = userStreak;
				prefsDailyStreak = userDailyStreak;
				prefsDailyTimestamp = userDailyTimestamp;
				
				PlayerPrefs.SetString(ParseUserUtils.KEY_CACHED_ID, ParseUser.CurrentUser.ObjectId);
				PlayerPrefs.SetInt(ParseUserUtils.KEY_STREAK, prefsStreak);
				PlayerPrefs.SetInt (ParseUserUtils.KEY_DAILY_STREAK, prefsDailyStreak);
				PlayerPrefs.SetString(ParseUserUtils.KEY_DAILY_TIMESTAMP, prefsDailyTimestamp.ToBinary().ToString());
				PlayerPrefs.Save();
			}
			else
			{
				if(userStreak > prefsStreak)
				{
					prefsStreak = userStreak;
					PlayerPrefs.SetInt(ParseUserUtils.KEY_STREAK, prefsStreak);
					PlayerPrefs.Save();
				}
				
				if(userDailyStreak > prefsStreak)
				{
					prefsDailyStreak = userDailyStreak;
					PlayerPrefs.SetInt(ParseUserUtils.KEY_DAILY_STREAK, prefsDailyStreak);
					PlayerPrefs.Save();
				}
				
				if(userDailyTimestamp > prefsDailyTimestamp)
				{
					prefsDailyTimestamp = userDailyTimestamp;
					PlayerPrefs.SetString(ParseUserUtils.KEY_DAILY_TIMESTAMP, prefsDailyTimestamp.ToBinary().ToString());
					PlayerPrefs.Save();
				}
			}
			// if the user is the same as the before then the local score should be aligned
			// with the user score
		}
		
		GameManager.Instance.DailyStreak = prefsDailyStreak;
		GameManager.Instance.DailyTimestamp = prefsDailyTimestamp;
		GameManager.Instance.HighStreak = prefsStreak;
		GameManager.Instance.Streak = 0;
		
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
		if (!isActive)
			return;
		
		if(isSyncing || LeaderboardController.IsActive() || SharingHandler.isSharing)
			return;
		
		didTap = true;
		
		switch (mainState) 
		{
		case MainState.Intro:
			OptionsHandler.Next();
			
			if(GameManager.Instance.IsOnline)
				PromptIntro();
			else
				PromptGame();
			
			break;
		case MainState.Options:
			OptionsHandler.Next();
			PromptOptions();
			break;
		case MainState.End:
			PromptIntro();
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
		if (!isActive)
			return;
		
		if(isSyncing || LeaderboardController.IsActive() || SharingHandler.isSharing)
			return;
		
		switch (mainState) 
		{
		case MainState.Intro:
			if(GameManager.Instance.IsOnline){
				if(OptionsHandler.GetSelectedIndex() == 0){
					PromptGame();
				}
				else if(OptionsHandler.GetSelectedIndex() == 1){
					StartCoroutine(LeaderboardCoroutine());
				}
				else if(OptionsHandler.GetSelectedIndex() == 2){
					OptionsHandler = new OptionsHandler (new List<string> () {"Change UserName", "Change Password", "Logout " + ParseUser.CurrentUser.Username, "Main Menu"});
					PromptOptions();
				}
			}
			else{
				Application.LoadLevel ("Bootstrap");
			}
			
			break;
		case MainState.Options:
			if(OptionsHandler.GetSelectedIndex() == 0){
				print("username change");
			}
			else if(OptionsHandler.GetSelectedIndex() == 1){
				print("password change");
			}
			else if(OptionsHandler.GetSelectedIndex() == 2){
				//logout here don't force them to logout on next page
				Application.LoadLevel ("Bootstrap");
			}
			else if(OptionsHandler.GetSelectedIndex() == 3){
				OptionsHandler = new OptionsHandler (new List<string> () {"Continue", "Leaderboard", "Options"});
				PromptIntro();
			}
			break;
		case MainState.End:
			SharingHandler.Share();
			break;
		}
	}
	
	public void OnLeaderboardActivate() {}
	
	public void OnLeaderboardEnd()
	{
		InputManager.Instance.SetInputListener (this);
		switch (mainState)
		{
		case MainState.Intro:
			PromptIntro();
			break;
		case MainState.End:
			PromptEnd();
			break;
		}
	}
	
	private void PromptIntro()
	{
		lastState = mainState;
		mainState = MainState.Intro;
		
		string greetingMessage = (GameManager.Instance.HighStreak > 0) ? 
			("Highest: " + GameManager.Instance.HighStreak) : ("Hello.");

		string choiceMessage = "";

		if (GameManager.Instance.IsOnline) {
			choiceMessage = "\n[Tap] to cycle \n[Hold] to select\n" + OptionsHandler.GetOptionListString();
		}
		else{
			choiceMessage = "\n[Tap] to continue\n[Hold] for login";
		}
		
		Writer.WriteTextInstant (greetingMessage + choiceMessage);
		

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
		
		Writer.WriteTextInstant("Streak: " + GameManager.Instance.Streak +
		                        "\nHighest: " + GameManager.Instance.HighStreak + 
		                        "\n[Tap] to Main Menu" +
		                        (GameManager.Instance.IsOnline ? "\n[Hold] to share" : ""));
	}

	private void PromptOptions()
	{
		lastState = mainState;
		mainState = MainState.Options;
		
		string choiceMessage = "\n[Tap] to cycle \n[Hold] to select\n" + OptionsHandler.GetOptionListString();

		Writer.WriteTextInstant (choiceMessage);
	}
	
	private IEnumerator InstructionsCoroutine()
	{
		yield return StartCoroutine (WaitForSecondsOrTap (4f));
		
		PromptGame ();
	}
	
	private IEnumerator GameCoroutine()
	{
		int currTierIndex = 0;
		GameManager.Instance.Streak = 0;
		
		while (true) 
		{
			GameManager.Instance.Tier = TierBook.TierList [currTierIndex];
			int wordLimit = GameManager.Instance.Tier.TierWordLimit;
			float typingSpeed = GameManager.Instance.Tier.TierTypingSpeed;
			
			// get a random phrase and generate a raw message from the phrase
			Phrase randomPhrase = PhraseBook.GetPhraseWithWordLimit(wordLimit);
			string correctMessage = randomPhrase.CorrectMessage;
			string rawMessage = Regex.Replace(correctMessage, @"\s+", "");
			int wordCount = correctMessage.Split(' ').Length;
			
			Writer.WriteTextInstant (rawMessage + "\n" + wordCount + " words");
			
			yield return StartCoroutine(WaitForSecondsOrTap(3f));
			
			// start writing raw message
			Writer.SetTypeDuration (typingSpeed);
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
			
			if(writeResult)
			{
				if(successSound != null)
					SoundManager.Instance.PlaySoundModulated(successSound, transform.position);
				
				GameManager.Instance.Streak = Math.Min(SCORE_MAX, GameManager.Instance.Streak + 1);
				
				if(currTierIndex + 1 < TierBook.TierList.Count && GameManager.Instance.Streak >= TierBook.TierList[currTierIndex + 1].TierThreshold)
				{
					currTierIndex++;
				}
				
				// first check if its a new day and update the day
				if(DateTime.UtcNow.Date > GameManager.Instance.DailyTimestamp.ToUniversalTime().Date)
				{
					GameManager.Instance.DailyStreak = 0;
					GameManager.Instance.DailyTimestamp = DateTime.UtcNow;
					PlayerPrefs.SetString(ParseUserUtils.KEY_DAILY_TIMESTAMP, GameManager.Instance.DailyTimestamp.ToBinary().ToString());
				} 
				
				// TODO: Potential problem here where timestamp might not save
				if(GameManager.Instance.Streak > GameManager.Instance.DailyStreak)
				{
					GameManager.Instance.DailyStreak = GameManager.Instance.Streak;
					PlayerPrefs.SetInt(ParseUserUtils.KEY_DAILY_STREAK, GameManager.Instance.DailyStreak);
					
					if(GameManager.Instance.Streak > GameManager.Instance.HighStreak) 
					{
						GameManager.Instance.HighStreak = GameManager.Instance.Streak;
						PlayerPrefs.SetInt(ParseUserUtils.KEY_STREAK, GameManager.Instance.HighStreak);
					}
					
					PlayerPrefs.Save();
				}
				
				// display streaks
				Writer.WriteTextInstant("Streak: " + GameManager.Instance.Streak + "\nHighest: " + GameManager.Instance.HighStreak);
				
				yield return StartCoroutine(WaitForSecondsOrTap(2f));
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
		
		IDictionary<string, object> userInfo = new Dictionary<string, object>
		{
			{ ParseUserUtils.KEY_STREAK, GameManager.Instance.HighStreak },
			{ ParseUserUtils.KEY_DAILY_STREAK, GameManager.Instance.DailyStreak },
			{ ParseUserUtils.KEY_DAILY_TIMESTAMP, GameManager.Instance.DailyTimestamp }
		};
		Task<IDictionary<string, object>> syncTask = 
			ParseCloud.CallFunctionAsync<IDictionary<string, object>> ("SubmitStreak", userInfo);
		
		while (!syncTask.IsCompleted) 
		{
			yield return null;
		}
		
		if (syncTask.IsFaulted || syncTask.IsCanceled)
		{
			using (IEnumerator<System.Exception> enumerator = syncTask.Exception.InnerExceptions.GetEnumerator()) 
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
		
		DateTime startTime = DateTime.UtcNow;
		TimeSpan waitDuration = TimeSpan.FromSeconds(duration);
		
		while (!didTap) 
		{
			if(DateTime.UtcNow - startTime >= waitDuration)
			{
				break;
			}
			
			yield return null;
		}
		
		didTap = false;
	}
	
	private bool ShouldSyncOverall()
	{
		if (!GameManager.Instance.IsOnline)
			return false;
		
		if(GameManager.Instance.DailyStreak > ParseUser.CurrentUser.Get<int>(ParseUserUtils.KEY_DAILY_STREAK))
			return true;
		
		return DateTime.UtcNow.Date != ParseUser.CurrentUser.Get<DateTime>(ParseUserUtils.KEY_DAILY_TIMESTAMP).Date;
	}
}
