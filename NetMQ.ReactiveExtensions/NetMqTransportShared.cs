#region
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NetMQ.Monitoring;
using NetMQ.Sockets;
// ReSharper disable ConvertToAutoProperty
#endregion

// ReSharper disable ConvertIfStatementToNullCoalescingExpression
// ReSharper disable InvertIf

namespace NetMQ.ReactiveExtensions
{
	/// <summary>
	///		Intent: We want to have one transport shared among all publishers and subscribers, in this process, if they
	///	    happen to use the same TCP/IP port.
	/// </summary>
	public class NetMqTransportShared : INetMqTransportShared
	{
		private static volatile NetMqTransportShared _instance;
		private static readonly object _syncRoot = new object();

		private NetMqTransportShared(Action<string> loggerDelegate = null)
		{
			this._loggerDelegate = loggerDelegate;
		}

		#region HighwaterMark
		private int _highwaterMark = 2000 * 1000;

		/// <summary>
		///		Intent: Default amount of messages to queue in memory before discarding more.
		/// </summary>
		public int HighwaterMark
		{
			get { return _highwaterMark; }
			set { _highwaterMark = value; }
		}
		#endregion

		private readonly Action<string> _loggerDelegate;

		/// <summary>
		/// Intent: Singleton.
		/// </summary>
		public static NetMqTransportShared Instance(Action<string> loggerDelegate = null)
		{
			if (_instance == null)
			{
				lock (_syncRoot)
				{
					if (_instance == null)
					{
						_instance = new NetMqTransportShared(loggerDelegate);
					}
				}
			}

			return _instance;
		}

		#region Get Publisher Socket (if it's already been opened, we reuse it).
		/// <summary>
		/// Intent: See interface.
		/// </summary>
		public PublisherSocket GetSharedPublisherSocket(string addressZeroMq)
		{
			lock (_initializePublisherLock)
			{
				if (_dictAddressZeroMqToPublisherSocket.ContainsKey(addressZeroMq) == false)
				{
					_dictAddressZeroMqToPublisherSocket[addressZeroMq] = GetNewPublisherSocket(addressZeroMq);
				}
				return _dictAddressZeroMqToPublisherSocket[addressZeroMq];
			}
		}

		#region Get publisher socket.
		private readonly object _initializePublisherLock = new object();
		private readonly ManualResetEvent _publisherReadySignal = new ManualResetEvent(false);
		readonly Dictionary<string, PublisherSocket> _dictAddressZeroMqToPublisherSocket = new Dictionary<string, PublisherSocket>();

		private PublisherSocket GetNewPublisherSocket(string addressZeroMq)
		{
			PublisherSocket publisherSocket;
			{
				_loggerDelegate?.Invoke(string.Format("Publisher socket binding to: {0}\n", addressZeroMq));

				publisherSocket = new PublisherSocket();

				// Corner case: wait until publisher socket is ready (see code below that waits for
				// "_publisherReadySignal").
				NetMQMonitor monitor;
				{
					// Must ensure that we have a unique monitor name for every instance of this class.
					string endPoint = string.Format("inproc://#SubjectNetMQ#Publisher#{0}#{1}", addressZeroMq, Guid.NewGuid().ToString());
					monitor = new NetMQMonitor(publisherSocket,
						endPoint,
						SocketEvents.Accepted | SocketEvents.Listening
						);
					monitor.Accepted += Publisher_Event_Accepted;                    
                    monitor.Listening += Publisher_Event_Listening;
					monitor.StartAsync();
				}

				publisherSocket.Options.SendHighWatermark = this.HighwaterMark;
				try
				{
					publisherSocket.Bind(addressZeroMq);
				}
				catch (NetMQException ex)
				{
					// This is usually because the address is in use.
					throw new Exception(string.Format("Error E56874. Cannot bind publisher to '{0}'. 95% probability that this is caused by trying to bind a publisher to a port already in use by another process. To fix, choose a unique publisher port for this process. For more on this error, see 'Readme.md' (or the GitHub homepage for NetMQ.ReactiveExtensions).", addressZeroMq), ex);
				}
				
				// Corner case: wait until publisher socket is ready (see code below that sets "_publisherReadySignal").
				{
					Stopwatch sw = Stopwatch.StartNew();
					_publisherReadySignal.WaitOne(TimeSpan.FromMilliseconds(3000));
					_loggerDelegate?.Invoke(string.Format("Publisher: Waited {0} ms for binding.\n", sw.ElapsedMilliseconds));
				}
				{
					monitor.Accepted -= Publisher_Event_Accepted;
					monitor.Listening -= Publisher_Event_Listening;
					// Current issue with NegMQ: Cannot stop or dispose monitor, or else it stops the parent socket.
					//monitor.Stop();
					//monitor.Dispose();
				}
			}

            // Otherwise, the first item we publish may get missed by the subscriber. 500 milliseconds consistently works 
            // locally, but occasionally fails on the AppVeyor build server. 650 milliseconds is optimal.
            using (EventWaitHandle wait = new ManualResetEvent(false))
            {
                // Cannot use Thread.Sleep() here, as this is incompatible with .NET Core 1.0, Windows 8.0, 8.1, and 10.
                wait.WaitOne(TimeSpan.FromMilliseconds(650));
            }

            return publisherSocket;
		}

		private void Publisher_Event_Listening(object sender, NetMQMonitorSocketEventArgs e)
		{
			_loggerDelegate?.Invoke(string.Format("Publisher event: {0}\n", e.SocketEvent));
			_publisherReadySignal.Set();
		}

		private void Publisher_Event_Accepted(object sender, NetMQMonitorSocketEventArgs e)
		{
			_loggerDelegate?.Invoke(string.Format("Publisher event: {0}\n", e.SocketEvent));
			_publisherReadySignal.Set();
		}
		#endregion
		#endregion

		#region Get Subscriber socket (if it's already been opened, we reuse it).
		private readonly object _initializeSubscriberLock = new object();
		/// <summary>
		/// Intent: See interface.
		/// </summary>		
		public SubscriberSocket GetSharedSubscriberSocket(string addressZeroMq)
		{
			lock (_initializeSubscriberLock)
			{
				// Must return a unique subscriber for every new ISubject of T.
				var subscriberSocket = new SubscriberSocket();
				subscriberSocket.Options.ReceiveHighWatermark = this.HighwaterMark;
				subscriberSocket.Connect(addressZeroMq);
				return subscriberSocket;
			}
		}
		#endregion
	}
}