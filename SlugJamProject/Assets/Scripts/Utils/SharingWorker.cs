#pragma warning disable 0168 // variable declared but not used.
#pragma warning disable 0219 // variable assigned but not used.

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;

public class SharingWorker : MonoBehaviour
{
	public TypeWriter SharingWriter;

	public bool isSharing { get; private set; }
	
	/// <summary>
	/// Share the current screen on the mobile device.
	/// First snaps a screenshot then saves locally
	/// When the screenshot is saved then the 
	/// </summary>
	public void Share()
	{
		StartCoroutine (SaveCoroutine ());
	}

	private IEnumerator SaveCoroutine ()
	{
		// wait for graphics to render
		yield return new WaitForEndOfFrame();

		isSharing = true;
		string oldWriterMessage = SharingWriter.GetWrittenText();

		#if UNITY_ANDROID || UNITY_IPHONE

		string destination = DateTime.Now.ToString ("yyyy-MM-dd-HHmmss") + ".png";
		string fullDestination = Path.Combine (Application.persistentDataPath, destination);

		Application.CaptureScreenshot (destination);

		SharingWriter.WriteTextInstant ("Preparing...");
		
		FileInfo fileInfo = new FileInfo(fullDestination);
		while(fileInfo == null || fileInfo.Exists == false)
		{
			yield return null;
			fileInfo = new FileInfo(fullDestination);
		}

		#endif

		#if UNITY_ANDROID

		yield return StartCoroutine(ShareCoroutineAndroid(destination, fullDestination));

		#elif UNITY_IPHONE

		yield return StartCoroutine(ShareCoroutineIPhone(destination, fullDestination));

		#endif

		isSharing = false;
		SharingWriter.WriteTextInstant (oldWriterMessage);

		yield return null;
	}

	private IEnumerator ShareCoroutineAndroid(string destination, string fullDestination)
	{
		#if UNITY_ANDROID
		
		AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
		AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
		AndroidJavaClass fileClass = new AndroidJavaClass("java.io.File");
		AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
		
		AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("parse", "file://" + fullDestination);

		AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");
		intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
		intentObject.Call<AndroidJavaObject>("setType", "image/jpeg");
		intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), "Check out my streak in S.pace!");
		intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"), uriObject);

		AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");

		// display the chooser
		AndroidJavaObject jChooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "Choose sharing method");
		currentActivity.Call("startActivity", jChooser);

		#endif

		yield return null;
	}

	private IEnumerator ShareCoroutineIPhone(string destination, string fullDestination)
	{
		#if UNITY_IPHONE || UNITY_IPAD

		InputManager.Instance.Reset();
		SharingiOSBridge.ShareTextWithImage (fullDestination);

		#endif

		yield return null;
	}
}
