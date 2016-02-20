using System;

namespace NetMQ.ReactiveExtensions
{
	public interface IPublisherNetMq<T>
	{
		string AddressZeroMq { get; set; }
		string SubscriberFilterName { get; set; }

		void Dispose();
		void OnCompleted();
		void OnError(Exception exception);
		void OnNext(T message);
	}
}