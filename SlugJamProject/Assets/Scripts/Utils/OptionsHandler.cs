using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OptionsHandler
{
	private int selectedIndex;
	private string optionText;
	
	private List<string> optionLabels = new List<string>();
	
	public OptionsHandler()
	{
		selectedIndex = 0;
		Next ();
	}
	
	public void FeedOptions(List<string> labels)
	{
		Reset ();
		optionLabels.Clear ();
		optionLabels = labels;
	}
	
	public void Reset()
	{
		selectedIndex = 0;
	}
	
	public void Next()
	{
		selectedIndex = (selectedIndex + 1) % optionLabels.Count;

		for (int i = 0; i < optionLabels.Count; i++)
		{

		}
	}
/*
	private string GetOptionsStr()
	{
		if (GameManager.Instance.IsOnline) 
		{
			optionsIndex = optionIndex;
			
			if (optionsIndex > 2) 
			{
				optionsIndex = 0;
			}
			
			switch (optionsIndex) 
			{
			case 0:
				optionText = ">Continue<\nLeaderboard\nOptions";
				break;
			case 1:
				optionText = "Continue\n>Leaderboard<\nOptions";
				break;
			case 2:
				optionText = "Continue\nLeaderboard\n>Options<";
				break;
			default:
				optionText = ">Continue<\nLeaderboard\nOptions";
				break;
			}
		}
		else 
		{
			optionText = "\n[Tap] to continue\n[Hold] for start screen";
		}
	}*/

	public string GetOptionLabel(int index)
	{
		return optionLabels [index];
	}

	public int GetSelectedIndex()
	{
		return selectedIndex;
	}
}