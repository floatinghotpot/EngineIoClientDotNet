
using System;
using System.Threading;

namespace Quobject.EngineIoClientDotNet.Thread
{
	public class EasyTimer
	{
		private Timer m_timer;

		public EasyTimer(Action method, int period)
		{
			this.m_timer = new Timer(new TimerCallback(DoWork), method, 0, period);
		}

		static void DoWork(object obj)
		{
			Action method = (Action)obj;
			if (method != null) method();
		}

		public static EasyTimer SetTimeout(Action method, int delayInMilliseconds)
		{
			// Returns a stop handle which can be used for stopping
			// the timer, if required
			return new EasyTimer(method, delayInMilliseconds);
		}

		public void Stop()
		{
			//var log = LogManager.GetLogger(Global.CallerName());
			//log.Info("EasyTimer stop");
			if (m_timer != null) m_timer.Dispose();
		}

		public static void TaskRun(Action action)
		{
			//ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadProc), action);
			var resetEvent = new ManualResetEvent(false);
			ThreadPool.QueueUserWorkItem(
			    arg => 
			    {
			        DoWork( action );
					resetEvent.Set();
			    });
			resetEvent.WaitOne();
		}

		public static void TaskRunNoWait(Action action)
		{
			ThreadPool.QueueUserWorkItem(new WaitCallback(DoWork), action);
		}
	}


}
