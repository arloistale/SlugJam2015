﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TypeWriter : MonoBehaviour 
{
	// external data
	public AudioClip TypeSound;

	// UI
	public Text TypeText;

	// internal data
	private Coroutine typeCoroutine;

	// type data
	private float typePauseDuration = 0.2f;

	private string typeMessageRaw;
	private int typeIndex;

	public bool isWriting { get; private set; }

	public void WriteTextInstant(string message)
	{
		if (typeCoroutine != null)
			StopCoroutine (typeCoroutine);

		TypeText.text = message;
	}

	public void WriteText(string messageRaw)
	{
		TypeText.text = "";
		typeMessageRaw = messageRaw;

		if (typeCoroutine != null)
			StopCoroutine (typeCoroutine);

		typeCoroutine = StartCoroutine (WriteTextCoroutine ());
	}

	public void StopWriting()
	{
		if (typeCoroutine != null)
			StopCoroutine (typeCoroutine);
	}

	public string GetWrittenText() 
	{
		return TypeText.text;
	}

	public void AddSpace() 
	{
		TypeText.text += " ";
	}

	public void SetPauseDuration(float duration)
	{
		typePauseDuration = duration;
	}

	private IEnumerator WriteTextCoroutine () 
	{
		typeIndex = 0;
		char[] typeMessageChars = typeMessageRaw.ToCharArray ();

		isWriting = true;

		for(; typeIndex < typeMessageChars.Length; typeIndex++)
		{
			char letter = typeMessageChars[typeIndex];
			TypeText.text += letter;

			if (TypeSound != null)
				GetComponent<AudioSource>().PlayOneShot (TypeSound);

			yield return new WaitForSeconds (typePauseDuration);
		}

		isWriting = false;
	}
}