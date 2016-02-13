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

			var subject = new SubjectNetMQ<MyMessage>("tcp://127.0.0.1:56001", loggerDelegate: msg => Console.Write(msg));

			subject.Subscribe(message =>
			{
				// Also subscribe to ourself!
				Console.Write("Received: {0}, '{1}'.\n", message.Num, message.Name);
			});

			int i = 0;
			while (true)
			{
				var message = new MyMessage(i, "Bob");
				subject.OnNext(message);

				Console.Write("Published: {0}, '{1}'.\n", message.Num, message.Name);
				Thread.Sleep(TimeSpan.FromMilliseconds(1000));
				i++;
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
