using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using System.Threading;
using NetMQ.Monitoring;
using NetMQ.Sockets;
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable InvertIf
#pragma warning disable 649

namespace NetMQ.ReactiveExtensions
{
	/// <summary>
	///	Intent: Pub/sub across different processes.
	/// </summary>
	/// <threadSafe>Yes</threadSafe>
	public class SubjectNetMQ<T> : IDisposable, ISubjectNetMQ<T>
	{
		#region Public

		public string SubscriberFilterName { get; private set; }

		public string AddressZeroMq { get; private set; }
		#endregion

		// ReSharper disable once NotAccessedField.Local
		private readonly WhenToCreateNetworkConnection _mWhenToCreateNetworkConnection;
		private CancellationTokenSource m_cancellationTokenSource;
		private readonly Action<string> _loggerDelegate;

		private readonly List<IObserver<T>> m_subscribers = new List<IObserver<T>>();
		private readonly object m_subscribersLock = new object();

		/// <summary>
		/// Intent: See interface.
		/// </summary>
		/// <param name="addressZeroMq">Address to connect to, e.g. "tcp://127.0.0.1:56000"</param>
		/// <param name="subscriberFilterName">Subscriber Filter name. Defaults to the type name T. Allows many types to get sent over the same transport connection.</param>
		/// <param name="whenToCreateNetworkConnection">When to create the network connection.</param>
		/// <param name="cancellationTokenSource">Allows graceful termination of all internal threads associated with this subject.</param>
		/// <param name="loggerDelegate">(optional) If we want to look at messages generated within this class, specify a logger here.</param>
		public SubjectNetMQ(string addressZeroMq, string subscriberFilterName = null, WhenToCreateNetworkConnection whenToCreateNetworkConnection = WhenToCreateNetworkConnection.LazyConnectOnFirstUse, CancellationTokenSource cancellationTokenSource = default(CancellationTokenSource), Action<string> loggerDelegate = null)
		{
			_mWhenToCreateNetworkConnection = whenToCreateNetworkConnection;
			m_cancellationTokenSource = cancellationTokenSource;
			_loggerDelegate = loggerDelegate;

			if (subscriberFilterName == null)
			{
				//SubscriberFilterName = typeof(T).ToString();
				// Unfortunately, the subscriber never scans more than the first 32 characters of the filter, so we must
                // trim to less than this length. Damn!
				//SubscriberFilterName = SubscriberFilterName.Substring(SubscriberFilterName.Length - 32);
				SubscriberFilterName = "default";
			}

			if (string.IsNullOrEmpty(Thread.CurrentThread.Name) == true)
			{
				// Cannot set the thread name twice.
				Thread.CurrentThread.Name = subscriberFilterName;
			}

			AddressZeroMq = addressZeroMq;

			if (string.IsNullOrEmpty(AddressZeroMq))
			{
				throw new Exception("Error. Must define the address for ZeroMQ.");
			}

			if (whenToCreateNetworkConnection == WhenToCreateNetworkConnection.InstantConnectOnClassInstantiation)
			{
				throw new ArgumentException(
					"Argument exception: \"whenToCreateNetworkConnection == WhenToCreateNetworkConnection.InstantConnectOnClassInstantiation\" is not supported for reasons of efficiency.\n" +
				    "By default, this class will lazily create a subscriber when '.Subscriber()' is first called, and lazily " +
					"create a publisher when '.OnNext()', '.OnError()', or '.OnCompleted()' are called for the first time.");
			}
		}

		#region Initialize publisher on demand.
		private PublisherSocket m_publisherSocket;
		private volatile bool m_initializePublisherDone = false;
		private readonly object m_initializePublisherLock = new object();
		private readonly ManualResetEvent m_publisherReadySignal = new ManualResetEvent(false);
		private void InitializePublisherOnFirstUse()
		{
			if (m_initializePublisherDone == false) // Double checked locking.
			{
				lock (m_initializePublisherLock)
				{
					if (m_initializePublisherDone == false)
					{
						_loggerDelegate?.Invoke(string.Format("Publisher socket binding to: {0}\n", AddressZeroMq));

						m_publisherSocket = new PublisherSocket();

						// Corner case: wait until publisher socket is ready (see code below that waits for
						// "_publisherReadySignal").
						NetMQMonitor monitor;
						{
							// Must ensure that we have a unique monitor name for every instance of this class.
							string endPoint = string.Format("inproc://#SubjectNetMQ#Publisher#{0}#{1}", this.SubscriberFilterName, this.AddressZeroMq);
							monitor = new NetMQMonitor(m_publisherSocket, endPoint,
								SocketEvents.Accepted | SocketEvents.Listening
								);
							monitor.Accepted += Publisher_Event_Accepted;
							monitor.Listening += Publisher_Event_Listening;
							monitor.StartAsync();
						}


						m_publisherSocket.Options.SendHighWatermark = 2000 * 1000;

						m_publisherSocket.Bind(this.AddressZeroMq);

						// Corner case: wait until publisher socket is ready (see code below that sets
						// "_publisherReadySignal").
						{
							Stopwatch sw = Stopwatch.StartNew();
							m_publisherReadySignal.WaitOne(TimeSpan.FromMilliseconds(3000));
							_loggerDelegate?.Invoke(string.Format("Publisher: Waited {0} ms for binding.\n", sw.ElapsedMilliseconds));
						}
						{
							monitor.Accepted -= Publisher_Event_Accepted;
							monitor.Listening -= Publisher_Event_Listening;
							// Current issue with NegMQ: Cannot stop or dispose monitor, or else it stops the parent socket.
							//monitor.Stop();
							//monitor.Dispose();
						}
						m_initializePublisherDone = true;
					}
				} // lock
				Thread.Sleep(500); // Otherwise, the first item we publish may get missed by the subscriber.
			}
		}

		private void Publisher_Event_Listening(object sender, NetMQMonitorSocketEventArgs e)
		{
			_loggerDelegate?.Invoke(string.Format("Publisher event: {0}\n", e.SocketEvent));
			m_publisherReadySignal.Set();
		}

		private void Publisher_Event_Accepted(object sender, NetMQMonitorSocketEventArgs e)
		{
			_loggerDelegate?.Invoke(string.Format("Publisher event: {0}\n", e.SocketEvent));
			m_publisherReadySignal.Set();
		}
		#endregion

		#region Initialize subscriber on demand.
		private SubscriberSocket m_subscriberSocket;
		private volatile bool m_initializeSubscriberDone = false;
		private Thread m_thread;
		private readonly ManualResetEvent m_subscriberReadySignal = new ManualResetEvent(false);

		private void InitializeSubscriberOnFirstUse()
		{
			if (m_initializeSubscriberDone == false) // Double checked locking.
			{
				lock (m_subscribersLock)
				{
					if (m_initializeSubscriberDone == false)
					{
						_loggerDelegate?.Invoke(string.Format("Subscriber socket connecting to: {0}\n", AddressZeroMq));
						m_subscriberSocket = new SubscriberSocket();

						// Corner case: wait until subscriber socket is ready (see code below that waits for
						// "_subscriberReadySignal").
						NetMQMonitor monitor;
						{
							// Must ensure that we have a unique monitor name for every instance of this class.
							string endpoint = string.Format("inproc://#SubjectNetMQ#Subscriber#{0}#{1}", this.SubscriberFilterName, this.AddressZeroMq);

							monitor = new NetMQMonitor(m_subscriberSocket, endpoint,
								SocketEvents.ConnectRetried | SocketEvents.Connected);
							monitor.ConnectRetried += Subscriber_Event_ConnectRetried;
							monitor.Connected += Subscriber_Event_Connected;
							monitor.StartAsync();
						}

						m_subscriberSocket.Options.ReceiveHighWatermark = 2000 * 1000;
						m_subscriberSocket.Connect(this.AddressZeroMq);

						// this.SubscriberFilterName is set to the type T of the incoming class by default, so we can have many 
						m_subscriberSocket.Subscribe(this.SubscriberFilterName);

						if (m_cancellationTokenSource == null)
						{
							m_cancellationTokenSource = new CancellationTokenSource();
						}

						ManualResetEvent threadReadySignal = new ManualResetEvent(false);

						m_thread = new Thread(() =>
						{
							try
							{
								_loggerDelegate?.Invoke(string.Format("Thread initialized.\n"));
								threadReadySignal.Set();
								while (m_cancellationTokenSource.IsCancellationRequested == false)
								{
									string messageTopicReceived = m_subscriberSocket.ReceiveFrameString();
									if (messageTopicReceived != SubscriberFilterName)
									{
										throw new Exception(string.Format("Error E65724. We should always subscribe on the queue name '{0}', instead we got '{1}'.", SubscriberFilterName, messageTopicReceived));
									}
									var type = m_subscriberSocket.ReceiveFrameString();
									switch (type)
									{
										// Originated from "OnNext".
										case "N":
											T messageReceived = m_subscriberSocket.ReceiveFrameBytes().DeserializeProtoBuf<T>();
											lock (m_subscribersLock)
											{
												m_subscribers.ForEach(o => o.OnNext(messageReceived));
											}
											break;
										// Originated from "OnCompleted".
										case "C":
											lock (m_subscribersLock)
											{
												m_subscribers.ForEach(o => o.OnCompleted());

												// We are done! We don't want to send any more messages to subscribers, and we
												// want to close the listening socket.
												m_cancellationTokenSource.Cancel();
											}
											break;
										// Originated from "OnException".
										case "E":
											Exception exception;
											string exceptionAsString = "Uninitialized.";
											try
											{
												// Not used, but useful for cross-platform debugging: we can read the error straight off the wire.
												exceptionAsString = m_subscriberSocket.ReceiveFrameBytes().DeserializeProtoBuf<string>();
												SerializableException exceptionWrapper = m_subscriberSocket.ReceiveFrameBytes().DeSerializeException();
												exception = exceptionWrapper.InnerException;
											}
											catch (Exception ex)
											{
												// If we had trouble deserializing the exception (probably due to a
												// different version of .NET), then do the next best thing: (1) The
												// inner exception is the error we got when deserializing, and (2) the
												// main exception is the human-readable "exception.ToString()" that we
												// originally captured.
												exception = new Exception(exceptionAsString, ex);
											}

											lock (m_subscribersLock)
											{
												m_subscribers.ForEach(o => o.OnError(exception));
											}
											break;
										// Originated from a "Ping" request.
										case "P":
											// Do nothing, this is a ping command used to wait until sockets are initialized properly.
											_loggerDelegate?.Invoke(string.Format("Received ping.\n"));
											break;
										default:
											throw new Exception(string.Format("Error E28734. Something is wrong - received '{0}' when we expected \"N\", \"C\" or \"E\" - are we out of sync?", type));
									}
								}
							}
							catch (Exception ex)
							{
								_loggerDelegate?.Invoke(string.Format("Error E23844. Exception in threadName \"{0}\". Thread exiting. Exception: \"{1}\".\n", SubscriberFilterName, ex.Message));
								lock (m_subscribersLock)
								{
									this.m_subscribers.ForEach((ob) => ob.OnError(ex));
								}
							}
							finally
							{
								lock (m_subscribersLock)
								{
									m_subscribers.Clear();
								}
								m_cancellationTokenSource.Dispose();

								// Disconnect from the socket.
								m_subscriberSocket.Dispose();
							}
						})
						{
							Name = this.SubscriberFilterName,
							IsBackground = true // Have to set it to background, or else it will not exit when the program exits.
						};
						m_thread.Start();

						// Wait for thread to properly spin up.
						threadReadySignal.WaitOne(TimeSpan.FromMilliseconds(3000));

						// Corner case: wait until the publisher socket is ready (see code above that sets
						// "_subscriberReadySignal").
						{
							Stopwatch sw = Stopwatch.StartNew();
							m_subscriberReadySignal.WaitOne(TimeSpan.FromMilliseconds(3000));
							_loggerDelegate?.Invoke(string.Format("Subscriber: Waited {0} ms for connection.\n", sw.ElapsedMilliseconds));

							monitor.ConnectRetried -= Subscriber_Event_ConnectRetried;
							monitor.Connected -= Subscriber_Event_Connected;

							// Issue with NetMQ - cannot .Stop or .Dispose, or else it will dispose of the parent socket.
							//monitor.Stop();
							//monitor.Dispose();
						}

						_loggerDelegate?.Invoke(string.Format("Subscriber: finished setup.\n"));

						m_initializeSubscriberDone = true;
					}
				} // lock
				Thread.Sleep(500); // Otherwise, the first item we subscribe  to may get missed by the subscriber.
			}
		}

		private void Subscriber_Event_Connected(object sender, NetMQMonitorSocketEventArgs e)
		{
			_loggerDelegate?.Invoke(string.Format("Subscriber event: {0}\n", e.SocketEvent));
			m_subscriberReadySignal.Set();
		}

		private void Subscriber_Event_ConnectRetried(object sender, NetMQMonitorIntervalEventArgs e)
		{
			_loggerDelegate?.Invoke(string.Format("Subscriber event: {0}\n", e.SocketEvent));
			m_subscriberReadySignal.Set();
		}
		#endregion

		#region IObservable<T> (i.e. the subscriber)
		public IDisposable Subscribe(IObserver<T> observer)
		{
			lock (m_subscribersLock)
			{
				this.m_subscribers.Add(observer);
			}

			InitializeSubscriberOnFirstUse();

			// Could return ".this", but this would introduce an issue: if one subscriber unsubscribed, it would
			// unsubscribe all subscribers.
			return new AnonymousDisposable(() =>
			{
				lock (m_subscribersLock)
				{
					this.m_subscribers.Remove(observer);
				}
			});
		}
		#endregion

		#region Implement IObserver<T> (i.e. the publisher).

		public void OnNext(T message)
		{
			try
			{
				InitializePublisherOnFirstUse();

				byte[] serialized = message.SerializeProtoBuf<T>();

				// Publish message using ZeroMQ as the transport mechanism.
				m_publisherSocket.SendMoreFrame(SubscriberFilterName)
					.SendMoreFrame("N") // "N", "E" or "C" for "OnNext", "OnError" or "OnCompleted".
					.SendFrame(serialized);

				// Comment in the remaining code for the standard pub/sub pattern.

				//if (this.HasObservers == false)
				//{
				//throw new QxNoSubscribers("Error E23444. As there are no subscribers to this publisher, this event will be lost.");
				//}

				//lock (_subscribersLock)
				//{
				//this._subscribers.ForEach(msg => msg.OnNext(message));
				//}
			}
			catch (InvalidOperationException ex)
			{
				if (ex.Source.ToLower().Contains("protobuf-net"))
				{
					var exWithDocs = new InvalidOperationException("Error: Message must be serializable by Protobuf-Net. To fix, annotate message with [ProtoContract] and [ProtoMember(N)]. See help on web.");
					this.OnError(exWithDocs);
					throw exWithDocs;
				}
			}
			catch (Exception ex)
			{
				_loggerDelegate?.Invoke(string.Format("Exception: {0}.", ex.Message));
				this.OnError(ex);
				throw;
			}
		}

		public void OnError(Exception exception)
		{
			InitializePublisherOnFirstUse();

			var exceptionWrapper = new SerializableException(exception);	
			byte[] serializedException = exceptionWrapper.SerializeException();
			string exceptionAsString = exception.ToString();

			m_publisherSocket.SendMoreFrame(SubscriberFilterName)
					.SendMoreFrame("E") // "N", "E" or "C" for "OnNext", "OnError" or "OnCompleted".
					.SendMoreFrame(exceptionAsString.SerializeProtoBuf()) // Human readable exception. Added for 100%
                                                                          // cross-platform debugging, so we can read
                                                                          // the error on the wire.
					.SendFrame(serializedException); // Machine readable exception. So we can pass the full exception to
                                                     // the .NET client.

			// Comment in the remaining code for the standard pub/sub pattern.

			//if (this.HasObservers == false)
			//{
			//throw new QxNoSubscribers("Error E28244. As there are no subscribers to this publisher, this published exception will be lost.");
			//}

			//lock (_subscribersLock)
			//{
			//this._subscribers.ForEach(msg => msg.OnError(exception));
			//}
		}

		public void OnCompleted()
		{
			InitializePublisherOnFirstUse();

			m_publisherSocket.SendMoreFrame(SubscriberFilterName)
				.SendFrame("C"); // "N", "E" or "C" for "OnNext", "OnError" or "OnCompleted".
		}
		#endregion

		#region Implement IDisposable.
		public void Dispose()
		{
			lock (m_subscribersLock)
			{
				m_subscribers.Clear();
			}
			m_cancellationTokenSource.Cancel();

			// Wait until the thread has exited.
			bool threadExitedProperly = m_thread.Join(TimeSpan.FromSeconds(30));
			if (threadExitedProperly == false)
			{
				throw new Exception("Error E62724. Thread did not exit when requested.");
			}
		}
		#endregion

		/// <summary>
		/// Intent: True if there are any subscribers registered.
		/// </summary>
		public bool HasObservers
		{
			get
			{
				lock (m_subscribersLock)
				{
					return this.m_subscribers.Count > 0;
				}
			}
		}
	}	
}

