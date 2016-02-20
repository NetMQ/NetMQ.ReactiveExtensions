using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NetMQ.Sockets;

namespace NetMQ.ReactiveExtensions
{
	/// <summary>
	/// Intent: Publisher.
	/// </summary>
	public class PublisherNetMq<T> : ISubjectNetMQ<T>, IObserver<T>, IPublisherNetMq<T>
	{
		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly Action<string> _loggerDelegate;
		private readonly WhenToCreateNetworkConnection _whenToCreateNetworkConnection;

		public string SubscriberFilterName { get; set; }

		public string AddressZeroMq { get; set; }

		///  <summary>
		/// 	Intent: Create a new publisher, using NetMQ as the transport layer.
		///  </summary>
		/// <param name="addressZeroMq">ZeroMq address to bind to, e.g. "tcp://localhost:56001</param>
		/// <param name="subscriberFilterName">Filter name on receiver. If you do not set this, it will default to the
		///     type name of T, and everything will just work.</param>
		/// <param name="whenToCreateNetworkConnection"></param>
		/// <param name="cancellationTokenSource"></param>
		/// <param name="loggerDelegate"></param>
		/// <returns></returns>
		public PublisherNetMq(string addressZeroMq, string subscriberFilterName = null, WhenToCreateNetworkConnection whenToCreateNetworkConnection = WhenToCreateNetworkConnection.SetupPublisherTransportNow, CancellationTokenSource cancellationTokenSource = default(CancellationTokenSource), Action<string> loggerDelegate = null)
		{
			_cancellationTokenSource = cancellationTokenSource;
			_loggerDelegate = loggerDelegate;
			_whenToCreateNetworkConnection = whenToCreateNetworkConnection;
			_cancellationTokenSource = cancellationTokenSource;

			if (subscriberFilterName == null)
			{
				// Unfortunately, the subscriber never scans more than the first 32 characters of the filter, so we must
				// trim to less than this length, but also ensure that it's unique. This ensures that if we get two
				// classnames of 50 characters that only differ by the last character, everything will still work and we
				// won't get crossed subscriptions.
				SubscriberFilterName = typeof(T).Name;

				if (SubscriberFilterName.Length > 32)
				{
					string uniqueHashCode = string.Format("{0:X8}", SubscriberFilterName.GetHashCode());

					SubscriberFilterName = SubscriberFilterName.Substring(0, Math.Min(SubscriberFilterName.Length, 24));
					SubscriberFilterName += uniqueHashCode;
				}
				if (SubscriberFilterName.Length > 32)
				{
					throw new Exception("Error E38742. Internal error. Logically, at this point subscription length could never be longer than 32 characters.");
				}
			}

			if (string.IsNullOrEmpty(Thread.CurrentThread.Name) == true)
			{
				// Cannot set the thread name twice.
				Thread.CurrentThread.Name = subscriberFilterName;
			}

			AddressZeroMq = addressZeroMq;

			if (string.IsNullOrEmpty(AddressZeroMq))
			{
				throw new Exception("Error E76244. Must define the endpoint address for ZeroMQ to connect (or bind) to.");
			}

			switch (whenToCreateNetworkConnection)
			{
				case WhenToCreateNetworkConnection.LazyConnectOnFirstUse:
					// (default) Do nothing; will be instantiated on first use.
					break;
				case WhenToCreateNetworkConnection.SetupPublisherTransportNow:
					InitializePublisherOnFirstUse(addressZeroMq);
					break;
				case WhenToCreateNetworkConnection.SetupSubscriberTransportNow:
					throw new Exception("Error E34875. In the publisher, cannot initialize the subscriber on demand.");
				default:
					throw new Exception("Error E38745. Internal error; at least one publisher or subscriber must be instantiated.");
			}
		}

		#region Initialize publisher on demand.
		private PublisherSocket _publisherSocket;
		private volatile bool _initializePublisherDone = false;
		private readonly object _initializePublisherLock = new object();
		private void InitializePublisherOnFirstUse(string addressZeroMq)
		{
			if (_initializePublisherDone == false) // Double checked locking.
			{
				lock (_initializePublisherLock)
				{
					if (_initializePublisherDone == false)
					{
						_loggerDelegate?.Invoke(string.Format("Publisher socket binding to: {0}\n", AddressZeroMq));
						_publisherSocket = NetMqTransportShared.Instance(_loggerDelegate).GetSharedPublisherSocket(addressZeroMq);
						_initializePublisherDone = true;
					}
				}
			}
		}
		#endregion

		#region Implement IObserver<T> (i.e. the publisher).
		public void OnNext(T message)
		{
			try
			{
				InitializePublisherOnFirstUse(this.AddressZeroMq);

				byte[] serialized = message.SerializeProtoBuf<T>();

				// Publish message using ZeroMQ as the transport mechanism.
				_publisherSocket.SendMoreFrame(SubscriberFilterName)
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
			InitializePublisherOnFirstUse(this.AddressZeroMq);

			var exceptionWrapper = new SerializableException(exception);
			byte[] serializedException = exceptionWrapper.SerializeException();
			string exceptionAsString = exception.ToString();

			_publisherSocket.SendMoreFrame(SubscriberFilterName)
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
			InitializePublisherOnFirstUse(this.AddressZeroMq);

			_publisherSocket.SendMoreFrame(SubscriberFilterName)
				.SendFrame("C"); // "N", "E" or "C" for "OnNext", "OnError" or "OnCompleted".
		}
		#endregion

		public void Dispose()
		{
			_cancellationTokenSource.Cancel();
		}
	}
}
