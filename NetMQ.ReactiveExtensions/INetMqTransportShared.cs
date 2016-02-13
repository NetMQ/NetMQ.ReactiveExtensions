using NetMQ.Sockets;

namespace NetMQ.ReactiveExtensions
{
	public interface INetMqTransportShared
	{
		/// <summary>
		///	Intent: Get a publisher socket. If the same port is already opened, return it.
		/// </summary>
		/// <param name="addressZeroMq">ZeroMQ address, e.g. "tcp://127.0.0.1:56001".</param>
		/// <returns>A publisher socket. If we already have one open, it will return the existing one.</returns>
		PublisherSocket GetSharedPublisherSocket(string addressZeroMq);

		/// <summary>
		///	Intent: Get a subscriber socket. If the same port is already opened, return it.
		/// </summary>
		/// <param name="addressZeroMq">ZeroMQ address, e.g. "tcp://127.0.0.1:56001".</param>
		/// <returns>A subscriber socket. If we already have one open, it will return the existing one.</returns>
		SubscriberSocket GetSharedSubscriberSocket(string addressZeroMq);
	}
}