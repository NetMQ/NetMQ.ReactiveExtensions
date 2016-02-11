using System;

namespace NetMQ.ReactiveExtensions
{
	/// <summary>
	/// Intent: Send messages anywhere on the network using Reactive Extensions (RX). Uses NetMQ as the transport layer.
	/// The API is a drop-in replacement for ISubject of T from Reactive Extensions, see
    /// https://github.com/NetMQ/NetMQ.ReactiveExtensions.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface ISubjectNetMQ<T> : IObservable<T>, IObserver<T>, IDisposable
	{
		/// <summary>
		/// Intent: Subscriber Filter name. Defaults to the type name T. Allows many types to get sent over the same
        /// transport connection.
		/// </summary>
		string SubscriberFilterName { get; }

		/// <summary>
		/// Intent: The current endpoint address specified in the constructor, e.g. "tcp://127.0.0.1:56001".
		/// </summary>
		string AddressZeroMq { get; }

		/// <summary>
		/// Intent: True if there are any subscribers registered in the current process.
		/// </summary>
		bool HasObservers { get; }
	}

	/// <summary>
	/// Intent: Control when the network connection is created.
	/// </summary>
	public enum WhenToCreateNetworkConnection
	{
		/// <summary>
		/// Intent: Default mode. The network connection is lazily created on first use.
		/// </summary>
		LazyConnectOnFirstUse = 0,

		/// <summary>
		/// Intent: (currently unsupported). The network connections are created when the class is instantiated.
		/// </summary>
		InstantConnectOnClassInstantiation = 1,
	}
}