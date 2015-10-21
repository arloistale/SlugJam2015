using UnityEditor;

public class PhraseBookAsset
{
	[MenuItem("Assets/Create/Phrase Book")]
	public static void CreateAsset ()
	{
		ScriptableObjectUtility.CreateAsset<PhraseBook> ();
	}
}