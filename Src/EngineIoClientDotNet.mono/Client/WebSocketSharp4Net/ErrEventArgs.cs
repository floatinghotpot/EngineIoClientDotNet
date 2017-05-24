using System;
using System.Collections.Generic;
using System.Text;

namespace WebSocketSharp4Net
{
	public class ErrEventArgs : EventArgs
	{
		public ErrEventArgs(Exception e)
		{
			Exception = e;
		}

		public Exception Exception { get; private set; }
	}
}