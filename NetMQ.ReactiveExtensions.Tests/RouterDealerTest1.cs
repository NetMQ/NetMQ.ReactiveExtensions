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
	public class RouterDealerTest1
	{
		/// <summary>
		///		From Router-Dealer docs, see: https://github.com/zeromq/netmq/blob/master/docs/router-dealer.md
		/// </summary>
		[Test]
		public void Router_Dealer_Demonstrating_Messages_From_Subscribers_To_Publisher()
		{
			// NOTES
			// 1. Use ThreadLocal<DealerSocket> where each thread has
			//    its own client DealerSocket to talk to server
			// 2. Each thread can send using it own socket
			// 3. Each thread socket is added to poller

			const int delay = 3000; // millis

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
								client.ReceiveReady += Client_ReceiveReady;
								clientSocketPerThread.Value = client;
								poller.Add(client);
							}
							else
							{
								client = clientSocketPerThread.Value;
							}

							while (true)
							{
								NetMQMessage messageToServer = new NetMQMessage();
								messageToServer.AppendEmptyFrame();
								messageToServer.Append(state.ToString());
								Console.WriteLine("======================================");
								Console.WriteLine(" OUTGOING MESSAGE TO SERVER ");
								Console.WriteLine("======================================");
								PrintFrames("Client Sending", messageToServer);
								client.SendMultipartMessage(messageToServer);
								Thread.Sleep(delay);
							}

						},
							string.Format("client {0}", i),
							TaskCreationOptions.LongRunning);
					}

					// start the poller
					poller.RunAsync();

					// server loop
					while (true)
					{
						NetMQMessage clientMessage = server.ReceiveMessage();
						Console.WriteLine("======================================");
						Console.WriteLine(" INCOMING CLIENT MESSAGE FROM CLIENT ");
						Console.WriteLine("======================================");
						PrintFrames("Server receiving", clientMessage);
						if (clientMessage.FrameCount == 3)
						{
							var clientAddress = clientMessage[0];
							var clientOriginalMessage = clientMessage[2].ConvertToString();
							string response = string.Format("{0} back from server {1}",
								clientOriginalMessage,
								DateTime.Now.ToLongTimeString());
							var messageToClient = new NetMQMessage();
							messageToClient.Append(clientAddress);
							messageToClient.AppendEmptyFrame();
							messageToClient.Append(response);
							server.SendMultipartMessage(messageToClient);
						}
					}
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
	}
}
