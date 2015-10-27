using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using System.Threading.Tasks;
using Parse;

public class PhraseKeeper : MonoBehaviour 
{	
	// const data
	private int PHRASE_QUEUE_LIMIT = 100;
	private float TODAY_PRIORITY = 0.5f;
	private float OVERALL_PRIORITY = 0.35f;
	private float LOCAL_PRIORITY = 0.15f;
	
	public PhraseBook LocalBook;
	
	// internal phrase data
	private List<Phrase> todayPhraseCollection = new List<Phrase>();
	private Queue<Phrase> phraseQueue = new Queue<Phrase>();
	
	// internal data
	private bool isFetching;
	private bool isFetchedReady;
	
	private ErrorInfo errorInfo;
	
	public void EnqueuePhrases(int wordLimit)
	{
		List<Phrase> finalPhrases = new List<Phrase>();
		
		if (GameManager.Instance.HighStreak == 0)
			finalPhrases.Add(new Phrase() { CorrectMessage = "Space This" });
		else if(GameManager.Instance.IsOnline && IsFetchedReady())
		{
			int randomIndex = UnityEngine.Random.Range(0, todayPhraseCollection.Count);
			finalPhrases.Add(todayPhraseCollection[randomIndex]);
		}
		
		List<Phrase> localPhrases = LocalBook.PhraseList.Where (p => p.CorrectMessage.Split (' ').Length <= wordLimit).ToList();
		localPhrases.Shuffle ();
		
		if (!GameManager.Instance.IsOnline)
		{
			localPhrases = localPhrases.Take (PHRASE_QUEUE_LIMIT).ToList();
			finalPhrases.AddRange (localPhrases);
		}
		else
		{
			localPhrases = localPhrases.Take (PHRASE_QUEUE_LIMIT).ToList();
		}
		
		phraseQueue = new Queue<Phrase>(finalPhrases);
	}
	
	public Phrase PopPhraseQueue()
	{
		string debugString = "";
		foreach(Phrase phrase in phraseQueue)
		{
			debugString += phrase.CorrectMessage + "|";
		}
		Debug.Log (debugString);
		return phraseQueue.Dequeue();
	}
	
	public void ClearPhraseQueue()
	{
		phraseQueue.Clear();
	}
	
	public bool IsPhraseQueueEmpty()
	{
		return phraseQueue.Count == 0;
	}
	
	public bool IsFetchedReady()
	{
		return isFetchedReady;
	}
	
	// enqueues a number of phrases, handles pulling phrases from server
	private IEnumerator FetchCoroutine()
	{
		isFetching = true;
		
		List<Phrase> todayPhrases = new List<Phrase> ();
		List<Phrase> overallPhrases = new List<Phrase> ();
		List<Phrase> finalPhrases = new List<Phrase>();
		
		int todayLimit = PHRASE_QUEUE_LIMIT * TODAY_PRIORITY;
		int overallLimit = PHRASE_QUEUE_LIMIT * OVERALL_PRIORITY;
		
		// prepare the pre-queue list with needed filters
		IDictionary<string, object> fetchInfo = new Dictionary<string, object>
		{
			{ "todayLimit", todayLimit },
			{ "overallLimit", overallLimit }
		};
		Task<IDictionary<string, object>> fetchTask = 
			ParseCloud.CallFunctionAsync<IDictionary<string, object>> ("FetchPhrases", fetchInfo);
		
		while (!fetchTask.IsCompleted) 
		{
			yield return null;
		}
		
		if (fetchTask.IsFaulted || fetchTask.IsCanceled) 
		{
			using (IEnumerator<System.Exception> enumerator = fetchTask.Exception.InnerExceptions.GetEnumerator()) 
			{
				if (enumerator.MoveNext ()) 
				{
					ParseException exception = (ParseException)enumerator.Current;
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
			IDictionary<string, object> result = fetchTask.Result;
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
			}
		}
		
		isFetching = false;
	}
}
