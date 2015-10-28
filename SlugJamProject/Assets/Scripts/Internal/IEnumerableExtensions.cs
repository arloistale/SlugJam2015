using System.Collections.Generic;

public static class IEnumerableExtensions
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
	
	public static void AddRange<T>(this Queue<T> queue, IEnumerable<T> elements)
	{
		foreach (T obj in elements)
			queue.Enqueue(obj);
	}
}