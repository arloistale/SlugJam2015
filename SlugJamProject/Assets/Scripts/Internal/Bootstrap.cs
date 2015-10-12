using UnityEngine;

using Parse;

public class Bootstrap : MonoBehaviour 
{
	void Awake()
	{
		ParseUser.RegisterSubclass<ParseUser> ();
	}

	void Start()
	{
		Application.LoadLevel ("Entry");
	}
}