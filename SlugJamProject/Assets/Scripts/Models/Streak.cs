using Parse;

[ParseClassName("Streak")]
public class Streak : ParseObject
{
	private const string KEY_DISPLAY_NAME = "DisplayName";
	private const string KEY_STREAK_VALUE = "StreakValue";

	[ParseFieldName(KEY_DISPLAY_NAME)]
	public string DisplayName 
	{
		get { return GetProperty<string> (KEY_DISPLAY_NAME); }
		set { SetProperty<string>(value, KEY_DISPLAY_NAME); }
	}

	[ParseFieldName(KEY_STREAK_VALUE)]
	public int StreakValue
	{
		get { return GetProperty<int> (KEY_STREAK_VALUE); }
		set { SetProperty<int>(value, KEY_STREAK_VALUE); }
	}
}