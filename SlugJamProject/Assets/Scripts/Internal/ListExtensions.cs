using System.Collections.Generic;

public static class ListExtensions
{
	public static void Shuffle<T>(this IList<T> shuffleList)
	{
		int count = shuffleList.Count;
		for (int i = 0; i < count; i++) 
		{
			var temp = shuffleList[i];
			int randomIndex = UnityEngine.Random.Range(i, count);
			shuffleList[i] = shuffleList[randomIndex];
			shuffleList[randomIndex] = temp;
		}
	}
}