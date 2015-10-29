using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SelectionHandler
{
	private int selectedIndex;
	private string optionText;
	
	private List<string> optionLabels = new List<string>();
	
	public SelectionHandler(List<string> labels)
	{
		selectedIndex = 0;
		optionLabels.Clear ();
		optionLabels = labels;
		SelectNextText ();
	}
	
	public void Reset()
	{
		selectedIndex = 0;
	}
	
	public void Next()
	{
		selectedIndex = (selectedIndex + 1) % optionLabels.Count;
		optionText = "";
		SelectNextText ();
	}

	private void SelectNextText(){
		for (int i = 0; i < optionLabels.Count; i++)
		{
			if(i == selectedIndex) optionText += ("> " + optionLabels[i] + " <");
			else optionText += optionLabels[i];
			
			optionText += "\n";
		}
	}

	public string GetOptionListString()
	{

		return optionText;
	}

	public int GetSelectedIndex()
	{
		return selectedIndex;
	}
}