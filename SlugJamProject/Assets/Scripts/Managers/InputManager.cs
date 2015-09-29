﻿using UnityEngine;
using System.Collections;
using UnityStandardAssets.CrossPlatformInput;

/// <summary>
/// Modified from InputManager.cs from Corgi Engine.
/// This persistent singleton handles the inputs and sends commands to specified InputListener.
/// </summary>
public class InputManager : PersistentSingleton<InputManager>
{
	private const float DURATION_HOLD = 0.5f;

	/// <summary>
	/// The input listener that commands are sent to.
	/// </summary>
	private static InputListener inputListener;

	// coroutine data
	private Coroutine holdCoroutine;
	
	/// <summary>
	/// At update, we check the various commands and send them to the input listener.
	/// </summary>
	void Update()
	{
		// assert that there is an input listener
		if (inputListener == null) 
		{
			return;
		}

		if (CrossPlatformInputManager.GetButtonDown ("Space")) 
		{
			inputListener.OnTapBegin ();
		}

		if (CrossPlatformInputManager.GetButtonUp ("Space")) 
		{
			inputListener.OnTapEnd();
		}

		if (CrossPlatformInputManager.GetButtonDown ("Submit")) 
		{
			inputListener.OnTapHold();
		}

		if(Input.touchCount > 0)
		{
			Touch mainTouch = Input.touches[0];
			if (mainTouch.tapCount == 1)
			{
				if(mainTouch.phase == TouchPhase.Began)
				{
					inputListener.OnTapBegin();

					holdCoroutine = StartCoroutine(HoldCoroutine());
				}
				else if(mainTouch.phase == TouchPhase.Ended)
				{
					inputListener.OnTapEnd();

					if(holdCoroutine != null)
						StopCoroutine(holdCoroutine);
				}
			}
		}
	}

	/// <summary>
	/// Sets the input listener.
	/// </summary>
	/// <param name="listener">Listener.</param>
	public void SetInputListener(InputListener listener) {
		inputListener = listener;
	}

	private IEnumerator HoldCoroutine()
	{
		yield return new WaitForSeconds (DURATION_HOLD);

		Debug.Log ("Calling hold!");
		inputListener.OnTapHold ();
	}

	/// Interface between input commands and actions.
	public interface InputListener
	{
		// when tap begins
		void OnTapBegin();

		// when tap ends
		void OnTapEnd();

		// when SPACE action is double tapped
		void OnTapHold();
	}
}
