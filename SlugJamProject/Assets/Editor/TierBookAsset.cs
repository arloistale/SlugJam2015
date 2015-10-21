using UnityEditor;

public class TierBookAsset
{
	[MenuItem("Assets/Create/Tier Book")]
	public static void CreateAsset ()
	{
		ScriptableObjectUtility.CreateAsset<TierBook> ();
	}
}