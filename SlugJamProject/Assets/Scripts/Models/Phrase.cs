using System;
using UnityEngine;
using System.Collections;

[Serializable]
public class Phrase
{
	/// <summary>
	/// The correct phrase message that is checked against the player.
	/// The phrase is scrapped of spaces in order to create the raw message fed to the player.
	/// </summary>
	public string correctMessage = "Default phrase message correct";
}