﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AsyncHelper : MonoBehaviour 
{
	public readonly static Queue<Action> ExecuteOnMainThread = new Queue<Action>();
	
	public virtual void Update()
	{
		// dispatch stuff on main thread
		while (ExecuteOnMainThread.Count > 0)
		{
			ExecuteOnMainThread.Dequeue().Invoke();
		}
	}
}