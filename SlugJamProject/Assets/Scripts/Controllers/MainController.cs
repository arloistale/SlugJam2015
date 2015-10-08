using UnityEngine;
using System;
using System.Text.RegularExpressions;
using System.Collections;

using Parse;

public class MainController : Controller, InputManager.InputListener
{
	public enum PendingState
	{
		Neutral,
		Listening,
		Tap,
		Hold
	}

	// constants
	private const string KEY_HIGH_STREAK = "High Streak";

	private const float DURATION_STOP = 999999f;

	private const int LEADERBOARD_INTENT_OPTIONS = 1;
	private const int LEADERBOARD_INTENT_VIEW = 2;

	// type data
	public TypeWriter Writer;
	public Phrase[] Phrases = new Phrase[] {};
	public AudioClip errorSound;
	public AudioClip successSound;

	// coroutine data
	private Coroutine waitCoroutine;
	//private Coroutine mainCoroutine;

	private PendingState lastState;

	protected override void Awake()
	{
		base.Awake ();

		InputManager.Instance.SetInputListener (this);
	}

	private void Start()
	{
		int prefsHighStreak = PlayerPrefs.GetInt (KEY_HIGH_STREAK, 0);
		GameManager.Instance.SetPointsThreshold(prefsHighStreak);

		//mainCoroutine = StartCoroutine (IntroCoroutine ());
		StartCoroutine (MainCoroutine ());
	}

	public void OnTouchBegin()
	{
		if (!isActive)
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

		lastState = PendingState.Tap;
	}

	public void OnHold()
	{
		if (!isActive)
			return;

		lastState = PendingState.Hold;
	}

	private IEnumerator LoadingCoroutine()
	{
		Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_LONG);
		Writer.WriteText ("...");

		yield return null;
	}

	private IEnumerator MainCoroutine()
	{
		GameManager.Instance.SetPoints (0);

		yield return StartCoroutine (WaitForSecondsOrBreak (1f));

		Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_SHORT);

		// intro message
		string greetingMessage = (GameManager.Instance.PointsThreshold > 0) ? 
			("Highest: " + GameManager.Instance.PointsThreshold) : ("Hello.");

		Writer.WriteText (greetingMessage + "\n[Tap] to continue\n[Hold] for leaderboard");
		yield return StartCoroutine (WaitForHoldOrBreak ());

		if(lastState == PendingState.Hold)
		{
			Writer.WriteText("Starting up leaderboard");
			
			yield return StartCoroutine(WaitForSecondsOrBreak(DURATION_STOP));
		}

		// instructions message
		string instructionsMessage = "[Tap] to add SPACE between words as they are typed";

		Writer.SetMode (TypeWriter.WriterMode.Normal);
		Writer.WriteText (instructionsMessage);

		yield return StartCoroutine(WaitForSecondsOrBreak(4f));

		while (true) 
		{
			Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_SHORT);

			// get a random phrase and generate a raw message from the phrase
			int phraseIndex = UnityEngine.Random.Range (0, Phrases.Length);
			Phrase randomPhrase = Phrases[phraseIndex];
			string rawMessage = Regex.Replace(randomPhrase.correctMessage, @"\s+", "");
			//Debug.Log (rawMessage + " | " + randomPhrase.correctMessage);
			string correctMessage = randomPhrase.correctMessage;
			int wordCount = randomPhrase.correctMessage.Split(' ').Length;

			Writer.SetMode(TypeWriter.WriterMode.Normal);
			Writer.WriteText (rawMessage + "\n" + wordCount + " words");

			yield return StartCoroutine(WaitForSecondsOrBreak(5f));
		
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

			Writer.SetTypeDuration(TypeWriter.TYPE_DURATION_SHORT);
			Writer.SetMode(TypeWriter.WriterMode.Normal);

			if(writeResult)
			{
				if(successSound != null)
					SoundManager.Instance.PlaySoundModulated(successSound, transform.position);
				
				GameManager.Instance.AddPoints(1);

				if(GameManager.Instance.PointsThreshold > 0)
					Writer.WriteText("Streak: " + GameManager.Instance.Points + "\nHighest: " + GameManager.Instance.PointsThreshold);
				else
					Writer.WriteText("Streak: " + GameManager.Instance.Points);

				yield return StartCoroutine(WaitForSecondsOrBreak(3f));
			}
			else
			{
				if(GameManager.Instance.Points > GameManager.Instance.PointsThreshold) 
				{
					GameManager.Instance.SetPointsThreshold(GameManager.Instance.Points);

					PlayerPrefs.SetInt(KEY_HIGH_STREAK, GameManager.Instance.PointsThreshold);
					PlayerPrefs.Save();
				}

				Writer.WriteText("Streak: " + GameManager.Instance.Points + 
				                 "\nHighest: " + GameManager.Instance.PointsThreshold + 
				                 "\n[Tap] to retry" +
				                 "\n[Hold] for leaderboard options");

				yield return StartCoroutine(WaitForHoldOrBreak());

				// time for a leaderboard?
				if(lastState == PendingState.Hold)
				{
					Writer.WriteText("Leaderboard Options\n" +
					                 "[Tap] to view\n" +
					                 "[Hold] to add\n" +
					                 "New streak: " + GameManager.Instance.Points + "\n");

					yield return StartCoroutine(WaitForHoldOrBreak());

					if(lastState == PendingState.Hold)
					{
						yield return StartCoroutine(MakeStreakCoroutine());
					}
				}

				GameManager.Instance.SetPoints(0);
			}
		}
	}

	private IEnumerator MakeStreakCoroutine()
	{
		int streakValue = GameManager.Instance.Points;

		Writer.WriteText("Streak: " + streakValue + "\n" +
		                 "[Tap] to cancel\n" +
		                 "[Hold] to submit\n" +
		                 "Enter name");

		yield return StartCoroutine (WaitForHoldOrBreak ());

		//ParseStreak streak = ParseObject.Create<ParseStreak> ();
		//streak.DisplayName = displayName;
		//streak.StreakValue = streakValue
	}

	private IEnumerator WaitForSecondsOrBreak(float duration)
	{
		lastState = PendingState.Listening;
		DateTime startTime = System.DateTime.UtcNow;
		TimeSpan waitDuration = TimeSpan.FromSeconds(duration);
		while (lastState != PendingState.Tap && lastState != PendingState.Neutral)
		{
			if(System.DateTime.UtcNow - startTime >= waitDuration)
			{
				lastState = PendingState.Neutral;
			}

			yield return null;
		}
	}

	private IEnumerator WaitForHoldOrBreak()
	{
		lastState = PendingState.Listening;
		while(lastState != PendingState.Hold && lastState != PendingState.Neutral && lastState != PendingState.Tap)
		{
			yield return null;
		}
	}
}