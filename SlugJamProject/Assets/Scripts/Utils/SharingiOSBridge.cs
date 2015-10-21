using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class SharingiOSBridge
{
	#if UNITY_IPHONE
	
	[DllImport("__Internal")]
	private static extern void _TAG_ShareTextWithImage (string iosPath, string message);

	public static void ShareTextWithImage (string imagePath, string message)
	{
		_TAG_ShareTextWithImage (imagePath, message);
	}
	
	#endif
}