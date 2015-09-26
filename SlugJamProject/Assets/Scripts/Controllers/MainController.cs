using UnityEngine;
using System;
using System.Text.RegularExpressions;
using System.Collections;

public class MainController : Controller, InputManager.InputListener
{
	// type data
	public TypeWriter Writer;
	public Phrase[] Phrases = new Phrase[] {};

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
		Writer.AddSpace ();
	}

	public void OnEnter()
	{
		Writer.AddEnter ();

		isWaiting = false;

		if(inGameLoop == false)
		{
			Writer.WriteText("");
		}
	}

	private IEnumerator IntroCoroutine()
	{
		yield return StartCoroutine (WaitForSecondsOrBreak (1f));

		Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_SHORT);

		// intro message
		Writer.WriteText("Hello.");

		yield return StartCoroutine (WaitForSecondsOrBreak (3f));

		Writer.WriteText ("Press SPACE to fill in the gaps");

		yield return StartCoroutine(WaitForSecondsOrBreak(4f));

		while (true) {
			// countdown
			Writer.WriteTextInstant ("3");
			yield return StartCoroutine(WaitForSecondsOrBreak(1f));
			Writer.WriteTextInstant ("2");
			yield return StartCoroutine(WaitForSecondsOrBreak(1f));
			Writer.WriteTextInstant ("1");
			yield return StartCoroutine(WaitForSecondsOrBreak(1f));

			Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_LONG);

			// get a random phrase and generate a raw message from the phrase
			int phraseIndex = UnityEngine.Random.Range (0, Phrases.Length);
			Phrase randomPhrase = Phrases[phraseIndex];
			string rawMessage = Regex.Replace(randomPhrase.correctMessage, @"\s+", "");
			//Debug.Log (rawMessage + " | " + randomPhrase.correctMessage);
			string correctMessage = randomPhrase.correctMessage;

			// start writing raw message
			Writer.WriteText (rawMessage);

			// here we check the written message against the correct message
			while(Writer.isWriting)
			{
				inGameLoop = true;
				string writtenText = Writer.GetWrittenText();
				if(writtenText != correctMessage.Substring(0, Mathf.Min(correctMessage.Length, writtenText.Length)))
				{
					Writer.StopWriting();
					yield return StartCoroutine(WaitForSecondsOrBreak(2f));
					Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_SHORT);
					Writer.WriteText("You failed. Press ENTER");
					yield return StartCoroutine (WaitForSecondsOrBreak(999999f));
					//break;
				} else if (writtenText.Length == correctMessage.Length) 
				{
					Writer.StopWriting();
					yield return StartCoroutine(WaitForSecondsOrBreak(2f));
					Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_SHORT);
					Writer.WriteText("Nice job. Press ENTER");
					yield return StartCoroutine(WaitForSecondsOrBreak(999999f));
					//break;
				}
				
				yield return null;
			}

			inGameLoop = false;

			/*
			yield return new WaitForSeconds(1.5f);

			Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_SHORT);

			Writer.WriteText("Great job! Ready for more?");
			*/
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