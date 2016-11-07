using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetMQ.Sockets;
using NUnit.Framework;
// ReSharper disable InconsistentNaming

namespace NetMQ.ReactiveExtensions.Tests
{
    [Ignore]
    [TestFixture]
    public class RouterDealer_Test2
    {
        private void PrintFrames(string operationType, NetMQMessage message)
        {
            for (var i = 0; i < message.FrameCount; i++)
            {
                Console.WriteLine("{0} Socket : Frame[{1}] = {2}", operationType, i,
                    message[i].ConvertToString());
            }
        }

        private void Client_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var hasmore = false;
            e.Socket.Receive(out hasmore);
            if (hasmore)
            {
                var result = e.Socket.ReceiveFrameString(out hasmore);
                Console.WriteLine("REPLY {0}", result);
            }
        }

        [Test]
        public void Messages_From_Dealer_To_Router()
        {
            var maxMessage = 5;
            var cd = new CountdownEvent(maxMessage);

            Console.Write("Test sending message from subscribers (dealer) to publisher(router).\n");

            using (var publisher = new RouterSocket())
            using (var subscriber = new DealerSocket())
            using (var poller = new NetMQPoller {subscriber})
            {
                var port = publisher.BindRandomPort("tcp://*");
                subscriber.Connect("tcp://127.0.0.1:" + port);
                subscriber.ReceiveReady += (sender, e) =>
                {
                    var strs = e.Socket.ReceiveMultipartStrings();
                    foreach (var str in strs)
                    {
                        Console.WriteLine(str);
                    }
                    cd.Signal();
                };
                var clientId = Encoding.Unicode.GetBytes("ClientId");
                subscriber.Options.Identity = clientId;

                const string request = "GET /\r\n";

                const string response = "HTTP/1.0 200 OK\r\n" +
                                        "Content-Type: text/plain\r\n" +
                                        "\r\n" +
                                        "Hello, World!";

                subscriber.SendFrame(request);

                var serverId = publisher.ReceiveFrameBytes();
                Assert.AreEqual(request, publisher.ReceiveFrameString());

                for (var i = 0; i < maxMessage; i++)
                {
                    publisher.SendMoreFrame(serverId).SendFrame(response);
                }

                poller.RunAsync();

                if (cd.Wait(TimeSpan.FromSeconds(10)) == false) // Blocks until _countdown.Signal has been called.
                {
                    Assert.Fail("Timed out, this test should complete in less than 10 seconds.");
                }
            }
        }

        [Test]
        public void Messages_From_Router_To_Dealer()
        {
            Console.Write("Test sending message from publisher(router) to subscribers (dealer).\n");

            var maxMessage = 5;
            var cd = new CountdownEvent(maxMessage);

            string endpoint;

            using (var publisher = new RouterSocket())
            using (var subscriber = new DealerSocket())
            using (var poller = new NetMQPoller {subscriber})
            {
                publisher.Bind("tcp://127.0.0.1:0");
                endpoint = publisher.Options.LastEndpoint;

                subscriber.Connect(endpoint);
                subscriber.ReceiveReady += (sender, e) =>
                {
                    var strs = e.Socket.ReceiveMultipartStrings();
                    foreach (var str in strs)
                    {
                        Console.WriteLine("Subscribe: " + str);
                    }
                    cd.Signal();
                };
                var clientId = Encoding.Unicode.GetBytes("ClientId");
                subscriber.Options.Identity = clientId;

                const string request = "Ping";

                // Work around "feature" of router/dealer: the publisher does not know the subscriber exists, until it
                // sends at least one message which makes it necessary to open the connection. I believe this is a
                // low-level feature of the TCP/IP transport.
                subscriber.SendFrame(request); // Ping.

                var serverId = publisher.ReceiveFrameBytes();
                Assert.AreEqual(request, publisher.ReceiveFrameString());

                for (var i = 0; i < maxMessage; i++)
                {
                    var msg = string.Format("[message: {0}]", i);
                    Console.Write("Publish: {0}\n", msg);
                    publisher.SendMoreFrame(serverId).SendFrame(msg);
                }

                poller.RunAsync();

                if (cd.Wait(TimeSpan.FromSeconds(10)) == false) // Blocks until _countdown.Signal has been called.
                {
                    Assert.Fail("Timed out, this test should complete in less than 10 seconds.");
                }
            }
        }

        [Test]
        public void Messages_From_Router_To_Dealer_With_Subscription()
        {
            Console.Write("Test sending message from publisher(router) to subscribers (dealer).\n");

            var maxMessage = 5;
            var cd = new CountdownEvent(maxMessage);

            string endpoint;

            using (var publisher = new RouterSocket())
            using (var subscriber = new DealerSocket())
            using (var poller = new NetMQPoller {subscriber})
            {
                publisher.Bind("tcp://127.0.0.1:0");
                endpoint = publisher.Options.LastEndpoint;

                subscriber.Connect(endpoint);
                subscriber.ReceiveReady += (sender, e) =>
                {
                    var strs = e.Socket.ReceiveMultipartStrings();
                    foreach (var str in strs)
                    {
                        Console.WriteLine("Subscribe: " + str);
                    }
                    cd.Signal();
                };
                var clientId = Encoding.Unicode.GetBytes("ClientIdTheIsLongerThen32BytesForSureAbsolutelySure");
                subscriber.Options.Identity = clientId;

                const string request = "Ping";

                // Work around "feature" of router/dealer: the publisher does not know the subscriber exists, until it
                // sends at least one message which makes it necessary to open the connection. I believe this is a
                // low-level feature of the TCP/IP transport.
                subscriber.SendFrame(request); // Ping.

                var serverId = publisher.ReceiveFrameBytes();
                //Assert.AreEqual(request, publisher.ReceiveFrameString());

                for (var i = 0; i < maxMessage; i++)
                {
                    var msg = string.Format("[message: {0}]", i);
                    Console.Write("Publish: {0}\n", msg);
                    publisher.SendMoreFrame(serverId).SendFrame(msg);
                    //publisher.SendMoreFrame("").SendFrame(msg);
                }

                poller.RunAsync();

                if (cd.Wait(TimeSpan.FromSeconds(10)) == false) // Blocks until _countdown.Signal has been called.
                {
                    Assert.Fail("Timed out, this test should complete in less than 10 seconds.");
                }
            }
        }

        /// <summary>
        ///     Intent: Modified from example in Router-Dealer docs, see:
        ///     https://github.com/zeromq/netmq/blob/master/docs/router-dealer.md
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

            string endpoint;

            using (var server = new RouterSocket("@tcp://127.0.0.1:0"))
                // If we specify 0, it will choose a random port for us.
            {
                endpoint = server.Options.LastEndpoint; // Lets us know which port was chosen.
                Console.Write("Last endpoint, including port: {0}\n", server.Options.LastEndpoint);
                using (var poller = new NetMQPoller())
                {
                    // Start some threads, each with its own DealerSocket
                    // to talk to the server socket. Creates lots of sockets,
                    // but no nasty race conditions no shared state, each
                    // thread has its own socket, happy days.
                    for (var i = 0; i < 4; i++)
                    {
                        Task.Factory.StartNew(state =>
                        {
                            DealerSocket client = null;

                            if (!clientSocketPerThread.IsValueCreated)
                            {
                                client = new DealerSocket();
                                client.Options.Identity =
                                    Encoding.Unicode.GetBytes(state.ToString());
                                client.Connect(endpoint);
                                //client.ReceiveReady += Client_ReceiveReady;
                                clientSocketPerThread.Value = client;
                                poller.Add(client);
                            }
                            else
                            {
                                client = clientSocketPerThread.Value;
                            }

                            Thread.Sleep(3000); // Wait until server is up.
                            client.SendFrame("Ping");

                            while (true)
                            {
                                Console.Write("Client {0}: Waiting for ping...\n", i);
                                // Work around "feature" of router/dealer: the publisher does not know the subscriber exists, until it
                                // sends at least one message which makes it necessary to open the connection. I believe this is a
                                // low-level feature of the TCP/IP transport.

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
                    var sequenceNo = 0;
                    for (var i = 0; i < 10; i++)
                    {
                        var messageToServer = new NetMQMessage();
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
    }
}