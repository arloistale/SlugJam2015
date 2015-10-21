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

	public enum TypeStatus
	{
		Normal,
		Success,
		Error
	}

	// constants
	public const float TYPE_DURATION_SHORT = 0.05f;
	public const float TYPE_DURATION_LONG = 0.5f;
	public const float TYPE_DURATION_MEDIUM = 0.1f;

	// external data
	public AudioClip[] TypeSounds = new AudioClip[] {};
	public AudioClip TypeSoundSpace;

	// UI
	public Text TypeText;

	// coroutine data
	private Coroutine typeCoroutine;

	// type data
	private WriterMode writerMode;
	private string currTypeMessage;
	private string finalMessage;
	private float typePauseDuration = 0.2f;

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
		{
			StopCoroutine (typeCoroutine);
			typeCoroutine = null;
		}

		if (TypeSoundSpace != null) {
			SoundManager.Instance.PlaySound (TypeSoundSpace, transform.position);
		}

		currTypeMessage = message;
		TypeText.text = currTypeMessage;
	}

	public void AppendTextInstant(string message)
	{
		if (typeCoroutine != null) 
		{
			StopCoroutine (typeCoroutine);
			typeCoroutine = null;
		}
		
		if (TypeSoundSpace != null) 
		{
			SoundManager.Instance.PlaySound (TypeSoundSpace, transform.position);
		}
		
		TypeText.text += message;
	}

	public void WriteText(string message)
	{
		ClearWriting ();

		if (typeCoroutine != null) 
		{
			StopCoroutine (typeCoroutine);
			typeCoroutine = null;
		}

		typeCoroutine = StartCoroutine (WriteTextCoroutine (message));
	}

	public void RepeatText(string message)
	{
		ClearWriting ();
		
		if (typeCoroutine != null) 
		{
			StopCoroutine (typeCoroutine);
			typeCoroutine = null;
		}
		
		typeCoroutine = StartCoroutine (RepeatTextCoroutine (message));
	}

	public void ClearWriting()
	{
		currTypeMessage = "";
		TypeText.text = "";
	}

	public void StopWriting()
	{
		if (typeCoroutine != null) 
		{
			StopCoroutine (typeCoroutine);
			typeCoroutine = null;
		}

		isWriting = false;
	}

	public string GetWrittenText() 
	{
		return currTypeMessage;
	}

	public void AddSpace() 
	{
		if(TypeSoundSpace != null)
			SoundManager.Instance.PlaySound (TypeSoundSpace, transform.position);

		if(currTypeMessage != finalMessage)
			currTypeMessage += ' ';

		TypeText.text = currTypeMessage;
	}

	public void SetTypeDuration(float duration)
	{
		typePauseDuration = duration;
	}

	public void SetTextStatusColor(TypeStatus status)
	{
		switch (status) 
		{
			case TypeStatus.Normal:
				TypeText.color = Color.black;
				break;
			case TypeStatus.Success:
				TypeText.color = Color.green;
				break;
			case TypeStatus.Error:
				TypeText.color = Color.red;
				break;
		}
	}

	private IEnumerator WriteTextCoroutine (string typeMessage) 
	{
		finalMessage = typeMessage;
		currTypeMessage = "";

		int typeIndex = 0;
		char[] typeMessageChars = typeMessage.ToCharArray ();

		isWriting = true;

		for (; typeIndex < typeMessageChars.Length; typeIndex++) 
		{
			char letter = typeMessageChars [typeIndex];
			if (letter != ' ' || writerMode == WriterMode.Normal)
			{
				currTypeMessage += letter;
				SetTextStatusColor(TypeStatus.Normal);
			}

			TypeText.text = currTypeMessage + "|";

			if (TypeSounds != null) 
			{
				int soundIndex = Random.Range(0, TypeSounds.Length);
				SoundManager.Instance.PlaySound (TypeSounds[soundIndex], transform.position);
			}

			yield return new WaitForSeconds (typePauseDuration);
		}

		TypeText.text = currTypeMessage + "|";

		isWriting = false;
	}

	private IEnumerator RepeatTextCoroutine (string typeMessage) 
	{
		int typeIndex;
		char[] typeMessageChars = typeMessage.ToCharArray ();
		
		isWriting = true;
		
		while (isWriting) 
		{
			typeIndex = 0;

			for (; typeIndex < typeMessageChars.Length; typeIndex++)
			{
				char letter = typeMessageChars [typeIndex];
				if (letter != ' ' || writerMode == WriterMode.Normal)
				{
					TypeText.text += letter;
					SetTextStatusColor(TypeStatus.Normal);
				}
				
				if (TypeSounds != null) 
				{
					int soundIndex = Random.Range (0, TypeSounds.Length);
					SoundManager.Instance.PlaySound (TypeSounds [soundIndex], transform.position);
				}
				
				yield return new WaitForSeconds (typePauseDuration);
			}

			yield return new WaitForSeconds (typePauseDuration);

			ClearWriting();

			yield return new WaitForSeconds (typePauseDuration);
		}
	}
}