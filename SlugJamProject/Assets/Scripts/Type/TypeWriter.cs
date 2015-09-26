using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TypeWriter : MonoBehaviour 
{
	// constants
	public const float TYPE_DURATION_SHORT = 0.05f;
	public const float TYPE_DURATION_LONG = 1f;

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

	/*void OnGUI()
	{
		GUIStyle style = new GUIStyle ();
		style.richText = true;
		if(TypeText.text.Length > 1) GUILayout.Label("<color=black>" + TypeText.text[1].ToString() + "</color>",style);
	}*/

	private IEnumerator WriteTextCoroutine () 
	{
		typeIndex = 0;
		char[] typeMessageChars = typeMessageRaw.ToCharArray ();

		isWriting = true;

		for(; typeIndex < typeMessageChars.Length; typeIndex++)
		{
			char letter = typeMessageChars[typeIndex];
			TypeText.text += letter;
			setTextToStatusColor(0);

			if (TypeSound != null)
				SoundManager.Instance.PlaySound(TypeSound, transform.position);

			yield return new WaitForSeconds (typePauseDuration);
		}
		isWriting = false;
	}
}