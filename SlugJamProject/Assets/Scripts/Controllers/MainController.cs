using UnityEngine;
using System;
using System.Text.RegularExpressions;
using System.Collections;

using Parse;

public class MainController : Controller, InputManager.InputListener
{
	// constants
	private const string KEY_HIGH_STREAK = "High Streak";

	private const float DURATION_STOP = 999999f;

	// type data
	public TypeWriter Writer;
	public Phrase[] Phrases = new Phrase[] {};
	public AudioClip errorSound;
	public AudioClip successSound;

	// coroutine data
	private Coroutine waitCoroutine;
	//private Coroutine mainCoroutine;

	// internal data
	private bool isWaiting;

	protected override void Awake()
	{
		base.Awake ();

		InputManager.Instance.SetInputListener (this);
	}

	private void Start()
	{
		//mainCoroutine = StartCoroutine (IntroCoroutine ());
		StartCoroutine (MainCoroutine ());

		int prefsHighStreak = PlayerPrefs.GetInt (KEY_HIGH_STREAK, 0);
		GameManager.Instance.SetPointsThreshold(prefsHighStreak);
	}

	public void OnTapBegin()
	{
		if (!isActive)
			return;

		if (Writer.GetMode () == TypeWriter.WriterMode.CullSpaces && Writer.isWriting) 
		{
			Writer.setTextToStatusColor (1);
			Writer.AddSpace ();
		}
	}

	public void OnTapEnd()
	{
		if (!isActive)
			return;

		isWaiting = false;
	}

	public void OnTapHold()
	{
		Debug.Log ("Saved");
	}

	private void MakeStreak(string displayName, int value)
	{
		ParseStreak streak = ParseObject.Create<ParseStreak> ();
		streak.DisplayName = displayName;
		streak.StreakValue = value;
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
		if (GameManager.Instance.PointsThreshold == 0)
		{
			Writer.WriteText ("Hello.\nTap to continue\nHold to view leaderboard");
			yield return StartCoroutine (WaitForSecondsOrBreak (DURATION_STOP));
		}
		else
		{
			Writer.WriteText("Highest: " + GameManager.Instance.PointsThreshold + "\nTap to continue\nHold to view leaderboard");
			yield return StartCoroutine (WaitForSecondsOrBreak (DURATION_STOP));
		}

		string instructionsMessage = "Tap to add SPACE between words as they are typed";

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

					Writer.setTextToStatusColor(2);
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
				                 "\nTap to retry" +
				                 "\nHold to add to leaderboard");

				GameManager.Instance.SetPoints(0);
				yield return StartCoroutine(WaitForSecondsOrBreak(DURATION_STOP));
			}
		}
	}

	private IEnumerator WaitForSecondsOrBreak(float duration)
	{
		isWaiting = true;
		DateTime startTime = System.DateTime.UtcNow;
		TimeSpan waitDuration = TimeSpan.FromSeconds(duration);
		while (isWaiting)
		{
			if(System.DateTime.UtcNow - startTime >= waitDuration)
			{
				isWaiting = false;
			}

			yield return null;
		}
	}
}