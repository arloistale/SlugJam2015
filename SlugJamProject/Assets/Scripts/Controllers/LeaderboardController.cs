using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using System.Threading.Tasks;

using Parse;

public class LeaderboardController : Controller, InputManager.InputListener
{
	private enum LeaderboardPage
	{
		Overall,
		Today,
		Options
	}

	private enum PageState
	{
		Fetching,
		Ready,
		Error
	}
	
	private enum ErrorType
	{
		Unknown,
		ParseInternal,
		ParseException
	}

	// const data
	public const int PAGINATION_AMOUNT = 10;
	
	// type data
	public TypeWriter Writer;

	// cached data
	private List<ParseUser> overallUsers;
	private List<ParseUser> todayUsers;
	
	// internal data
	private LeaderboardPage leaderboardPage;
	private PageState overallPageState;
	private PageState todayPageState;

	private ErrorType overallErrorType;
	private ParseException.ErrorCode overallErrorCode;

	private ErrorType todayErrorType;
	private ParseException.ErrorCode todayErrorCode;

	protected override void Awake()
	{
		base.Awake ();

		isActive = false;
	}
	
	public void Activate()
	{
		isActive = true;

		InputManager.Instance.SetInputListener (this);

		StartCoroutine (FetchCoroutine ());

		PromptOverall ();
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

		switch (leaderboardPage) 
		{
		case LeaderboardPage.Overall:
			PromptToday();
			break;
		case LeaderboardPage.Today:
			PromptOptions();
			break;
		case LeaderboardPage.Options:
			PromptOverall();
			break;
		}
	}
	
	public void OnHold()
	{
		if (!isActive)
			return;
		
		switch (leaderboardPage) 
		{
		case LeaderboardPage.Overall:
			//PromptToday();
			break;
		case LeaderboardPage.Today:
			//PromptOptions();
			break;
		case LeaderboardPage.Options:
			// end
			break;
		}
	}
	
	private void PromptOverall()
	{
		leaderboardPage = LeaderboardPage.Overall;
		
		if (overallPageState == PageState.Ready) 
		{
			Writer.WriteTextInstant("Top Overall\n" +
			                        "[Tap] to cycle\n\n");
			for(int i = 0; i < overallUsers.Count; i++)
			{
				Writer.AppendTextInstant(overallUsers[i].Username + ": " + overallUsers[i].Get<int>(ParseUserUtils.KEY_STREAK) + "\n");
			}
		}
		else if (overallPageState == PageState.Fetching) 
		{
			Writer.WriteTextInstant("Fetching...\n" +
			                        "[Tap] to cycle\n");
		}
		else if (overallPageState == PageState.Error)
		{
			string errorStr = "Unknown error";
			
			switch(overallErrorType)
			{
			case ErrorType.ParseInternal:
				errorStr = "Server error";
				break;
			case ErrorType.ParseException:
				if(MessageBook.ParseExceptionMap.ContainsKey(overallErrorCode))
					errorStr = MessageBook.ParseExceptionMap[overallErrorCode];
				else
					errorStr = overallErrorCode + "";
				break;
			}
			
			Writer.WriteTextInstant(errorStr + "\n" +
			                        "[Tap] to cycle\n");
		}
	}
	
	private void PromptToday()
	{
		leaderboardPage = LeaderboardPage.Today;
		
		if (todayPageState == PageState.Ready) 
		{
			Writer.WriteTextInstant("Top Today\n" +
			                        "[Tap] to cycle\n\n");
			for(int i = 0; i < todayUsers.Count; i++)
			{
				Writer.AppendTextInstant(todayUsers[i].Username + ": " + todayUsers[i].Get<int>(ParseUserUtils.KEY_STREAK) + "\n");
			}
		}
		else if (todayPageState == PageState.Fetching) 
		{
			Writer.WriteTextInstant("Fetching...");
		}
		else if (todayPageState == PageState.Error)
		{
			string errorStr = "Unknown error";
			
			switch(todayErrorType)
			{
			case ErrorType.ParseInternal:
				errorStr = "Server error";
				break;
			case ErrorType.ParseException:
				if(MessageBook.ParseExceptionMap.ContainsKey(todayErrorCode))
					errorStr = MessageBook.ParseExceptionMap[todayErrorCode];
				else
					errorStr = todayErrorCode + "";
				break;
			}
			
			Writer.WriteTextInstant(errorStr + "\n" +
			                        "[Tap] to cycle\n");
		}
	}
	
	private void PromptOptions()
	{
		leaderboardPage = LeaderboardPage.Options;
	}

	private IEnumerator FetchCoroutine()
	{
		// set both sets of cached users as dirty
		overallUsers = null;
		todayUsers = null;
		
		overallPageState = PageState.Fetching;
		todayPageState = PageState.Fetching;
		
		// first we must fetch and cache high scores for overall
		ParseQuery<ParseUser> overallQuery = ParseUser.Query.
			WhereGreaterThan(ParseUserUtils.KEY_STREAK, 0).
			OrderByDescending (ParseUserUtils.KEY_STREAK).
				Limit (PAGINATION_AMOUNT);
		
		Task<IEnumerable<ParseUser>> overallTask = overallQuery.FindAsync ();
		
		while (!overallTask.IsCompleted) 
		{
			yield return null;
		}
		
		if (overallTask.IsFaulted || overallTask.IsCanceled)
		{
			using (IEnumerator<System.Exception> enumerator = overallTask.Exception.InnerExceptions.GetEnumerator()) 
			{
				if (enumerator.MoveNext())
				{
					ParseException exception = (ParseException) enumerator.Current;
					overallErrorCode = exception.Code;
					overallErrorType = ErrorType.ParseException;
				}
				else
				{
					overallErrorType = ErrorType.ParseInternal;
				}
			}
			
			overallPageState = PageState.Error;
		}
		else
		{
			overallUsers = overallTask.Result.ToList();
			overallPageState = PageState.Ready;
		}
		
		if (leaderboardPage == LeaderboardPage.Overall)
			PromptOverall ();

		// now we load and cache todays high scores
		ParseQuery<ParseUser> todayQuery = ParseUser.Query.
			WhereGreaterThan(ParseUserUtils.KEY_STREAK, 0).
			OrderByDescending (ParseUserUtils.KEY_STREAK).
			Limit (PAGINATION_AMOUNT);
		
		Task<IEnumerable<ParseUser>> todayTask = todayQuery.FindAsync ();
		
		while (!todayTask.IsCompleted) 
		{
			yield return null;
		}
		
		if (todayTask.IsFaulted || todayTask.IsCanceled)
		{
			using (IEnumerator<System.Exception> enumerator = todayTask.Exception.InnerExceptions.GetEnumerator()) 
			{
				if (enumerator.MoveNext())
				{
					ParseException exception = (ParseException) enumerator.Current;
					todayErrorCode = exception.Code;
					todayErrorType = ErrorType.ParseException;
				}
				else
				{
					todayErrorType = ErrorType.ParseInternal;
				}
			}
			
			todayPageState = PageState.Error;
		}
		else
		{
			todayUsers = todayTask.Result.ToList();
			todayPageState = PageState.Ready;
		}
		
		if (leaderboardPage == LeaderboardPage.Today)
			PromptToday ();
	}
}