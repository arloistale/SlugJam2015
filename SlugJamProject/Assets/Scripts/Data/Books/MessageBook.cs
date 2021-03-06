﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Parse;

public class MessageBook
{
	public static string AppName = "Spacepace";
	
	public static IDictionary<ParseException.ErrorCode, string> ParseExceptionMap = new Dictionary<ParseException.ErrorCode, string>() 
	{
		{ ParseException.ErrorCode.ConnectionFailed, "Bad connection" },
		{ ParseException.ErrorCode.InternalServerError, "Server error" },
		{ ParseException.ErrorCode.InvalidACL, "Server error" },
		{ ParseException.ErrorCode.InvalidSessionToken, "Invalid session" },
		{ ParseException.ErrorCode.MissingObjectId, "User error" },
		{ ParseException.ErrorCode.NotInitialized, "User error" },
		{ ParseException.ErrorCode.ObjectNotFound, "Wrong credentials" },
		{ ParseException.ErrorCode.OperationForbidden, "Server error" },
		{ ParseException.ErrorCode.OtherCause, "Unknown server error" },
		{ ParseException.ErrorCode.RequestLimitExceeded, "Server limits reached" },
		{ ParseException.ErrorCode.Timeout, "Bad connection" },
		{ ParseException.ErrorCode.UsernameTaken, "Username already taken" },
		{ ParseException.ErrorCode.ValidationFailed, "Wrong credentials" },
		{ ParseException.ErrorCode.IncorrectType, "Internal error" },
		{ ParseException.ErrorCode.InvalidJSON, "Server error" },
		{ ParseException.ErrorCode.ScriptFailed, "Internal server error" },
		{ ParseException.ErrorCode.SessionMissing, "Invalid session" },
		{ ParseException.ErrorCode.UsernameMissing, "Username missing" }
	};

	public static string InstructionsMessage = "[Tap] to add SPACE between words as they are typed";
}