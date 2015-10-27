public enum ErrorType
{
	Unknown,
	ParseInternal,
	ParseException
}

public struct ErrorInfo
{
	public ErrorType ErrorType;
	public int ErrorCode;
	
	public ErrorInfo(ErrorType type, int code = -1)
	{
		ErrorType = type;
		ErrorCode = code;
	}
}