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
	private const int PHRASE_REQUEST_LIMIT = 25;
	private const int PHRASE_QUEUE_LIMIT = 10;
	private const int COLLECTION_SAFETY_THRESHOLD = 15;
	private const int QUEUE_SAFETY_THRESHOLD = 5;
	
	public PhraseBook LocalBook;
	
	// state data
	public KeeperState keeperState { get; private set; }
	public bool isFetchedReady { get; private set; }
	
	// internal phrase data
	private List<Phrase> phraseCollection = new List<Phrase>();
	private Queue<Phrase> phraseQueue = new Queue<Phrase>();
	
	// internal data
	public ErrorInfo errorInfo { get; private set; }
	
	private Coroutine fetchCoroutine;
	
	// fetches phrases from server
	public void FetchPhrases()
	{
		Debug.Log ("Fetching");
		if(GameManager.Instance.IsOnline)
		{
			if(fetchCoroutine != null)
				StopCoroutine(fetchCoroutine);
				
			fetchCoroutine = StartCoroutine(FetchCoroutine());
		}
		else
		{
			isFetchedReady = true;
			// TODO: Currently since we're not clearing its possible to get overlapping quotes
			// is this maybe a good thing?
			phraseCollection.AddRange(LocalBook.PhraseList);
			phraseCollection.Shuffle();
		}
	}
	
	public void EnqueuePhrases(int wordLimit)
	{
		string phraseString = "";
		// first recycle the remaining elements in the queue
		phraseCollection.AddRange(phraseQueue);
		if(phraseQueue.Count > 1)
		{
			phraseString += "(Shaking it up)";
			phraseCollection.Shuffle();
		}
		phraseQueue.Clear();
		
		List<Phrase> finalPhrases = new List<Phrase>();
		
		phraseString += "Collection before = " + phraseCollection.Count;
		
		if (GameManager.Instance.HighStreak == 0)
			finalPhrases.Add(new Phrase() { CorrectMessage = "Space This" });
		
		// get a filtereed list of phrases from collection adhering to word limit
		// we keep a ghost of wordLimit and increase until we get something that matches
		// this means wordLimit is more of a recommendation
		int tempLimit = wordLimit;
		while(finalPhrases.Count < QUEUE_SAFETY_THRESHOLD && phraseCollection.Count > 0)
		{
			// first we get a listing of the phrases adhering to this temporary limit
			IEnumerable<Phrase> filteredPhrases = phraseCollection.
				Where(p => p.CorrectMessage.Split(' ').Length <= tempLimit);
			int finalCount = finalPhrases.Count;
			// also ensure that we don't surpass the queue limit
			// we do this by making sure we don't take more than the remaining difference between limit and currently added
			filteredPhrases = filteredPhrases.Take(Mathf.Min (filteredPhrases.Count(), PHRASE_QUEUE_LIMIT - finalCount));
			// then remove these phrases from the collection and add them to final phrases
			phraseCollection = phraseCollection.Except(filteredPhrases).ToList();
			finalPhrases.AddRange(filteredPhrases);
			tempLimit++;
		}
		
		// check if we should refetch and if so begin the fetching
		if(ShouldRefetch())
			FetchPhrases();
		
		phraseQueue.AddRange(finalPhrases);
		
		Debug.Log (phraseString + " | After = " + phraseCollection.Count);
	}
	
	public void StopFetching()
	{
		if(fetchCoroutine != null)
			StopCoroutine(fetchCoroutine);
	}
	
	public Phrase PopPhraseQueue()
	{
		Phrase resultPhrase = phraseQueue.Dequeue();
		if(IsPhraseQueueEmpty())
		{
			EnqueuePhrases(GameManager.Instance.Tier.TierWordLimit);
			return new Phrase() { CorrectMessage = "We ran out of quotes" };
		}
			
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
		
		List<Phrase> finalPhrases = new List<Phrase>();
		
		// prepare the pre-queue list with needed filters
		IDictionary<string, object> fetchInfo = new Dictionary<string, object>
		{
			{ "requestLimit", PHRASE_REQUEST_LIMIT }
		};
		
		//var fetchTask = ParseCloud.CallFunctionAsync<IEnumerable<ParsePhrase>> ("FetchPhrases", fetchInfo);
		var fetchQuery = new ParseQuery<ParsePhrase>().Limit(PHRASE_REQUEST_LIMIT);
		var fetchTask = fetchQuery.FindAsync();
		
		while (!fetchTask.IsCompleted) yield return null;
		
		if (fetchTask.IsFaulted || fetchTask.IsCanceled) 
		{
			using (IEnumerator<System.Exception> enumerator = fetchTask.Exception.InnerExceptions.GetEnumerator()) 
			{
				if (enumerator.MoveNext ()) 
				{
					Debug.Log (enumerator.Current.ToString());
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
			IEnumerable<ParsePhrase> result = fetchTask.Result;
			if(result != null)
			{
				foreach(ParsePhrase phrase in result)
				{
					phraseCollection.Add (new Phrase() { CorrectMessage = phrase.CorrectMessage });
				}
			}
			else
			{
				errorInfo = new ErrorInfo(ErrorType.ParseInternal);
				keeperState = KeeperState.Error;
				yield break;
			}
		}
		
		isFetchedReady = true;
		phraseQueue.Clear();
		phraseQueue.AddRange(finalPhrases);
		fetchCoroutine = null;
	}
	
	private bool ShouldRefetch()
	{
		return phraseCollection.Count <= COLLECTION_SAFETY_THRESHOLD;
	}
}