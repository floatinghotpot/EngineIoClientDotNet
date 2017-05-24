using System;
using System.Collections.Generic;
using System.Text;

namespace WebSocketSharp4Net
{
	public class DataReceivedEventArgs : EventArgs
	{
		public DataReceivedEventArgs(byte[] data)
		{
			Data = data;
		}

		public byte[] Data { get; private set; }
	}
}