# NetMQ.ReactiveExtensions

Effortlessly send messages anywhere using Reactive Extensions (RX). Uses NetMQ as the transport layer.

The libary is very simple to use. With a couple of lines of code, we can send any RX stream between different processes or machines.

The API is a drop-in replacement for `Subject<T>` from Reactive Extensions (RX).

As a refresher, to use `Subject<T>` in Reactive Extensions (RX):

```csharp
Subject<int> subject = new Subject<int>();
	subject.Subscribe(message =>
	  {
		// Receives 42.
	  });
	subject.OnNext(42);
```

The new API is virtually identical:

```csharp
SubjectNetMQ<int> subject = new SubjectNetMQ<int>("tcp://127.0.0.1:56001");
	subject.Subscribe(message =>
	  {
		// Receives 42.
	  });
	subject.OnNext(42);
```

Currently, serialization is performed using ProtoBuf, so we have to annotate POCO classes with `ProtoContract` and `ProtoMember` like this:

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

To check out the demos, see:
- Publisher: Project `NetMQ.ReactiveExtensions.SamplePublisher`
- Subscriber: Project `NetMQ.ReactiveExtensions.SampleSubscriber`
- Sample unit tests: Project `NetMQ.ReactiveExtensions.Tests`

Notes:
- Compatible with all existing RX code. Can use `.Where()`, `.Select()`, etc.
- Runs at 120,000 messages per second on localhost.
- Supports `.OnNext()`, `.OnException()`, and `.OnCompleted()`.
- Properly passes exceptions across the wire.
- Supported by many unit tests.

