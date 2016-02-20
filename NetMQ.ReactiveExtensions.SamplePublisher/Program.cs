using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;

namespace NetMQ.ReactiveExtensions.SampleServer
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.Write("Reactive Extensions publisher demo:\n");

			string endPoint = "tcp://127.0.0.1:56001";
			if (args.Length >= 1)
			{
				endPoint = args[0];
			}

			Console.Write("Endpoint: {0}\n", endPoint);

			// Debug: Subscribe to ourself.
			{
				var subscriber = new SubscriberNetMq<MyMessage>(endPoint, loggerDelegate: msg => Console.Write(msg));
				// Debug: subscribe to ourself. If you run the "SampleSubscriber" project now, you will see the same
				// messages appearing in that subscriber too.
				subscriber.Subscribe(message =>
				{
					Console.Write("Received: {0}, '{1}'.\n", message.Num, message.Name);
				});
			}

			// Publisher.
			{
				var publisher = new PublisherNetMq<MyMessage>(endPoint, loggerDelegate: msg => Console.Write(msg));

				int i = 0;
				while (true)
				{
					var message = new MyMessage(i, "Bob");

					// When we call "OnNext", it binds a publisher to this endpoint endpoint.
					publisher.OnNext(message);

					Console.Write("Published: {0}, '{1}'.\n", message.Num, message.Name);
					Thread.Sleep(TimeSpan.FromMilliseconds(1000));
					i++;
				}
			}

			// NOTE: If you run the "SampleSubscriber" project now, you will see the same messages appearing in the subscriber.
		}
	}

	[ProtoContract]
	public class MyMessage
	{
		public MyMessage()
		{
			
		}

		public MyMessage(int num, string name)
		{
			Num = num;
			Name = name;
		}

		[ProtoMember(1)]
		public int Num { get; set; }

		[ProtoMember(2)]
		public string Name { get; set; }
	}
}
