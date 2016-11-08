using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NetMQ.Monitoring;
using NetMQ.Sockets;
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable InvertIf
#pragma warning disable 649

namespace NetMQ.ReactiveExtensions
{
	public class SubjectNetMQ<T> : IDisposable, ISubjectNetMQ<T>, IObservable<T>, IObserver<T>
	{
		private readonly PublisherNetMq<T> _publisher;
		private readonly SubscriberNetMq<T> _subscriber;

		public SubjectNetMQ(string addressZeroMq, string subscriberFilterName = null, WhenToCreateNetworkConnection whenToCreateNetworkConnection = WhenToCreateNetworkConnection.LazyConnectOnFirstUse, CancellationTokenSource cancellationTokenSource = default(CancellationTokenSource), Action<string> loggerDelegate = null)
		{
			_publisher = new PublisherNetMq<T>(addressZeroMq, subscriberFilterName, WhenToCreateNetworkConnection.LazyConnectOnFirstUse, cancellationTokenSource, loggerDelegate);
			_subscriber = new SubscriberNetMq<T>(addressZeroMq, subscriberFilterName, WhenToCreateNetworkConnection.LazyConnectOnFirstUse, cancellationTokenSource, loggerDelegate);
		}

		public string SubscriberFilterName
		{
			get { return _publisher.SubscriberFilterName; }
		}

		public string AddressZeroMq
		{
			get { return _publisher.SubscriberFilterName; }
		}

		public IDisposable Subscribe(IObserver<T> observer)
		{
			return _subscriber.Subscribe(observer);
		}

		public void OnNext(T value)
		{
			_publisher.OnNext(value);
		}

		public void OnError(Exception error)
		{
			_publisher.OnError(error);
		}

		public void OnCompleted()
		{
			_publisher.OnCompleted();
		}

		public void Dispose()
		{
			_publisher.Dispose();
			_subscriber.Dispose();
		}
	}
}


