using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace NetMQ.ReactiveExtensions
{



	/// <summary>
	/// 
	/// </summary>
	public static class PublisherCreateNewExtension<T>
	{
		///  <summary>
		/// 	Intent: Create a new publisher, using NetMQ as the transport layer.
		///  </summary>
		///  <param name="addressZeroMq">ZeroMq address to bind to, e.g. "tcp://localhost:56001</param>
		///  <param name="subscriberFilterName">Filter name on receiver. If you do not set this, it will default to the
		///  type name of T, and everything will just work.</param>
		/// <param name="cancellationTokenSource"></param>
		/// <param name="loggerDelegate"></param>
		/// <returns></returns>
		public static IObservable<T> PublisherNetMq(string addressZeroMq, string subscriberFilterName = null, CancellationTokenSource cancellationTokenSource = default(CancellationTokenSource), Action<string> loggerDelegate = null)
		{
			return null;
			/*/*return new SubjectNetMQ<T>(
				subscriberFilterName: subscriberFilterName,
				addressZeroMq: addressZeroMq,
				cancellationTokenSource: cancellationTokenSource,
				loggerDelegate: loggerDelegate);#1#

			_mWhenToCreateNetworkConnection = whenToCreateNetworkConnection;
			m_cancellationTokenSource = cancellationTokenSource;
			_loggerDelegate = loggerDelegate;

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
					throw new Exception("Error E38742. Internal error; subscription length can never be longer than 32 characters.");
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
					InitializeSubscriberOnFirstUse(addressZeroMq);
					break;
				default:
					throw new Exception("Error E38745. Internal error; at least one publisher or subscriber must be instantiated.");
			}*/
		}

	}

	/// <summary>
	///	Intent: Create a new subscriber of type T.
	/// </summary>
	/// <typeparam name="T">Type we want to create to.</typeparam>
	public static class SubscriberCreateNewExtension<T>
	{
		/// <summary>
		/// Intent: Create a new subscriber, using NetMQ as the transport layer.
		/// </summary>
		/// <param name="addressZeroMq">ZeroMq address to bind to, e.g. "tcp://localhost:56001".</param>
		/// <param name="subscriberFilterName">Filter name on receiver. If you do not set this, it will default to the
		/// type name of T, and everything will just work.</param>
		/// <param name="cancellationTokenSource"></param>
		/// <param name="loggerDelegate"></param>
		/// <returns></returns>
		public static IObserver<T> SubscriberNetMq(string addressZeroMq, string subscriberFilterName = null, CancellationTokenSource cancellationTokenSource = default(CancellationTokenSource), Action<string> loggerDelegate = null)
		{
			return new SubjectNetMQ<T>(
				addressZeroMq: addressZeroMq,
				subscriberFilterName: subscriberFilterName,
				whenToCreateNetworkConnection: WhenToCreateNetworkConnection.SetupPublisherTransportNow,
				cancellationTokenSource: cancellationTokenSource,
				loggerDelegate: loggerDelegate
			);
		}
	}
}
