using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using System.Threading.Tasks;
using Parse;

public class PhraseKeeper : MonoBehaviour 
{	
	public enum KeeperState
	{
		Neutral,
		Fetching,
		Error
	}
	
	// const data
	private const int PHRASE_QUEUE_LIMIT = 10;
	private const float TODAY_PRIORITY = 0.5f;
	private const float OVERALL_PRIORITY = 0.35f;
	private const float LOCAL_PRIORITY = 0.15f;
	
	private const float REQUEUE_THRESHOLD = 0.15f;
	
	public PhraseBook LocalBook;
	
	// state data
	public KeeperState keeperState { get; private set; }
	public bool isFetchedReady { get; private set; }
	
	// internal phrase data
	private Queue<Phrase> phraseQueue = new Queue<Phrase>();
	
	private int wordLimit;
	
	// internal data
	private ErrorInfo errorInfo;
	
	private Coroutine fetchCoroutine;
	
	// fetches phrases from server
	// or loads from phrasebook if offline
	// 
	public void FetchEnqueuePhrases()
	{
		if(GameManager.Instance.IsOnline)
		{
			if(fetchCoroutine != null)
				StopCoroutine(fetchCoroutine);
				
			fetchCoroutine = StartCoroutine(FetchCoroutine());
		}
		else
		{
			EnqueueLocalPhrases();
		}
	}
	
	public void StopFetching()
	{
		if(fetchCoroutine != null)
			StopCoroutine(fetchCoroutine);
	}
	
	public Phrase PopPhraseQueue()
	{
		Phrase resultPhrase = phraseQueue.Dequeue();
		if(ShouldRequeue())
			FetchEnqueuePhrases();
			
		string debugString = "Popped queue: ";
		foreach(Phrase phrase in phraseQueue)
		{
			debugString += phrase.CorrectMessage + "|";
		}
		Debug.Log (debugString);
		return resultPhrase;
	}
	
	public void ClearPhraseQueue()
	{
		phraseQueue.Clear();
	}
	
	public void SetWordLimit(int limit)
	{
		wordLimit = limit;
	}
	
	public bool IsPhraseQueueEmpty()
	{
		return phraseQueue.Count == 0;
	}
	
	// fetches phrases from server
	// we pull double the limit 
	private IEnumerator FetchCoroutine()
	{
		keeperState = KeeperState.Fetching;
		isFetchedReady = false;
		
		List<Phrase> todayPhrases = new List<Phrase> ();
		List<Phrase> overallPhrases = new List<Phrase> ();
		List<Phrase> finalPhrases = new List<Phrase>();
		
		if (GameManager.Instance.HighStreak == 0)
			finalPhrases.Add(new Phrase() { CorrectMessage = "Space This" });
		
		// TODO: recycle old phrases
		int todayLimit = (int)(PHRASE_QUEUE_LIMIT * TODAY_PRIORITY);
		int overallLimit = (int)(PHRASE_QUEUE_LIMIT * OVERALL_PRIORITY);
		
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
			
			keeperState = KeeperState.Error;
			yield break;
		} 
		else
		{
			IDictionary<string, object> result = fetchTask.Result;
			object resultPhrasesRaw;
			if(result.TryGetValue("resultPhrases", out resultPhrasesRaw))
			{
				List<Phrase> resultPhrases = (List<Phrase>) resultPhrasesRaw;
				string debugString = "";
				foreach(Phrase phrase in resultPhrases)
				{
					debugString += phrase.CorrectMessage + "|";
				}
				Debug.Log (debugString);
			}
		}
		
		isFetchedReady = true;
		phraseQueue.Clear();
		phraseQueue.AddRange(finalPhrases);
	}
	
	// enqueues local phrases
	// all phrases in phrase queue will be local if offline
	// otherwise only (LOCAL_PRIORITY * 100)% phrases will be local
	private void EnqueueLocalPhrases()
	{
		List<Phrase> finalPhrases = new List<Phrase>();
		
		if (GameManager.Instance.HighStreak == 0)
			finalPhrases.Add(new Phrase() { CorrectMessage = "Space This" });
		
		List<Phrase> localPhrases = LocalBook.PhraseList.Where (p => p.CorrectMessage.Split (' ').Length <= wordLimit).ToList();
		localPhrases.Shuffle ();
		
		int localLimit = (int)(PHRASE_QUEUE_LIMIT * (GameManager.Instance.IsOnline ? LOCAL_PRIORITY : 1));
		localPhrases = localPhrases.Take (localLimit).ToList();
		finalPhrases.AddRange (localPhrases);
		
		phraseQueue.Clear();
		phraseQueue.AddRange(finalPhrases);
	}
	
	private bool ShouldRequeue()
	{
		return phraseQueue.Count <= (int)(PHRASE_QUEUE_LIMIT * REQUEUE_THRESHOLD);
	}
}