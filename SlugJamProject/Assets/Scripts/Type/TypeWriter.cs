using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TypeWriter : MonoBehaviour 
{
	public enum WriterMode
	{
		Normal,
		CullSpaces
	}

	// constants
	public const float TYPE_DURATION_SHORT = 0.05f;
	public const float TYPE_DURATION_LONG = 0.5f;
	public const float TYPE_DURATION_MEDIUM = 0.15f;

	// external data
	public AudioClip[] TypeSounds = new AudioClip[] {};
	public AudioClip TypeSoundSpace;

	// UI
	public Text TypeText;

	// coroutine data
	private Coroutine typeCoroutine;

	// type data
	private WriterMode writerMode;

	private float typePauseDuration = 0.2f;
	private string typeMessage;
	private int typeIndex;

	public bool isWriting { get; private set; }

	public void SetMode(WriterMode mode)
	{
		writerMode = mode;
	}

	public WriterMode GetMode()
	{
		return writerMode;
	}

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

	public void WriteText(string message)
	{
		TypeText.text = "";
		typeMessage = message;

		if (typeCoroutine != null)
			StopCoroutine (typeCoroutine);

		typeCoroutine = StartCoroutine (WriteTextCoroutine ());
	}

	public void ClearWriting()
	{
		TypeText.text = "";
	}

	public void StopWriting()
	{
		if (typeCoroutine != null)
			StopCoroutine (typeCoroutine);

		typeMessage = null;
		isWriting = false;
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

	public void SetTypeDuration(float duration)
	{
		typePauseDuration = duration;
	}

	public void setTextToStatusColor(int status)
	{
		if (status == 0)
			TypeText.color = Color.black;
		if (status == 1) 
			TypeText.color = Color.green;
		if (status == 2) 
			TypeText.color = Color.red;
	}

	private IEnumerator WriteTextCoroutine () 
	{
		typeIndex = 0;
		char[] typeMessageChars = typeMessage.ToCharArray ();

		isWriting = true;

		for (; typeIndex < typeMessageChars.Length; typeIndex++) {
			char letter = typeMessageChars [typeIndex];
			if (letter != ' ' || writerMode == WriterMode.Normal)
			{
				TypeText.text += letter;
				setTextToStatusColor(0);
			}

			if (TypeSounds != null) 
			{
				int soundIndex = Random.Range(0, TypeSounds.Length);
				SoundManager.Instance.PlaySound (TypeSounds[soundIndex], transform.position);
			}

			yield return new WaitForSeconds (typePauseDuration);
		}

		isWriting = false;
	}
}