﻿using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections;

public class MainController : Controller, InputManager.InputListener
{
	public TypeWriter Writer;

	public Phrase[] Phrases = new Phrase[] {};

	private Coroutine mainCoroutine;

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
		//Debug.Log ("hey");
	}

	private IEnumerator IntroCoroutine()
	{
		yield return new WaitForSeconds (1f);

		Writer.SetPauseDuration (0.05f);

		// intro message
		Writer.WriteText("Hello.");

		yield return new WaitForSeconds(3f);

		Writer.WriteText ("Press SPACE to fill in the gaps");

		while (true) {
			// countdown
			yield return new WaitForSeconds (4f);
			Writer.WriteTextInstant ("3");
			yield return new WaitForSeconds (1f);
			Writer.WriteTextInstant ("2");
			yield return new WaitForSeconds (1f);
			Writer.WriteTextInstant ("1");
			yield return new WaitForSeconds (1f);

			Writer.SetPauseDuration (1f);

			// get a random phrase and generate a raw message from the phrase
			int phraseIndex = Random.Range (0, Phrases.Length);
			Phrase randomPhrase = Phrases[phraseIndex];
			string rawMessage = Regex.Replace(randomPhrase.correctMessage, @"\s+", "");
			//Debug.Log (rawMessage + " | " + randomPhrase.correctMessage);
			string correctMessage = randomPhrase.correctMessage;

			// start writing raw message
			Writer.WriteText (rawMessage);

			// here we check the written message against the correct message
			while(Writer.isWriting)
			{
				string writtenText = Writer.GetWrittenText();
				if(writtenText != correctMessage.Substring(0, Mathf.Min(correctMessage.Length, writtenText.Length)))
				{
					Debug.Log ("Wrong");
					Writer.StopWriting();
					break;
				}

				yield return null;
			}

			yield return new WaitForSeconds(1.5f);

			Writer.SetPauseDuration (0.05f);

			Writer.WriteText("Great job! Ready for more?");
		}
	}
}