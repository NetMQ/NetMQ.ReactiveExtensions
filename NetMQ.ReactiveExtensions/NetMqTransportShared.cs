// *************************************************************************************************************************
// (c)2014,2015,2016 NeuralFutures LLC.
// Embedded copy protection will prevent this code from generating full alpha on any unauthorized 3rd party machine.
// Licenses are reasonably priced and will be supplied to interested 3rd parties. Just ask what NeuralFutures is working on.
// *************************************************************************************************************************

#region
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NetMQ.Monitoring;
using NetMQ.Sockets;
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

		private NetMqTransportShared()
		{
		}

		/// <summary>
		/// Intent: Singleton.
		/// </summary>
		public static NetMqTransportShared Instance
		{
			get
			{
				if (_instance == null)
				{
					lock (_syncRoot)
					{
						if (_instance == null)
						{
							_instance = new NetMqTransportShared();
						}
					}
				}

				return _instance;
			}
		}

		#region Get Publisher Socket (if it's already been opened, we reuse it).
		/// <summary>
		/// Intent: See interface.
		/// </summary>
		public PublisherSocket GetSharedPublisherSocket(string addressZeroMq)
		{
			lock (m_initializePublisherLock)
			{
				if (_dictAddressZeroMqToPublisherSocket.ContainsKey(addressZeroMq) == false)
				{
					_dictAddressZeroMqToPublisherSocket[addressZeroMq] = GetNewPublisherSocket(addressZeroMq);
				}
				return _dictAddressZeroMqToPublisherSocket[addressZeroMq];
			}
		}

		#region Get publisher socket.
		private readonly object m_initializePublisherLock = new object();
		private readonly ManualResetEvent m_publisherReadySignal = new ManualResetEvent(false);
		readonly Dictionary<string, PublisherSocket> _dictAddressZeroMqToPublisherSocket = new Dictionary<string, PublisherSocket>();

		private PublisherSocket GetNewPublisherSocket(string addressZeroMq)
		{
			PublisherSocket publisherSocket;
			{
				Console.Write("Get new publisher socket.\n");

				//_loggerDelegate?.Invoke(string.Format("Publisher socket binding to: {0}\n", addressZeroMq));

				publisherSocket = new PublisherSocket();

				// Corner case: wait until publisher socket is ready (see code below that waits for
				// "_publisherReadySignal").
				NetMQMonitor monitor;
				{
					// Must ensure that we have a unique monitor name for every instance of this class.
					string endPoint = string.Format("inproc://#SubjectNetMQ#Publisher#{0}", addressZeroMq);
					monitor = new NetMQMonitor(publisherSocket,
						endPoint,
						SocketEvents.Accepted | SocketEvents.Listening
						);
					monitor.Accepted += Publisher_Event_Accepted;
					monitor.Listening += Publisher_Event_Listening;
					monitor.StartAsync();
				}

				publisherSocket.Options.SendHighWatermark = 2000*1000;
				publisherSocket.Bind(addressZeroMq);

				// Corner case: wait until publisher socket is ready (see code below that sets "_publisherReadySignal").
				{
					Stopwatch sw = Stopwatch.StartNew();
					m_publisherReadySignal.WaitOne(TimeSpan.FromMilliseconds(3000));
					//_loggerDelegate?.Invoke(string.Format("Publisher: Waited {0} ms for binding.\n", sw.ElapsedMilliseconds));
				}
				{
					monitor.Accepted -= Publisher_Event_Accepted;
					monitor.Listening -= Publisher_Event_Listening;
					// Current issue with NegMQ: Cannot stop or dispose monitor, or else it stops the parent socket.
					//monitor.Stop();
					//monitor.Dispose();
				}
			}
			Thread.Sleep(500); // Otherwise, the first item we publish may get missed by the subscriber.
			return publisherSocket;
		}

		private void Publisher_Event_Listening(object sender, NetMQMonitorSocketEventArgs e)
		{
			//_loggerDelegate?.Invoke(string.Format("Publisher event: {0}\n", e.SocketEvent));
			m_publisherReadySignal.Set();
		}

		private void Publisher_Event_Accepted(object sender, NetMQMonitorSocketEventArgs e)
		{
			//_loggerDelegate?.Invoke(string.Format("Publisher event: {0}\n", e.SocketEvent));
			m_publisherReadySignal.Set();
		}
		#endregion
		#endregion

		#region Get Subscriber socket (if it's already been opened, we reuse it).
		readonly Dictionary<string, SubscriberSocket> _dictAddressZeroMqToSubscriberSocket = new Dictionary<string, SubscriberSocket>();
		private readonly object m_initializeSubscriberLock = new object();
		/// <summary>
		/// Intent: See interface.
		/// </summary>		
		public SubscriberSocket GetSharedSubscriberSocket(string addressZeroMq)
		{
			lock (m_initializeSubscriberLock)
			{
				if (_dictAddressZeroMqToSubscriberSocket.ContainsKey(addressZeroMq) == false)
				{
					_dictAddressZeroMqToSubscriberSocket[addressZeroMq] = GetNewSubscriberSocket(addressZeroMq);
					return _dictAddressZeroMqToSubscriberSocket[addressZeroMq];
				}
				else
				{
					Console.Write("Opening second subscriber.\n");
					var subscriberSocket = new SubscriberSocket();
					subscriberSocket.Options.ReceiveHighWatermark = 2000 * 1000;
					subscriberSocket.Connect(addressZeroMq);
					Thread.Sleep(TimeSpan.FromMilliseconds(500));
					return subscriberSocket;
				}
			}
		}

		private readonly ManualResetEvent m_subscriberReadySignal = new ManualResetEvent(false);

		private SubscriberSocket GetNewSubscriberSocket(string addressZeroMq)
		{
			var subscriberSocket = new SubscriberSocket();

			// Corner case: wait until subscriber socket is ready (see code below that waits for
			// "_subscriberReadySignal").
			NetMQMonitor monitor;
			{
				// Must ensure that we have a unique monitor name for every instance of this class.
				string endpoint = string.Format("inproc://#SubjectNetMQ#Subscriber#{0}{1}", addressZeroMq, Guid.NewGuid());

				monitor = new NetMQMonitor(subscriberSocket,
					endpoint,
					SocketEvents.ConnectRetried | SocketEvents.Connected);
				monitor.ConnectRetried += Subscriber_Event_ConnectRetried;
				monitor.Connected += Subscriber_Event_Connected;
				monitor.StartAsync();
			}

			subscriberSocket.Options.ReceiveHighWatermark = 2000*1000;
			subscriberSocket.Connect(addressZeroMq);

			// Corner case: wait until the publisher socket is ready (see code above that sets
			// "_subscriberReadySignal").
			{
				Stopwatch sw = Stopwatch.StartNew();
				m_subscriberReadySignal.WaitOne(TimeSpan.FromMilliseconds(3000));
				//_loggerDelegate?.Invoke(string.Format("Subscriber: Waited {0} ms for connection.\n", sw.ElapsedMilliseconds));

				monitor.ConnectRetried -= Subscriber_Event_ConnectRetried;
				monitor.Connected -= Subscriber_Event_Connected;

				// Issue with NetMQ - cannot .Stop or .Dispose, or else it will dispose of the parent socket.
				//monitor.Stop();
				//monitor.Dispose();
			}

			Thread.Sleep(TimeSpan.FromMilliseconds(500));
			return subscriberSocket;			
		}

		private void Subscriber_Event_Connected(object sender, NetMQMonitorSocketEventArgs e)
		{
			//_loggerDelegate?.Invoke(string.Format("Subscriber event: {0}\n", e.SocketEvent));
			m_subscriberReadySignal.Set();
		}

		private void Subscriber_Event_ConnectRetried(object sender, NetMQMonitorIntervalEventArgs e)
		{
			//_loggerDelegate?.Invoke(string.Format("Subscriber event: {0}\n", e.SocketEvent));
			m_subscriberReadySignal.Set();
		}
		#endregion
	}
}