using UnityEngine;
using System.Collections;
/// <summary>
/// Modified from Corgi Engine SoundManager.cs
/// This persistent singleton handles sound playing
/// </summary>

public class SoundManager : PersistentSingleton<SoundManager>
{	
	/// true if the music is enabled	
	public bool MusicOn=true;
	/// true if the sound fx are enabled
	public bool SfxOn=true;
	/// the music volume
	[Range(0,1)]
	public float MusicVolume=0.3f;
	
	private AudioSource _backgroundMusic;
	
	/// <summary>
	/// Plays a sound
	/// </summary>
	/// <returns>An audiosource</returns>
	/// <param name="Sfx">The sound clip you want to play.</param>
	/// <param name="Location">The location of the sound.</param>
	/// <param name="Volume">The volume of the sound.</param>
	public AudioSource PlaySound(AudioClip sfx, float sfxVolume = 1f)
	{
		if (!SfxOn)
			return null;
		// we create a temporary game object to host our audio source
		GameObject temporaryAudioHost = new GameObject("TempAudio");
		// we add an audio source to that host
		AudioSource audioSource = temporaryAudioHost.AddComponent<AudioSource>() as AudioSource; 
		// we set that audio source clip to the one in paramaters
		audioSource.clip = sfx; 
		// we set the audio source volume to the one in parameters
		audioSource.volume = sfxVolume;
		audioSource.spatialBlend = 0f;
		// we start playing the sound
		audioSource.Play(); 
		// we destroy the host after the clip has played
		Destroy(temporaryAudioHost, sfx.length);
		// we return the audiosource reference
		return audioSource;
	}

	/// <summary>
	/// Plays a sound with random pitch
	/// </summary>
	/// <returns>An audiosource</returns>
	/// <param name="Sfx">The sound clip you want to play.</param>
	/// <param name="Location">The location of the sound.</param>
	/// <param name="Volume">The volume of the sound.</param>
	public AudioSource PlaySoundModulated(AudioClip sfx, float sfxVolume = 1f)
	{
		if (!SfxOn)
			return null;
		// we create a temporary game object to host our audio source
		GameObject temporaryAudioHost = new GameObject("TempAudio");
		// we add an audio source to that host
		AudioSource audioSource = temporaryAudioHost.AddComponent<AudioSource>() as AudioSource; 
		// we set that audio source clip to the one in paramaters
		audioSource.clip = sfx; 
		// we set the audio source volume to the one in parameters
		audioSource.volume = sfxVolume;
		float randomNormalPitch = GaussianUtils.NextGaussian (1f, 3f);
		audioSource.pitch = randomNormalPitch;
		audioSource.spatialBlend = 0f;
		// we start playing the sound
		audioSource.Play(); 
		// we destroy the host after the clip has played
		Destroy(temporaryAudioHost, sfx.length);
		// we return the audiosource reference
		return audioSource;
	}
}
