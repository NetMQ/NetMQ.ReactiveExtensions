# NetMQ.ReactiveExtensions

[![NuGet](https://img.shields.io/nuget/v/NetMQ.ReactiveExtensions.svg)](https://www.nuget.org/packages/NetMQ.ReactiveExtensions/) [![Build](https://img.shields.io/appveyor/ci/drewnoakes/netmq-reactiveextensions.svg)](https://ci.appveyor.com/project/drewnoakes/netmq-reactiveextensions)

Effortlessly send messages anywhere on the network using Reactive Extensions (RX). Uses NetMQ as the transport layer.

## Sample Code

The API is a drop-in replacement for `Subject<T>` from Reactive Extensions (RX).

As a refresher, to use `Subject<T>` in Reactive Extensions (RX):

```csharp
var subject = new Subject<int>();
subject.Subscribe(message =>
{
	// Receives 42.
});
subject.OnNext(42);
```

The new API starts with a drop-in replacement for `Subject<T>`:

```csharp
var subject = new SubjectNetMQ<int>("tcp://127.0.0.1:56001");
subject.Subscribe(message =>
{
	// Receives 42.
});
subject.OnNext(42); // Sends 42.
```

This is great for a demo, but is not recommended for any real life application.

For those of us familiar with Reactive Extensions (RX), `Subject<T>` is a combination of a publisher and a subscriber. If we are running a real-life application, we should separate out the publisher and the subscriber, because this means we can create the connection earlier which makes the transport setup more deterministic:

```csharp
var publisher = new PublisherNetMQ<int>("tcp://127.0.0.1:56001");
var subscriber = new SubscriberNetMQ<int>("tcp://127.0.0.1:56001");
subscriber.Subscribe(message =>
{
	// Receives 42.
});
publisher.OnNext(42); // Sends 42.
```

If we want to run in separate applications:

```csharp
// Application 1 (subscriber)
var subscriber = new SubscriberNetMQ<int>("tcp://127.0.0.1:56001");
subscriber.Subscribe(message =>
{
	// Receives 42.
});

// Application 2 (subscriber)
var subscriber = new SubscriberNetMQ<int>("tcp://127.0.0.1:56001");
subscriber.Subscribe(message =>
{
	// Receives 42.
});

// Application 3 (publisher)
var publisher = new PublisherNetMQ<int>("tcp://127.0.0.1:56001");
publisher.OnNext(42); // Sends 42.
```

Currently, serialization is performed using [ProtoBuf](https://github.com/mgravell/protobuf-net "ProtoBuf"). It will handle simple types such as `int` without annotation, but if we want to send more complex classes, we have to annotate like this:

```csharp
[ProtoContract]
public struct MyMessage
{
	[ProtoMember(1)]
	public int Num { get; set; }
	[ProtoMember(2)]
	public string Name { get; set; }
}
```

## NuGet Package

[![NuGet](https://img.shields.io/nuget/v/NetMQ.ReactiveExtensions.svg)](https://www.nuget.org/packages/NetMQ.ReactiveExtensions/)

See [NetMQ.ReactiveExtensions](https://www.nuget.org/packages/NetMQ.ReactiveExtensions/).

## Demos

To check out the demos, see:
- Publisher: Project `NetMQ.ReactiveExtensions.SamplePublisher`
- Subscriber: Project `NetMQ.ReactiveExtensions.SampleSubscriber`
- Sample unit tests: Project `NetMQ.ReactiveExtensions.Tests`

## Projects like this one

- See [Obvs](https://github.com/inter8ection/Obvs), an fantastic RX wrapper which supports many transport layers including NetMQ, RabbitMQ and Azure, and many serialization methods including ProtoBuf and MsgPack.
- See [Obvs.NetMQ](https://github.com/inter8ection/Obvs.Netmq), the RX wrapper with NetMQ as the transport layer. 
- Search for [all packages on NuGet that depend on RX](http://nugetmusthaves.com/Dependencies/Rx-Linq), and pick out the ones that are related to message buses.

## Notes - General

- Compatible with all existing RX code. Can use `.Where()`, `.Select()`, `.Buffer()`, `.Throttle()`, etc.
- Runs at >120,000 messages per second on localhost.
- Supports `.OnNext()`, `.OnException()`, and `.OnCompleted()`.
- Properly passes exceptions across the wire.
- Supported by a full suite of unit tests.

## Notes - Use Cases

### Good Use Cases

This library is good for any situation where we have one application that wants to publish data to many other applications (i.e. one-to-many publishing).

Examples of good use cases for this library:

- A C# service wishes to publish data to an Excel plugin written in C#.
- A C# service wishes to publish health statistics to any application that is listening.
- In Service Oriented Architecture (SOA), a single service wishes to publish data to all GUI applications that are listening.
- We absolutely need good point-to-point performance (this library does 180,000 messages per second on localhost).
- We want to use multicast, using the `pgm://` protocol.

### Bad Use Cases

This library is *not* designed for many applications, all wanting to talk to each other (i.e. many-to-many publishing), because configuration becomes very difficult as we need to keep track of many endpoints. If we want things to just work, we need some sort of centralized message broker sitting in the middle. This message broker makes configuration very easy: each application only has to know the address of the central message broker.

Examples of bad use cases for this library:

- A service bus where any service can talk to any other service.
- If we suddenly realize that we have 7 applications using this library, and they all want to talk to each other.

If we really want good support for many-to-many communication, and we are not too worried about speed, we will have to try a transport that has some form of centralized message broker, see [Obvs](https://github.com/inter8ection/Obvs).

If we want many-to-many communication, and we also want ultra fast speed for a handful of links in the network, there is a hybrid approach: use a message broker by default for most things, and for speed, use this library, and use the message broker to fetch dynamic endpoint configuration. 

## Notes - Shared Transport

There are no limitations on the number of subscribers to a single endpoint, e.g. `tcp://127.0.0.1:56001`. However, only one process can publish on an endpoint, e.g. `tcp://127.0.0.1:56001`.

Within a [process](http://superuser.com/questions/209654/whats-the-difference-between-an-application-process-and-services), a single shared transport is used for all publishing, e.g.:

```csharp
var publisher1 = new PublisherNetMQ<MyMessage1>("tcp://127.0.0.1:56001");
subject1.OnNext(42); // Automatically sets up a transport as a publisher.

var publisher2 = new PublisherNetMQ<MyMessage2>("tcp://127.0.0.1:56001"); 
subject2.OnNext(42); // Automatically reuses the shared transport.
```

However, if a [**second process**](http://superuser.com/questions/209654/whats-the-difference-between-an-application-process-and-services) attempts to bind to the publishing endpoint in the [**first process**](http://superuser.com/questions/209654/whats-the-difference-between-an-application-process-and-services), an "in-use" exception will be thrown, e.g.

```csharp
// Application 1
var publisher1 = new PublisherNetMQ<MyMessage1>("tcp://127.0.0.1:56001");
subject1.OnNext(42); // Automatically binds as a publisher.

// Application 2 (fails)
var publisher2 = new PublisherNetMQ<MyMessage2>("tcp://127.0.0.1:56001"); 
subject1.OnNext(42); // Automatically attempts to bind to the publisher.
// throws exception at this point: "Cannot bind to 'tcp://127.0.0.1:56001'.
```

If we really want good support for many-to-many communication, we will have to support a transport that has some form of centralized message broker, see [Obvs](https://github.com/inter8ection/Obvs).

## Wiki

See the [Wiki with more documentation](https://github.com/NetMQ/NetMQ.ReactiveExtensions/wiki).



