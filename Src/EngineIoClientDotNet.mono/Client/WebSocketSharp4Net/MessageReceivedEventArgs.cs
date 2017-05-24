using System;
using System.Collections.Generic;
using System.Text;

namespace WebSocketSharp4Net
{
	public class MessageReceivedEventArgs : EventArgs
	{
		public MessageReceivedEventArgs(string message)
		{
			Message = message;
		}

		public string Message { get; private set; }
	}
}