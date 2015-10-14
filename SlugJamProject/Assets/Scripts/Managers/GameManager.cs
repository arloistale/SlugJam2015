using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Parse;

/// <summary>
/// Modified from GameManager.cs from Corgi Engine.
/// The game manager is a persistent singleton that handles points and game state
/// </summary>
public class GameManager : PersistentSingleton<GameManager>
{
	/// the streak value
	public int Streak { get; private set; }
	/// the high streak value
	public int HighStreak { get; private set; }
	// the current controller
	public MainController Controller { get; set; }
	/// whether the game is online or offline
	public bool IsOnline { get { return ParseUser.CurrentUser != null; } } 

	public override void Awake()
	{
		base.Awake ();

		GameObject controllerObject = GameObject.FindGameObjectWithTag ("MainController");
		Controller = controllerObject.GetComponent<MainController> ();
	}
	
	/// <summary>
	/// this method resets the whole game manager
	/// </summary>
	public void Reset()
	{
		Streak = 0;
	}
	
	/// <summary>
	/// Adds specified amount to the streak.
	/// </summary>
	public void AddStreak(int value)
	{
		Streak = Mathf.Max(0, value + Streak);
	}
	
	/// <summary>
	/// Sets the streak.
	/// </summary>
	public void SetStreak(int value)
	{
		Streak = value;
	}

	/// <summary>
	/// Sets the high streak.
	/// </summary>
	public void SetHighStreak(int value)
	{
		HighStreak = value;
	}
}
