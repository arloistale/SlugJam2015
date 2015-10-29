using Parse;

[ParseClassName("Phrase")]
public class ParsePhrase : ParseObject
{
	[ParseFieldName("correctMessage")]
	public string CorrectMessage
	{
		get { return GetProperty<string>("CorrectMessage"); }
		set { SetProperty<string>(value, "CorrectMessage"); }
	}
}