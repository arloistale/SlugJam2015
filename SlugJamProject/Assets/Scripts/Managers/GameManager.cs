using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Parse;

/// <summary>
/// Modified from GameManager.cs from Corgi Engine.
/// The game manager is a persistent singleton that handles points and game state
/// </summary>
public class GameManager : PersistentSingleton<GameManager>
{
	/// the current number of game points
	public int Points { get; private set; }
	/// the current points threshold (this requires at least one LevelGate to be present)
	public int PointsThreshold { get; private set; }
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
		Points = 0;
	}
	
	/// <summary>
	/// Adds the points in parameters to the current game points.
	/// </summary>
	public void AddPoints(int pointsToAdd)
	{
		Points = Mathf.Max(0, pointsToAdd + Points);
	}
	
	/// <summary>
	/// use this to set the current points to the one you pass as a parameter
	/// </summary>
	/// <param name="points">Points.</param>
	public void SetPoints(int points)
	{
		Points = points;
	}
	
	public void SetPointsThreshold(int pointsThreshold)
	{
		PointsThreshold = pointsThreshold;
	}
}
