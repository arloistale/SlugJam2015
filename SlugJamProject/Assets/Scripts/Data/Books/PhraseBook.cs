using UnityEngine;
using System.Collections.Generic;

public class PhraseBook : ScriptableObject
{
	public List<Phrase> PhraseList = new List<Phrase>();

	public Phrase GetPhraseWithWordLimit(int limit)
	{
		if (GameManager.Instance.Tier.TierCustomMessage.Length > 0) 
		{
			return new Phrase() { CorrectMessage = GameManager.Instance.Tier.TierCustomMessage };
		}

		int phraseIndex = (GameManager.Instance.HighStreak > 0) ? Random.Range (1, PhraseList.Count) : 0;
		Phrase filterPhrase = PhraseList[phraseIndex];
		while (filterPhrase.CorrectMessage.Split(' ').Length > limit)
		{
			phraseIndex = (GameManager.Instance.HighStreak > 0) ? Random.Range (1, PhraseList.Count) : 0;
			filterPhrase = PhraseList[phraseIndex];
		}

		return filterPhrase;
	}
}