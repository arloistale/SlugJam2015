﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TypeWriter : MonoBehaviour 
{
	// constants
	public const float TYPE_DURATION_SHORT = 0.05f;
	public const float TYPE_DURATION_LONG = 1f;

	// external data
	public AudioClip[] TypeSounds = new AudioClip[] {};
	public AudioClip TypeSoundSpace;

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

		if (TypeSounds != null) {
			int soundIndex = Random.Range(0, TypeSounds.Length);
			SoundManager.Instance.PlaySound (TypeSounds[soundIndex], transform.position);
		}

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
		if(TypeSoundSpace != null)
			SoundManager.Instance.PlaySound (TypeSoundSpace, transform.position);

		TypeText.text += " ";
	}

	public void AddEnter()
	{
		if(TypeSoundSpace != null)
			SoundManager.Instance.PlaySound (TypeSoundSpace, transform.position);
	}

	public void SetTypeDuration(float duration)
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

			if (TypeSounds != null) {
				int soundIndex = Random.Range(0, TypeSounds.Length);
				SoundManager.Instance.PlaySound (TypeSounds[soundIndex], transform.position);
			}

			yield return new WaitForSeconds (typePauseDuration);
		}

		isWriting = false;
	}
}