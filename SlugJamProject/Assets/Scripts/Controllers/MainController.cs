using UnityEngine;
using System;
using System.Text.RegularExpressions;
using System.Collections;

public class MainController : Controller, InputManager.InputListener
{
	// type data
	public TypeWriter Writer;
	public Phrase[] Phrases = new Phrase[] {};
	public AudioClip errorSound;
	public AudioClip successSound;

	// coroutine data
	private Coroutine mainCoroutine;

	// internal data
	private bool inGameLoop = false;
	private bool isWaiting = false;

	protected override void Awake()
	{
		base.Awake ();

		InputManager.Instance.SetInputListener (this);
	}

	private void Start()
	{
		mainCoroutine = StartCoroutine (IntroCoroutine ());
	}

	public void OnSpace()
	{
		if (!isActive)
			return;

		Writer.AddSpace ();

		isWaiting = false;
		
		if(inGameLoop == false)
		{
			Writer.WriteText("");
		}
	}

	private IEnumerator IntroCoroutine()
	{
		GameManager.Instance.SetPoints (0);

		yield return StartCoroutine (WaitForSecondsOrBreak (1f));

		Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_SHORT);

		// intro message
		Writer.WriteText("Hello.");

		yield return StartCoroutine (WaitForSecondsOrBreak (3f));

		Writer.WriteText ("Press SPACE to fill in the gaps");

		yield return StartCoroutine(WaitForSecondsOrBreak(4f));

		while (true) {
			Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_SHORT);

			// get a random phrase and generate a raw message from the phrase
			int phraseIndex = UnityEngine.Random.Range (0, Phrases.Length);
			Phrase randomPhrase = Phrases[phraseIndex];
			string rawMessage = Regex.Replace(randomPhrase.correctMessage, @"\s+", "");
			//Debug.Log (rawMessage + " | " + randomPhrase.correctMessage);
			string correctMessage = randomPhrase.correctMessage;
			string theme = randomPhrase.messageTheme;
			int wordCount = randomPhrase.correctMessage.Split(' ').Length;

			Writer.WriteText (rawMessage + "\n" + wordCount + " words");

			yield return StartCoroutine(WaitForSecondsOrBreak(5f));

			// countdown
			Writer.WriteTextInstant ("3");
			yield return StartCoroutine(WaitForSecondsOrBreak(1f));
			Writer.WriteTextInstant ("2");
			yield return StartCoroutine(WaitForSecondsOrBreak(1f));
			Writer.WriteTextInstant ("1");
			yield return StartCoroutine(WaitForSecondsOrBreak(1f));

			Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_MEDIUM);
			
			// start writing raw message
			Writer.WriteText (rawMessage);

			bool writeResult = true;

			// here we check the written message against the correct message
			while(Writer.isWriting)
			{
				inGameLoop = true;
				string writtenText = Writer.GetWrittenText();
				if(writtenText != correctMessage.Substring(0, Mathf.Min(correctMessage.Length, writtenText.Length)))
				{
					Writer.StopWriting();
					writeResult = false;
				}
				
				yield return null;
			}

			if(writeResult)
				GameManager.Instance.AddPoints(1);
			else
				GameManager.Instance.SetPoints(0);

			Writer.SetTypeDuration(TypeWriter.TYPE_DURATION_SHORT);
			Writer.WriteText("Streak: " + GameManager.Instance.Points);
			yield return StartCoroutine(WaitForSecondsOrBreak(2f));

			inGameLoop = false;
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