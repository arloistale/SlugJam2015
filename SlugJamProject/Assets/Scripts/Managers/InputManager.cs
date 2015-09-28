using UnityEngine;
using System.Collections;
using UnityStandardAssets.CrossPlatformInput;

/// <summary>
/// Modified from InputManager.cs from Corgi Engine.
/// This persistent singleton handles the inputs and sends commands to specified InputListener.
/// </summary>
public class InputManager : PersistentSingleton<InputManager>
{
	/// <summary>
	/// The input listener that commands are sent to.
	/// </summary>
	private static InputListener inputListener;
	
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
			inputListener.OnSpace();
		}

		if (CrossPlatformInputManager.GetButtonDown ("Submit")) 
		{
			inputListener.OnDoubleSpace();
		}

		if(Input.touchCount > 0)
		{
			Touch mainTouch = Input.touches[0];
			if (mainTouch.phase == TouchPhase.Began && mainTouch.tapCount == 1)
				inputListener.OnSpace();
		}
	}

	/// <summary>
	/// Sets the input listener.
	/// </summary>
	/// <param name="listener">Listener.</param>
	public void SetInputListener(InputListener listener) {
		inputListener = listener;
	}

	/// Interface between input commands and actions.
	public interface InputListener
	{
		// when SPACE action is tapped
		void OnSpace();

		// when SPACE action is double tapped
		void OnDoubleSpace();
	}
}
