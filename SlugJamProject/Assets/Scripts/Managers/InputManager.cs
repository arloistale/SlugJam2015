using UnityEngine;
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

	public bool isHolding;
	private bool didExpendHold;
	private float holdDuration;

	void Start()
	{
		Reset ();
	}

	void OnApplicationPause()
	{
		Reset ();
	}

	void OnApplicationFocus()
	{
		Reset ();
	}

	void Enable()
	{
		Reset ();
	}

	void Disable()
	{
		Reset ();
	}
	
	/// <summary>
	/// At update, we check the various commands and send them to the input listener.
	/// </summary>
	void Update()
	{
		// assert that there is an input listener
		if (inputListener == null)
			return;

		if (isHolding) 
		{
			if (!didExpendHold && holdDuration >= DURATION_HOLD) 
			{
				didExpendHold = true;
				inputListener.OnHold ();
			}

			holdDuration += Time.deltaTime;
		}

		if (CrossPlatformInputManager.GetButtonDown ("Space")) 
		{
			if(!isHolding)
				OnTouchBegin();
		}
		else if (CrossPlatformInputManager.GetButtonUp ("Space")) 
		{
			if(isHolding)
				OnTouchEnd();
		}

		if(Input.touchCount > 0)
		{
			for(int i = 0; i < Input.touches.Length; i++)
			{
				if (Input.touches[i].tapCount == 1)
				{
					Touch mainTouch = Input.touches[i];
					if(mainTouch.phase == TouchPhase.Began)
					{
						if(!isHolding)
							OnTouchBegin();
					}
					else if(mainTouch.phase == TouchPhase.Ended)
					{
						if(isHolding)
							OnTouchEnd();
					}
					break;
				}
			}
		}
	}
	
	public void Reset()
	{
		isHolding = false;
		holdDuration = 0f;
		didExpendHold = false;
	}

	public void EmulateTouchBegin()
	{
		OnTouchBegin ();
	}

	private void OnTouchBegin()
	{
		holdDuration = 0f;
		didExpendHold = false;
		isHolding = true;
		inputListener.OnTouchBegin ();
	}

	private void OnTouchEnd()
	{
		isHolding = false;
		
		Debug.Log (holdDuration);
		if(holdDuration < DURATION_HOLD)
		{
			inputListener.OnTap();
		}

		Reset ();
	}

	/// <summary>
	/// Sets the input listener.
	/// </summary>
	/// <param name="listener">Listener.</param>
	public void SetInputListener(InputListener listener) 
	{
		inputListener = listener;
	}

	/// Interface between input commands and actions.
	public interface InputListener
	{
		// when touch begins, always called
		void OnTouchBegin();

		// when tapped, only called if held less than HOLD_DURATION time
		void OnTap();

		// called when held time exceeds HOLD_DURATION time
		void OnHold();
	}
}
