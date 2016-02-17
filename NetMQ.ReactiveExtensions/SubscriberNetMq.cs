using System;
using System.Threading;

namespace NetMQ.ReactiveExtensions
{
	/// <summary>
	///	Intent: Create a new subscriber of type T, using NetMQ as the transport layer.
	/// </summary>
	/// <typeparam name="T">Type we want to create to.</typeparam>
	public class SubscriberNetMq<T>
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
		public SubscriberNetMq(string addressZeroMq, string subscriberFilterName = null, CancellationTokenSource cancellationTokenSource = default(CancellationTokenSource), Action<string> loggerDelegate = null)
		{
			//return null;
			/*return new SubjectNetMQ<T>(
				addressZeroMq: addressZeroMq,
				subscriberFilterName: subscriberFilterName,
				whenToCreateNetworkConnection: WhenToCreateNetworkConnection.SetupPublisherTransportNow,
				cancellationTokenSource: cancellationTokenSource,
				loggerDelegate: loggerDelegate
			);*/
		}
	}
}