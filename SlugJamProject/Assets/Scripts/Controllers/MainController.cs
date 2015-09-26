using UnityEngine;
using System.Text.RegularExpressions;
using System.Collections;

public class MainController : Controller, InputManager.InputListener
{
	// type data
	public TypeWriter Writer;
	public Phrase[] Phrases = new Phrase[] {};

	// coroutine data
	private Coroutine mainCoroutine;

	private bool inGameLoop = false;

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

		if (inGameLoop) 
		{
			Writer.AddSpace ();
			Writer.setTextToStatusColor (1);
		}
	}

	public void OnEnter()
	{
		//Debug.Log ("hey");
	}

	private IEnumerator IntroCoroutine()
	{
		yield return new WaitForSeconds (1f);

		Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_SHORT);

		// intro message
		Writer.WriteText("Hello. Welcome to this installation known as SpaceBar");

		yield return new WaitForSeconds(5f);

		Writer.WriteText ("Press SPACE to fill in the gaps");

		while (true) {
			yield return new WaitForSeconds (4f);

			// get a random phrase and generate a raw message from the phrase
			int phraseIndex = Random.Range (0, Phrases.Length);
			Phrase randomPhrase = Phrases[phraseIndex];
			string rawMessage = Regex.Replace(randomPhrase.correctMessage, @"\s+", "");
			//Debug.Log (rawMessage + " | " + randomPhrase.correctMessage);
			string correctMessage = randomPhrase.correctMessage;
			string theme = randomPhrase.messageTheme;
			int wordCount = randomPhrase.correctMessage.Split(' ').Length;

			Writer.WriteText (theme + "\n" + wordCount + " words");

			// countdown
			yield return new WaitForSeconds (4f);
			Writer.WriteTextInstant ("3");
			yield return new WaitForSeconds (1f);
			Writer.WriteTextInstant ("2");
			yield return new WaitForSeconds (1f);
			Writer.WriteTextInstant ("1");
			yield return new WaitForSeconds (1f);

			Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_LONG);

			// start writing raw message
			Writer.WriteText (rawMessage);

			// here we check the written message against the correct message
			while(Writer.isWriting)
			{
				inGameLoop = true;
				//Writer.setTextToStatusColor(1);
				string writtenText = Writer.GetWrittenText();
				if(writtenText != correctMessage.Substring(0, Mathf.Min(correctMessage.Length, writtenText.Length)))
				{
					Debug.Log ("Wrong");
					Writer.setTextToStatusColor(2);
					Writer.StopWriting();
					break;
				}

				yield return null;
			}
			inGameLoop = false;
			yield return new WaitForSeconds(1.5f);

			Writer.SetTypeDuration (TypeWriter.TYPE_DURATION_SHORT);

			Writer.WriteText("Great job! Ready for more?");
		}
	}
}