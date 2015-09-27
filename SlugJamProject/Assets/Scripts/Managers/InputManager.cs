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

		/*
		if ( CrossPlatformInputManager.GetButtonDown("Cancel") )
			GameManager.Instance.Pause();
		
		if (GameManager.Instance.Paused)
			return;	
		
		// if the player can't move for some reason, we do nothing else
		if (!GameManager.Instance.CanMove)
			return;
		*/
		if (CrossPlatformInputManager.GetButtonDown ("Space")) 
		{
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
		// when SPACE action is pressed
		void OnSpace();
	}
}
