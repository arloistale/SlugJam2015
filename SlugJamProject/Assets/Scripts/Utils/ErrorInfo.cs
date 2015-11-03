using Parse;

public enum ErrorType
{
	Unknown,
	ParseInternal,
	ParseException,
	Timeout
}

public struct ErrorInfo
{
	public ErrorType ErrorType;
	public ParseException.ErrorCode ErrorCode;
	
	public ErrorInfo(ErrorType type)
	{
		ErrorType = type;
		ErrorCode = ParseException.ErrorCode.OtherCause;
	}
	
	public ErrorInfo(ErrorType type, ParseException.ErrorCode code)
	{
		ErrorType = type;
		ErrorCode = code;
	}
	
	public string GetErrorStr()
	{
		string errorStr = "Unknown error";
		
		switch(ErrorType)
		{
			case ErrorType.ParseInternal:
				errorStr = "Server error";
				break;
			case ErrorType.ParseException:
				if(MessageBook.ParseExceptionMap.ContainsKey(ErrorCode))
					errorStr = MessageBook.ParseExceptionMap[ErrorCode];
				else
					errorStr = ErrorCode + "";
				break;
			case ErrorType.Timeout:
				errorStr = "Server timeout";
				break;
		}
		
		return errorStr;
	}
}