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

The new API is virtually identical:

```csharp
var subject = new SubjectNetMQ<int>("tcp://127.0.0.1:56001");
subject.Subscribe(message =>
{
	// Receives 42.
});
subject.OnNext(42);
```

If we want to run in separate processes:

```csharp
// Machine 1
var subject = new SubjectNetMQ<int>("tcp://127.0.0.1:56001");
subject.Subscribe(message =>
{
	// Receives 42.
});

// Machine 2
var subject = new SubjectNetMQ<int>("tcp://127.0.0.1:56001");
subject.OnNext(42); // Sends 42.
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

## Notes - General

- Compatible with all existing RX code. Can use `.Where()`, `.Select()`, `.Buffer()`, `.Throttle()`, etc.
- Runs at >120,000 messages per second on localhost.
- Supports `.OnNext()`, `.OnException()`, and `.OnCompleted()`.
- Properly passes exceptions across the wire.
- Supported by a full suite of unit tests.

## Notes - Shared Transport

*Note: this limitation may be removed in the next version which uses Router/Dealer sockets.*

There are no limitations on the number of subscribers on a single endpoint, e.g. `tcp://127.0.0.1:56001`. However, only one process can publish on an endpoint, e.g. `tcp://127.0.0.1:56001`.

Within a process, a single shared transport is used for all publishing, e.g.:

```csharp
var subject1 = new SubjectNetMQ<MyMessage1>("tcp://127.0.0.1:56001");
subject1.OnNext(42); // We are publishing, automatically sets up a transport as a publisher.

var subject2 = new SubjectNetMQ<MyMessage2>("tcp://127.0.0.1:56001"); 
subject2.OnNext(42); // We are publishing, automatically reuses the shared transport.
```

However, if a **second process** attempts to bind to the publishing endpoint in the **first process**, an "in-use" exception will be thrown, e.g.

```csharp
// Process 1
var subject1 = new SubjectNetMQ<MyMessage1>("tcp://127.0.0.1:56001");
subject1.OnNext(42); // We are publishing, automatically binds as a publisher.

// Process 2 (fails)
var subject2 = new SubjectNetMQ<MyMessage2>("tcp://127.0.0.1:56001"); 
subject1.OnNext(42); // We are publishing,  automatically attempts to bind to the publisher (which fails as its in use).
// throws exception at this point
```

In practice, this is probably what we want: we don't want two processes publishing on the same endpoint, as the subscribers will get duplicate messages. This could be solved by replacing the Pub/Sub with Router/Dealer behind the scenes, however, this introduces other, more advanced limitations (e.g. do we want the same messages to be duplicated on each subscriber, and what if the process that hosts the Dealer is stopped?).

## Wiki

See the [Wiki with more documentation](https://github.com/NetMQ/NetMQ.ReactiveExtensions/wiki).

