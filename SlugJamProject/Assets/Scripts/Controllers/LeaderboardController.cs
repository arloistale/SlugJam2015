using UnityEngine;
using System.IO;
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

	// const data
	public const int PAGINATION_AMOUNT = 10;
	
	// modules data
	public TypeWriter ListingWriter;
	public TypeWriter AsyncWriter;

	// cached data
	private List<ParseUser> overallUsers;
	private List<ParseUser> todayUsers;
	
	// internal data
	private LeaderboardListener leaderboardListener;

	private LeaderboardPage leaderboardPage;
	private PageState overallPageState;
	private PageState todayPageState;

	private ErrorInfo overallErrorInfo;
	private ErrorInfo todayErrorInfo;

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

		PromptToday ();

		if(leaderboardListener != null) leaderboardListener.OnLeaderboardActivate ();
	}

	public void End()
	{
		isActive = false;

		AsyncWriter.ClearWriting ();
		ListingWriter.ClearWriting ();

		if(leaderboardListener != null) leaderboardListener.OnLeaderboardEnd ();
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

		if (overallPageState == PageState.Fetching || todayPageState == PageState.Fetching)
			return;

		switch (leaderboardPage) 
		{
		case LeaderboardPage.Overall:
			PromptOptions();
			break;
		case LeaderboardPage.Today:
			PromptOverall();
			break;
		case LeaderboardPage.Options:
			PromptToday();
			break;
		}
	}
	
	public void OnHold()
	{
		if (!isActive)
			return;

		if (overallPageState == PageState.Fetching || todayPageState == PageState.Fetching)
			return;
		
		switch (leaderboardPage) 
		{
		case LeaderboardPage.Overall:
			break;
		case LeaderboardPage.Today:
			break;
		case LeaderboardPage.Options:
			End ();
			break;
		}
	}

	public void SetListener(LeaderboardListener listener)
	{
		leaderboardListener = listener;
	}
	
	private void PromptOverall()
	{
		leaderboardPage = LeaderboardPage.Overall;
		
		if (overallPageState == PageState.Ready) 
		{
			AsyncWriter.ClearWriting();

			ListingWriter.WriteTextInstant("Top Overall\n" +
			                               "[Tap] to cycle\n\n" +
			                               GetStreakListing(overallUsers));
		}
		else if (overallPageState == PageState.Fetching) 
		{
			AsyncWriter.WriteTextInstant("Fetching...");
		}
		else if (overallPageState == PageState.Error)
		{	
			AsyncWriter.WriteTextInstant(overallErrorInfo.GetErrorStr() + "\n" +
			                        "[Tap] to cycle\n");
		}
	}
	
	private void PromptToday()
	{
		leaderboardPage = LeaderboardPage.Today;
		
		if (todayPageState == PageState.Ready) 
		{
			AsyncWriter.ClearWriting();

			ListingWriter.WriteTextInstant("Top Today\n" +
			                               "[Tap] to cycle\n\n" +
			                               GetDailyStreakListing(todayUsers));
		}
		else if (todayPageState == PageState.Fetching)
		{
			AsyncWriter.WriteTextInstant("Fetching...");
		}
		else if (todayPageState == PageState.Error)
		{
			AsyncWriter.WriteTextInstant(todayErrorInfo.GetErrorStr() + "\n" +
			                             "[Tap] to cycle\n");
		}
	}
	
	private void PromptOptions()
	{
		leaderboardPage = LeaderboardPage.Options;

		ListingWriter.ClearWriting ();
		AsyncWriter.WriteTextInstant ("Options\n" +
		                              "[Tap] to cycle\n" +
		                              "[Hold] for Main Menu");
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
		
		DateTime startTime = DateTime.UtcNow;
		TimeSpan waitDuration = TimeSpan.FromSeconds(TimeUtils.TIMEOUT_DURATION);
		while (!overallTask.IsCompleted) 
		{
			if(DateTime.UtcNow - startTime >= waitDuration) 
				break;
			
			yield return null;
		}
		
		if(!overallTask.IsCompleted)
		{
			overallErrorInfo = new ErrorInfo(ErrorType.Timeout);
			overallPageState = PageState.Error;
		}
		else if (overallTask.IsFaulted)
		{
			using (IEnumerator<System.Exception> enumerator = overallTask.Exception.InnerExceptions.GetEnumerator()) 
			{
				if (enumerator.MoveNext())
				{
					ParseException exception = (ParseException) enumerator.Current;
					overallErrorInfo = new ErrorInfo(ErrorType.ParseException, exception.Code);
				}
				else
				{
					overallErrorInfo = new ErrorInfo(ErrorType.ParseInternal);
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
			WhereGreaterThanOrEqualTo(ParseUserUtils.KEY_DAILY_TIMESTAMP, DateTime.UtcNow.Date).
			WhereGreaterThan(ParseUserUtils.KEY_DAILY_STREAK, 0).
			OrderByDescending (ParseUserUtils.KEY_DAILY_STREAK).
			Limit (PAGINATION_AMOUNT);
		
		Task<IEnumerable<ParseUser>> todayTask = todayQuery.FindAsync ();
		
		startTime = DateTime.UtcNow;
		waitDuration = TimeSpan.FromSeconds(TimeUtils.TIMEOUT_DURATION);
		while (!todayTask.IsCompleted) 
		{
			if(DateTime.UtcNow - startTime >= waitDuration) 
				break;
			
			yield return null;
		}
		
		if(!todayTask.IsCompleted)
		{
			todayErrorInfo = new ErrorInfo(ErrorType.Timeout);
			todayPageState = PageState.Error;
		}
		else if (todayTask.IsFaulted)
		{
			using (IEnumerator<System.Exception> enumerator = todayTask.Exception.InnerExceptions.GetEnumerator()) 
			{
				if (enumerator.MoveNext())
				{
					ParseException exception = (ParseException) enumerator.Current;
					todayErrorInfo = new ErrorInfo(ErrorType.ParseException, exception.Code);
				}
				else
				{
					todayErrorInfo = new ErrorInfo(ErrorType.ParseInternal);
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

	private string GetStreakListing(List<ParseUser> users)
	{
		if (users.Count == 0)
			return "Oh boy, it's empty!";

		ParseUser currentUser = ParseUser.CurrentUser;
		string listingStr = "";

		for(int i = 0; i < PAGINATION_AMOUNT; i++)
		{
			if(i < users.Count)
			{
				listingStr += String.Format("{0,-3} {1,-14} {2,4}", (i + 1) + ".", 
				                            (currentUser.ObjectId != users[i].ObjectId) ? users[i].Username : ("*" + users[i].Username + "*"), 
				                            users[i].Get<int>(ParseUserUtils.KEY_STREAK));
			}
			
			if(i < PAGINATION_AMOUNT - 1)
			{
				listingStr += "\n";
			}
		}
		
		return listingStr;
	}

	private string GetDailyStreakListing(List<ParseUser> users)
	{
		if (users.Count == 0)
			return "Oh boy, it's empty!";

		ParseUser currentUser = ParseUser.CurrentUser;
		string listingStr = "";
		
		for(int i = 0; i < PAGINATION_AMOUNT; i++)
		{
			if(i < users.Count)
			{
				listingStr += String.Format("{0,-3} {1,-14} {2,4}", (i + 1) + ".", 
				                            (currentUser.ObjectId != users[i].ObjectId) ? users[i].Username : ("*" + users[i].Username + "*"), 
				                            users[i].Get<int>(ParseUserUtils.KEY_DAILY_STREAK));
			}
			
			if(i < PAGINATION_AMOUNT - 1)
			{
				listingStr += "\n";
			}
		}
		
		return listingStr;
	}

	public interface LeaderboardListener
	{
		void OnLeaderboardActivate();
		void OnLeaderboardEnd();
	}
}