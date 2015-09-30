using UnityEngine;

public class Gaussian
{
	private static bool uselast = true;
	private static float nextGaussian = 0f;
	
	public static float BoxMuller()
	{
		if (uselast) 
		{ 
			uselast = false;
			return nextGaussian;
		}
		else
		{
			float v1, v2, s;
			do
			{
				v1 = Random.Range (-1f, 1f);
				v2 = Random.Range (-1f, 1f);
				s = v1 * v1 + v2 * v2;
			} while (s >= 1f || s == 0);

			s = Mathf.Sqrt((-2f * Mathf.Log (s)) / s);
			
			nextGaussian = v2 * s;
			uselast = true;
			return v1 * s;
		}
	}
	
	public static float BoxMuller(float mean, float standardDeviation)
	{
		return mean + BoxMuller() * standardDeviation;
	}
	
	// Will approximitely give a random gaussian integer between min and max so that min and max are at
	// 3.5 deviations from the mean (half-way of min and max).
	public static float Next(float min, float max)
	{
		float deviations = 3.5f;
		float r;
		while ((r = BoxMuller(min + (max - min) * 0.5f, (max - min) * 0.5f / deviations)) > max || r < min)
		{
		}
		
		return r;
	}
}