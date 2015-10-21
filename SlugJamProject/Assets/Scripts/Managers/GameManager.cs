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
	/// current tier
	public Tier Tier { get; set; }
	/// the streak value
	public int Streak { get; set; }
	/// the high streak value
	public int HighStreak { get; set; }
	/// the daily streak value
	public int DailyStreak { get; set; }
	/// the daily streak timestamp
	public DateTime DailyTimestamp { get; set; }
	// the current controller
	public MainController Controller { get; set; }
	/// whether the game is online or offline
	public bool IsOnline { get; set; } 

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
		HighStreak = 0;
		DailyStreak = 0;
		DailyTimestamp = DateTime.Now;
	}
}
