using UnityEngine;

using Parse;

public class Bootstrap : MonoBehaviour 
{
	void Awake()
	{
		ParseObject.RegisterSubclass<ParsePhrase>();
	}

	void Start()
	{
		Application.LoadLevel ("Entry");
	}
}