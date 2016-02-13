using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetMQ.Sockets;
using NUnit.Framework;

namespace NetMQ.ReactiveExtensions.Tests
{
	[TestFixture]
	public class RouterDealerTest2
	{
		/// <summary>
		///	Intent: Modified from example in Router-Dealer docs, see:
        ///	https://github.com/zeromq/netmq/blob/master/docs/router-dealer.md
		/// </summary>
		[Test]
		public void Router_Dealer_Demonstrating_Messages_From_Publisher_To_Subscribers()
		{
			// NOTES
			// 1. Use ThreadLocal<DealerSocket> where each thread has
			//    its own client DealerSocket to talk to server
			// 2. Each thread can send using it own socket
			// 3. Each thread socket is added to poller

			const int delay = 500; // millis

			var clientSocketPerThread = new ThreadLocal<DealerSocket>();

			using (var server = new RouterSocket("@tcp://127.0.0.1:5556"))
			{
				using (var poller = new NetMQPoller())
				{
					// Start some threads, each with its own DealerSocket
					// to talk to the server socket. Creates lots of sockets,
					// but no nasty race conditions no shared state, each
					// thread has its own socket, happy days.
					for (int i = 0; i < 4; i++)
					{
						Task.Factory.StartNew(state =>
						{
							DealerSocket client = null;

							if (!clientSocketPerThread.IsValueCreated)
							{
								client = new DealerSocket();
								client.Options.Identity =
									Encoding.Unicode.GetBytes(state.ToString());
								client.Connect("tcp://127.0.0.1:5556");
								//client.ReceiveReady += Client_ReceiveReady;
								clientSocketPerThread.Value = client;
								poller.Add(client);
							}
							else
							{
								client = clientSocketPerThread.Value;
							}

							while (true)
							{
								var clientMessage = client.ReceiveMultipartMessage();
								Console.WriteLine("======================================");
								Console.WriteLine(" INCOMING CLIENT MESSAGE FROM SERVER");
								Console.WriteLine("======================================");
								PrintFrames("Server receiving", clientMessage);
							}

						},
							string.Format("client {0}", i),
							TaskCreationOptions.LongRunning);
					}

					// start the poller
					poller.RunAsync();

					// server loop
					int sequenceNo = 0;
					for (int i=0;i<5;i++)
					{
						NetMQMessage messageToServer = new NetMQMessage();
						messageToServer.AppendEmptyFrame();
						messageToServer.Append(sequenceNo.ToString());
						sequenceNo++;
						Console.WriteLine("======================================");
						Console.WriteLine(" OUTGOING MESSAGE {0} TO CLIENTS ", sequenceNo);
						Console.WriteLine("======================================");
						PrintFrames("Client Sending", messageToServer);
						server.SendMultipartMessage(messageToServer);
						Thread.Sleep(delay);						
					}
					Console.WriteLine("Finished.");
				}
			}
		}

		void PrintFrames(string operationType, NetMQMessage message)
		{
			for (int i = 0; i < message.FrameCount; i++)
			{
				Console.WriteLine("{0} Socket : Frame[{1}] = {2}", operationType, i,
					message[i].ConvertToString());
			}
		}

		void Client_ReceiveReady(object sender, NetMQSocketEventArgs e)
		{
			bool hasmore = false;
			e.Socket.Receive(out hasmore);
			if (hasmore)
			{
				string result = e.Socket.ReceiveFrameString(out hasmore);
				Console.WriteLine("REPLY {0}", result);
			}
		}

		[Test]
		public void Two_Messages_FromRouter_To_Dealer()
		{
			using (var server = new RouterSocket())
			using (var client = new DealerSocket())
			using (var poller = new NetMQPoller { client })
			{
				var port = server.BindRandomPort("tcp://*");
				client.Connect("tcp://127.0.0.1:" + port);
				var cnt = 0;
				client.ReceiveReady += (sender, e) =>
				{
					var strs = e.Socket.ReceiveMultipartStrings();
					foreach (var str in strs)
					{
						Console.WriteLine(str);
					}
					cnt++;
					if (cnt == 2)
					{
						poller.Stop();
					}
				};
				byte[] clientId = Encoding.Unicode.GetBytes("ClientId");
				client.Options.Identity = clientId;

				const string request = "GET /\r\n";

				const string response = "HTTP/1.0 200 OK\r\n" +
						"Content-Type: text/plain\r\n" +
						"\r\n" +
						"Hello, World!";

				client.SendFrame(request);

				byte[] serverId = server.ReceiveFrameBytes();
				Assert.AreEqual(request, server.ReceiveFrameString());

				// two messages in a row, not frames
				server.SendMoreFrame(serverId).SendFrame(response);
				server.SendMoreFrame(serverId).SendFrame(response);

				poller.Run();
			}
		}
	}
}
