using UnityEngine;

using Parse;

public class Bootstrap : MonoBehaviour 
{
	void Awake()
	{
		ParseObject.RegisterSubclass<Streak> ();
	}

	void Start()
	{
		Application.LoadLevel ("Entry");
	}
}