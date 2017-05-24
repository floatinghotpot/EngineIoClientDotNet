
using System;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.Reflection;
using WebSocketSharp;

namespace WebSocketSharp4Net
{
	public partial class WebSocket : IDisposable
	{
		internal WebSocketSharp.WebSocket mClient { get; private set; }

		public WebSocketVersion Version { get; private set; }
		public DateTime LastActiveTime { get; internal set; }

		public bool EnableAutoSendPing { get; set; }
		public int AutoSendPingInterval { get; set; }

		protected const string UserAgentKey = "User-Agent";
		//internal IProtocolProcessor ProtocolProcessor { get; private set; }
		//public bool SupportBinary { get { return ProtocolProcessor.SupportBinary; } }

		internal Uri TargetUri { get; private set; }
		internal string SubProtocol { get; private set; }
		internal IDictionary<string, object> Items { get; private set; }
		internal List<KeyValuePair<string, string>> Cookies { get; private set; }
		internal List<KeyValuePair<string, string>> CustomHeaderItems { get; private set; }
		public const int DefaultReceiveBufferSize = 4096;

		private int m_StateCode;
		internal int StateCode
		{
			get { return m_StateCode; }
		}

		public WebSocketState State
		{
			get { return (WebSocketState)m_StateCode; }
		}

		public bool Handshaked { get; private set; }

		private Timer m_WebSocketTimer;
		internal string LastPongResponse { get; set; }
		private string m_LastPingRequest;

		private const string m_UriScheme = "ws";
		private const string m_UriPrefix = m_UriScheme + "://";
		private const string m_SecureUriScheme = "wss";
		private const int m_SecurePort = 443;
		private const string m_SecureUriPrefix = m_SecureUriScheme + "://";

		//public IProxyConnector Proxy { get; set; }
		//private EndPoint m_HttpConnectProxy;
		//internal EndPoint HttpConnectProxy {get { return m_HttpConnectProxy; }}
		internal string HandshakeHost { get; private set; }
		internal string Origin { get; private set; }

		private bool m_Disposed = false;

		static WebSocket()
		{
		}

		public WebSocket(string uri, string subProtocol = "", List<KeyValuePair<string, string>> cookies = null, List<KeyValuePair<string, string>> customHeaderItems = null)
		{
			Initialize(uri, subProtocol, cookies, customHeaderItems);
		}

		private void Initialize(string uri, string subProtocol, List<KeyValuePair<string, string>> cookies, List<KeyValuePair<string, string>> customHeaderItems)
		{
			string[] protocols = new string[1];
			protocols[0] = subProtocol;

			m_StateCode = WebSocketStateConst.None;

			var client = new WebSocketSharp.WebSocket(uri, protocols);
			client.OnOpen += new EventHandler(client_Connected);
			client.OnMessage += new EventHandler<MessageEventArgs>(client_DataReceived);
			client.OnError += new EventHandler<ErrorEventArgs>(client_Error);
			client.OnClose += new EventHandler<CloseEventArgs>(client_Closed);

			mClient = client;

			EnableAutoSendPing = true;
		}

		public void Open()
		{
			m_StateCode = WebSocketStateConst.Connecting;

			mClient.Connect();
		}

		void client_DataReceived(object sender, MessageEventArgs e)
		{
			OnDataReceived(e.RawData);
		}

		void client_Error(object sender, ErrorEventArgs e)
		{
			OnError(e.Exception);

			//Also fire close event if the connection fail to connect
            OnClosed();
		}

		void client_Closed(object sender, EventArgs e)
		{
            OnClosed();
		}

		void client_Connected(object sender, EventArgs e)
		{
            OnConnected();
		}

		void OnConnected()
		{
			if (Items.Count > 0) 
				Items.Clear();

			// TODO: send hand shake
		}

		private void OnDataReceived(byte[] data)
		{
			FireDataReceived(data);
		}

		private EventHandler m_Opened;

		public event EventHandler Opened
		{
			add { m_Opened += value; }
			remove { m_Opened -= value; }
		}

		private EventHandler<MessageReceivedEventArgs> m_MessageReceived;

		public event EventHandler<MessageReceivedEventArgs> MessageReceived
		{
			add { m_MessageReceived += value; }
			remove { m_MessageReceived -= value; }
		}

		internal void FireMessageReceived(string message)
		{
			if (m_MessageReceived == null) return;

            m_MessageReceived(this, new MessageReceivedEventArgs(message));
		}

		private EventHandler<DataReceivedEventArgs> m_DataReceived;

		public event EventHandler<DataReceivedEventArgs> DataReceived
		{
			add { m_DataReceived += value; }
			remove { m_DataReceived -= value; }
		}

		internal void FireDataReceived(byte[] data)
		{
			if (m_DataReceived == null) return;

            m_DataReceived(this, new DataReceivedEventArgs(data));
		}

		private const string m_NotOpenSendingMessage = "You must send data by websocket after websocket is opened!";

		private bool EnsureWebSocketOpen()
		{
			if (!Handshaked)
			{
                OnError(new Exception(m_NotOpenSendingMessage));
				return false;
			}
			return true;
		}

		public void Send(string message)
		{
			var client = mClient;
			if (client != null)
			{
				client.Send(message);
			}
		}

		public void Send(byte[] data, int offset, int length)
		{
			var client = mClient;
			if (client != null)
			{
				client.Send(data);
			}
		}

		public void Send(IList<ArraySegment<byte>> segments)
		{
			var client = mClient;
			if (client != null)
			{
				// TODO:
			}
		}

		private ClosedEventArgs m_ClosedArgs;
		private void OnClosed()
		{
			var fireBaseClose = false;

			if (m_StateCode == WebSocketStateConst.Closing || m_StateCode == WebSocketStateConst.Open || m_StateCode == WebSocketStateConst.Connecting)
				fireBaseClose = true;
			
			m_StateCode = WebSocketStateConst.Closed;

			if (fireBaseClose)
				FireClosed();
		}

		public void Close(int statusCode, string reason)
		{
			m_ClosedArgs = new ClosedEventArgs((short)statusCode, reason);

			//The websocket never be opened
			if (Interlocked.CompareExchange(ref m_StateCode, WebSocketStateConst.Closed, WebSocketStateConst.None)
                    == WebSocketStateConst.None)
            {
                OnClosed();
                return;
            }

            var client = mClient;

            if (client != null)
            {
                client.Close();
                return;
            }

			OnClosed();
		}

		public void Close(string reason)
		{
		}

		public void Close()
		{
		}

		private EventHandler<ErrEventArgs> m_Error;

		public event EventHandler<ErrEventArgs> Error
		{
			add { m_Error += value; }
			remove { m_Error -= value; }
		}

		internal void FireError(Exception error)
		{
            OnError(error);
		}

		private void OnError(Exception e)
		{
			OnError(new ErrEventArgs(e));
		}

		private void OnError(ErrEventArgs e)
		{
			var handler = m_Error;

			if (handler == null)
				return;

			handler(this, e);
		}

		private EventHandler m_Closed;
		public event EventHandler Closed { 
			add { m_Closed += value; }
			remove { m_Closed -= value; }
		}

		private void ClearTimer()
		{
		}

		private void FireClosed()
		{
            ClearTimer();

			var handler = m_Closed;

			if (handler != null)
                handler(this, m_ClosedArgs ?? EventArgs.Empty);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (m_Disposed) return;

			if (disposing)
			{
				var client = mClient;

				if (client != null)
				{
					client.OnOpen -= new EventHandler(client_Connected);
					client.OnMessage -= new EventHandler<MessageEventArgs>(client_DataReceived);
					client.OnError -= new EventHandler<ErrorEventArgs>(client_Error);
					client.OnClose -= new EventHandler<CloseEventArgs>(client_Closed);

					client.Close();
					client = null;
				}

				ClearTimer();
			}
		}

		public void Dispose()
		{
            Dispose(true);
			GC.SuppressFinalize(this);
		}

		~WebSocket()
		{
		}
	}
}
