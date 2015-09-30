using UnityEngine;

using Parse;

public class Bootstrap : MonoBehaviour 
{
	void Awake()
	{
		ParseObject.RegisterSubclass<ParseStreak> ();
	}

	void Start()
	{
		Application.LoadLevel ("Entry");
	}
}