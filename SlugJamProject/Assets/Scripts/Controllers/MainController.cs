﻿using UnityEngine;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

using System.Threading.Tasks;
using Parse;

public class MainController : Controller, InputManager.InputListener, LeaderboardController.LeaderboardListener, OptionsController.OptionsListener
{
	#region Enum Defs


	private enum MainState
	{
		Intro,
		Instructions,
		Game,
		End,
		SyncError,
		Options,
		Social
	}
	
	private enum DifficultyState
	{
		Normal,
		Hard,
		Dynamic
	}


	#endregion

	#region Data
	

	// const data
	private const int SELECTION_GAME = 0;
	private const int SELECTION_LEADERBOARD = 1;
	private const int SELECTION_DIFFICULTY = 2;
	private const int SELECTION_OPTIONS = 3;

	// data data
	public TierBook TierBook;
	
	// modular data
	public LeaderboardController LeaderboardController;
	public OptionsController OptionsController;
	
	public SharingWorker SharingWorker;
	public PhraseKeeper PhraseKeeper;
	
	// type data
	public TypeWriter Writer;
	public AudioClip errorSound;
	public AudioClip successSound;
	
	// internal modules
	private SelectionHandler selectionHandler;
	
	// internal data
	private Coroutine syncCoroutine;
	
	private int SCORE_MAX = 8192;

	private MainState mainState;
	private ErrorInfo errorInfo;

	private bool isSyncing;
	//private DifficultyState difficultyState;
	
	private bool didTap;


	#endregion

	#region MonoBehaviour Overrides


	protected override void Awake()
	{
		base.Awake ();
		
		InputManager.Instance.SetInputListener (this);
		LeaderboardController.SetListener (this);
		OptionsController.SetListener (this);
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


	#endregion

	#region Input Overrides


	public void OnTouchBegin()
	{
		if (!isActive)
			return;

		if(isSyncing || LeaderboardController.IsActive() || SharingWorker.isSharing || OptionsController.IsActive())
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
		
		if(isSyncing || LeaderboardController.IsActive() || SharingWorker.isSharing || OptionsController.IsActive())
			return;
		
		didTap = true;
		
		switch (mainState)
		{
		case MainState.Intro:
			PromptGame();
			break;
		case MainState.Social:
			StartCoroutine(LeaderboardCoroutine());
			break;
		case MainState.End:
			PromptIntro();
			break;
		case MainState.SyncError:
			PromptIntro();
			break;
		}
	}
	
	public void OnHold()
	{
		if (!isActive)
			return;
		
		if(isSyncing || LeaderboardController.IsActive() || SharingWorker.isSharing || OptionsController.IsActive())
			return;

		switch (mainState) 
		{
		case MainState.Intro:
			if(GameManager.Instance.IsOnline)
				PromptSocial();
			else
				GoToLevel("Entry");
			break;
		case MainState.Social:
			Writer.ClearWriting();
			OptionsController.Activate();
			break;
		case MainState.End:
			SharingWorker.Share();
			break;
		}
	}


	#endregion

	#region Leaderboard Overrides

	
	public void OnLeaderboardActivate() {}
	
	public void OnLeaderboardEnd()
	{
		InputManager.Instance.SetInputListener (this);
		switch (mainState)
		{
		case MainState.Social:
			PromptIntro();
			break;
		}
	}


	#endregion
	
	#region Options Overrides
	
	
	public void OnOptionsActivate() {}
	
	public void OnOptionsEnd()
	{
		InputManager.Instance.SetInputListener(this);
		PromptIntro();
	}
	
	
	#endregion

	#region Prompt Functions

	
	private void PromptIntro()
	{
		mainState = MainState.Intro;
		
		string greetingMessage = (GameManager.Instance.HighStreak > 0) ? 
			("Highest: " + GameManager.Instance.HighStreak) : ("Hello.");

		string choiceMessage = "";
		
		if(GameManager.Instance.IsOnline)
			choiceMessage = "\n[Tap] to continue \n[Hold] for social";
		else
			choiceMessage = "\n[Tap] to continue \n[Hold] for login";
		
		Writer.WriteTextInstant (greetingMessage + choiceMessage);
		

	}
	
	private void PromptInstructions()
	{
		mainState = MainState.Instructions;
		
		Writer.WriteTextInstant (MessageBook.InstructionsMessage);
		
		StartCoroutine (InstructionsCoroutine ());
	}
	
	private void PromptGame()
	{
		mainState = MainState.Game;
		
		StartCoroutine (GameCoroutine ());
	}
	
	private void PromptEnd()
	{
		mainState = MainState.End;
		
		Writer.WriteTextInstant("Streak: " + GameManager.Instance.Streak +
		                        "\nHighest: " + GameManager.Instance.HighStreak + 
		                        "\n[Tap] for menu" +
		                        "\n[Hold] to share");
	}


	#endregion

	#region State Coroutines

	private void PromptSocial()
	{
		mainState = MainState.Social;

		string choiceMessage = 
			"[Tap] for leaderboard\n" +
			"[Hold] for settings";
		Writer.WriteTextInstant (choiceMessage);
	}
	
	private IEnumerator InstructionsCoroutine()
	{
		yield return StartCoroutine (WaitForSecondsOrTap (4f));
		
		PromptGame ();
	}
	
	private IEnumerator FetchPhrasesCoroutine()
	{	
		if(PhraseKeeper.keeperState != PhraseKeeper.KeeperState.Fetching)
			yield break;
			
		Writer.WriteTextInstant("Fetching...");
		
		while(PhraseKeeper.keeperState == PhraseKeeper.KeeperState.Fetching)
		{
			yield return null;
		}
		
		if(PhraseKeeper.keeperState == PhraseKeeper.KeeperState.Error)
		{
			Writer.WriteTextInstant(PhraseKeeper.errorInfo.GetErrorStr() + "\n" +
			                             "[Tap] to return\n");
			yield return StartCoroutine(WaitForTap());
			StopAllCoroutines();
			PromptIntro();
		}
	}

	private IEnumerator GameCoroutine()
	{
		int currTierIndex = 0;
		GameManager.Instance.Streak = 0;
		GameManager.Instance.Tier = TierBook.TierList [currTierIndex];
		
		yield return StartCoroutine(FetchPhrasesCoroutine());
	
		PhraseKeeper.EnqueuePhrases(GameManager.Instance.Tier.TierWordLimit);

		while (true) 
		{
			// get phrases before we begin only if needed
			yield return StartCoroutine(FetchPhrasesCoroutine());
			
			// get a random phrase and generate a raw message from the phrase
			Phrase randomPhrase = PhraseKeeper.PopPhraseQueue();
			string correctMessage = randomPhrase.CorrectMessage;
			string rawMessage = Regex.Replace(correctMessage, @"\s+", "");
			int wordCount = correctMessage.Split(' ').Length;
			
			Writer.WriteTextInstant (rawMessage + "\n" + wordCount + " words");
			
			yield return StartCoroutine(WaitForSecondsOrTap(3f));
			
			// start writing raw message
			Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_AUTO);
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
						SoundManager.Instance.PlaySound(errorSound, 0.5f);
					
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
					SoundManager.Instance.PlaySoundModulated(successSound, 0.5f);

				// increment streak				
				GameManager.Instance.Streak = Math.Min(SCORE_MAX, GameManager.Instance.Streak + 1);

				if(currTierIndex + 1 < TierBook.TierList.Count && GameManager.Instance.Streak >= TierBook.TierList[currTierIndex + 1].TierThreshold)
				{
					currTierIndex++;
					GameManager.Instance.Tier = TierBook.TierList [currTierIndex];
					PhraseKeeper.EnqueuePhrases(GameManager.Instance.Tier.TierWordLimit);
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
				if(ShouldSyncOverall())
				{
					if(syncCoroutine != null)
						StopCoroutine(syncCoroutine);
					
					syncCoroutine = StartCoroutine(SyncCoroutine());
				}
				
				PromptEnd();
				break;
			}
		}
	}
	
	private IEnumerator LeaderboardCoroutine()
	{
		if (ShouldSyncOverall() && !isSyncing)
		{
			if(syncCoroutine != null)
				StopCoroutine(syncCoroutine);
				
			syncCoroutine = StartCoroutine (SyncCoroutine ());
			yield return syncCoroutine;
			
			if (mainState == MainState.SyncError)
			{
				Writer.WriteTextInstant(errorInfo.GetErrorStr() + "\n" +
				                        "[Tap] to return\n");
				
				yield break;
			}
		}
		
		Writer.ClearWriting ();
		LeaderboardController.Activate ();
	}


	#endregion

	#region Network Coroutines


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
		
		DateTime startTime = DateTime.UtcNow;
		TimeSpan waitDuration = TimeSpan.FromSeconds(TimeUtils.TIMEOUT_DURATION);
		while (!syncTask.IsCompleted) 
		{
			if(DateTime.UtcNow - startTime >= waitDuration) 
				break;
			
			yield return null;
		}
		
		if(!syncTask.IsCompleted)
		{
			errorInfo = new ErrorInfo(ErrorType.Timeout);
			mainState = MainState.SyncError;
		}
		else if (syncTask.IsFaulted)
		{
			using (IEnumerator<System.Exception> enumerator = syncTask.Exception.InnerExceptions.GetEnumerator()) 
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

			mainState = MainState.SyncError;
		}
		else
		{
			// we're good, update the local instance of ParseUser
			ParseUser.CurrentUser[ParseUserUtils.KEY_STREAK] = GameManager.Instance.HighStreak;
			ParseUser.CurrentUser[ParseUserUtils.KEY_DAILY_STREAK] = GameManager.Instance.DailyStreak;
			ParseUser.CurrentUser[ParseUserUtils.KEY_DAILY_TIMESTAMP] = GameManager.Instance.DailyTimestamp;
		}
		
		isSyncing = false;
	}


	#endregion

	#region Generic Coroutines


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


	#endregion

	#region Helpers
	

	private bool ShouldSyncOverall()
	{
		if (!GameManager.Instance.IsOnline)
			return false;
		
		if(DateTime.UtcNow.Date != ParseUser.CurrentUser.Get<DateTime>(ParseUserUtils.KEY_DAILY_TIMESTAMP).Date)
			return true;
		
		return GameManager.Instance.DailyStreak > ParseUser.CurrentUser.Get<int>(ParseUserUtils.KEY_DAILY_STREAK);
	}
	

	#endregion
}
