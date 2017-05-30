# NetMQ.ReactiveExtensions

[![Build](https://img.shields.io/appveyor/ci/drewnoakes/netmq-reactiveextensions.svg)](https://ci.appveyor.com/project/drewnoakes/netmq-reactiveextensions) [![NuGet](https://img.shields.io/nuget/v/NetMQ.ReactiveExtensions.svg)](https://www.nuget.org/packages/NetMQ.ReactiveExtensions/) [![NuGet prerelease](https://img.shields.io/nuget/vpre/NetMQ.ReactiveExtensions.svg)](https://www.nuget.org/packages/NetMQ.ReactiveExtensions/)

Effortlessly send messages anywhere on the network using Reactive Extensions (RX). Uses NetMQ as the transport layer. 

Fast! Runs at >120,000 messages per second on localhost (by comparison, Tibco runs at 100,000 on the same machine).

## Sample Code

The API is a drop-in replacement for `Subject<T>` from Reactive Extensions (RX).

As a refresher, to use `Subject<T>` in Reactive Extensions (RX):

```csharp 
var subject = new Subject<int>();
subject.Subscribe(message =>
{
	Console.Write(message); // Prints "42".
});
subject.OnNext(42);
```

The new API starts with a drop-in replacement for `Subject<T>`:

```csharp
var subject = new SubjectNetMQ<int>("tcp://127.0.0.1:56001");
subject.Subscribe(message =>
{
	Console.Write(message); // Prints "42".
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
	Console.Write(message); // Prints "42".
});
publisher.OnNext(42); // Sends 42.
```

If we want to run in separate applications:

```csharp
// Application 1 (subscriber)
var subscriber = new SubscriberNetMQ<int>("tcp://127.0.0.1:56001");
subscriber.Subscribe(message =>
{
	Console.Write(message); // Prints "42".
});

// Application 2 (subscriber)
var subscriber = new SubscriberNetMQ<int>("tcp://127.0.0.1:56001");
subscriber.Subscribe(message =>
{
	Console.Write(message); // Prints "42".
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

var publisher = new PublisherNetMQ<MyMessage>("tcp://127.0.0.1:56001");
var subscriber = new SubscriberNetMQ<MyMessage>("tcp://127.0.0.1:56001");
subscriber.Subscribe(message =>
{
	Console.Write(message.Num); // Prints "42".
	Console.Write(message.Name); // Prints "Bill".
});
publisher.OnNext(new MyMessage(42, "Bill"); 
```

## NuGet Package

[![Build](https://img.shields.io/appveyor/ci/drewnoakes/netmq-reactiveextensions.svg)](https://ci.appveyor.com/project/drewnoakes/netmq-reactiveextensions) [![NuGet](https://img.shields.io/nuget/v/NetMQ.ReactiveExtensions.svg)](https://www.nuget.org/packages/NetMQ.ReactiveExtensions/) [![NuGet prerelease](https://img.shields.io/nuget/vpre/NetMQ.ReactiveExtensions.svg)](https://www.nuget.org/packages/NetMQ.ReactiveExtensions/)

See [NetMQ.ReactiveExtensions](https://www.nuget.org/packages/NetMQ.ReactiveExtensions/).

The NuGet package 0.9.4-rc7 is now compatible with .NET Core 1.1, .NET 4.5, and .NET Standard 1.6. If you want to build it for other platforms, please let me know.

## .NET Core 1.1 Ready

As of v0.9.4-rc7, this package will build for:
- .NET 4.5 and up
- [.NET Core 1.1](https://www.microsoft.com/net/download/core)
- .NET Standard 1.6 and up

As this library supports .NET Standard 1.6 (which is a subset of .NET Core 1.1), this library should be compatible with:
- Windows
- Linux
- Mac

This library is tested on Window and Linux. If it passes it's unit tests on any given platform, then it should perform nicely on different architectures such as Mac.

## Compiling from source

- Install [Visual Studio 2015 Update 3](https://www.visualstudio.com/en-us/news/releasenotes/vs2015-update3-vs).
- Install "[.NET Core 1.1 SDK - Installer](https://www.microsoft.com/net/download/core)" from https://www.microsoft.com/net/download/core.
- NuGet Restore. It may not compile until a manual "[nuget restore](https://docs.nuget.org/ndocs/consume-packages/package-restore)" is performed for each project (this also rebuilds the `project.lock.json` file). You can either do this from the command line, or by right clicking on  the solution and choosing `Restore NuGet packages`.
- If the project does not compile on your machine, raise an issue here on GitHub.

NOTE: Not compatible with .NET Core 1.0 or .NET Core 1.0.1. Must install .NET Core 1.1 and above to avoid potential compile errors.

## Demos

To check out the demos, see:
- Publisher: Project `NetMQ.ReactiveExtensions.SamplePublisher`
- Subscriber: Project `NetMQ.ReactiveExtensions.SampleSubscriber`
- Sample unit tests: Project `NetMQ.ReactiveExtensions.Tests`

## Performance

- Runs at >120,000 messages per second on localhost.

## 100% compatible with Reactive Extensions (RX) 

- Compatible with all existing Reactive Extensions code, as it implements IObservable<T> and IObserver<T> from Microsoft.
- Can use `.Where()`, `.Select()`, `.Buffer()`, `.Throttle()`, etc.
- Supports `.OnNext()`, `.OnException()`, and `.OnCompleted()`.
- Properly passes exceptions across the wire.

## Unit tests

- Supported by a full suite of unit tests.

## Projects like this one that do messaging

- See [Obvs](https://github.com/inter8ection/Obvs), an fantastic RX wrapper which supports many transport layers including NetMQ, RabbitMQ and Azure, and many serialization methods including ProtoBuf and MsgPack.
- See [Obvs.NetMQ](https://github.com/inter8ection/Obvs.Netmq), the RX wrapper with NetMQ as the transport layer. 
- Search for [all packages on NuGet that depend on RX](http://nugetmusthaves.com/Dependencies/Rx-Linq), and pick out the ones that are related to message buses.
- Check out Kafka. It provides many-to-many messaging, with persistance, and multi-node redundancy. And its blindingly fast.

## Wiki

See the [Wiki with more documentation](https://github.com/NetMQ/NetMQ.ReactiveExtensions/wiki).



