using System;
using System.Diagnostics;
using System.Threading;
using NetMQ.Sockets;
using NUnit.Framework;
// ReSharper disable InconsistentNaming

namespace NetMQ.ReactiveExtensions.Tests
{
    [TestFixture]
    public class NetMQ_ReactiveExtensions_Test_Batch_2
    {
        [Test]
        public void If_Message_Not_Serializable_By_Protobuf_Throw_A_Meaningful_Error()
        {
            NUnitUtils.PrintTestName();
            var sw = Stopwatch.StartNew();

            var freePort = NUnitUtils.TcpPortFree();
            var pubSub = new SubjectNetMQ<MessageNotSerializableByProtobuf>("tcp://127.0.0.1:" + freePort,
                loggerDelegate: Console.Write);
            try
            {
                pubSub.OnNext(new MessageNotSerializableByProtobuf());

                // We should have thrown an exception if the class was not serializable by ProtoBuf-Net.
                Assert.Fail();
            }
            catch (InvalidOperationException ex)
            {
                Assert.True(ex.Message.ToLower().Contains("protobuf"));
                Console.Write("Pass - meaningful message thrown if class was not serializable by ProtoBuf-Net.");
            }
            catch (Exception)
            {
                Assert.Fail();
            }

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }

        [Test]
        public void PubSub_Should_Not_Crash_If_No_Thread_Sleep()
        {
            NUnitUtils.PrintTestName();
            var swAll = Stopwatch.StartNew();

            using (var pub = new PublisherSocket())
            {
                using (var sub = new SubscriberSocket())
                {
                    var freePort = NUnitUtils.TcpPortFree();
                    pub.Bind("tcp://127.0.0.1:" + freePort);
                    sub.Connect("tcp://127.0.0.1:" + freePort);

                    sub.Subscribe("*");

                    var sw = Stopwatch.StartNew();
                    {
                        for (var i = 0; i < 50; i++)
                        {
                            pub.SendFrame("*"); // Ping.

                            Console.Write("*");
                            string topic;
                            var gotTopic = sub.TryReceiveFrameString(TimeSpan.FromMilliseconds(100), out topic);
                            string ping;
                            var gotPing = sub.TryReceiveFrameString(TimeSpan.FromMilliseconds(100), out ping);
                            if (gotTopic)
                            {
                                Console.Write("\n");
                                break;
                            }
                        }
                    }
                    Console.WriteLine("Connected in {0} ms.", sw.ElapsedMilliseconds);
                }
            }
            NUnitUtils.PrintElapsedTime(swAll.Elapsed);
        }

        [Test]
        public void Send_Two_Types_Simultaneously_Over_Same_Transport()
        {
            NUnitUtils.PrintTestName();
            var sw = Stopwatch.StartNew();

            var cd1 = new CountdownEvent(5);
            var cd2 = new CountdownEvent(5);
            {
                var freePort = NUnitUtils.TcpPortFree();

                var pubSub1 = new SubjectNetMQ<MyMessageStructType1>("tcp://127.0.0.1:" + freePort,
                    loggerDelegate: Console.Write);
                var pubSub2 = new SubjectNetMQ<MyMessageStructType2>("tcp://127.0.0.1:" + freePort,
                    loggerDelegate: Console.Write);
                pubSub1.Subscribe(o =>
                {
                    Assert.IsTrue(o.Name == "Bob");
                    Console.Write("Test 1: Num={0}, Name={1}\n", o.Num, o.Name);
                    cd1.Signal();
                },
                    ex => { Console.WriteLine("Exception! {0}", ex.Message); });

                pubSub2.Subscribe(o =>
                {
                    Assert.IsTrue(o.Name == "Bob");
                    Console.Write("Test 2: Num={0}, Name={1}\n", o.Num, o.Name);
                    cd2.Signal();
                },
                    ex => { Console.WriteLine("Exception! {0}", ex.Message); });

                pubSub1.OnNext(new MyMessageStructType1(38, "Bob"));
                pubSub1.OnNext(new MyMessageStructType1(39, "Bob"));
                pubSub1.OnNext(new MyMessageStructType1(40, "Bob"));
                pubSub1.OnNext(new MyMessageStructType1(41, "Bob"));
                pubSub1.OnNext(new MyMessageStructType1(42, "Bob"));

                pubSub2.OnNext(new MyMessageStructType2(38, "Bob"));
                pubSub2.OnNext(new MyMessageStructType2(39, "Bob"));
                pubSub2.OnNext(new MyMessageStructType2(40, "Bob"));
                pubSub2.OnNext(new MyMessageStructType2(41, "Bob"));
                pubSub2.OnNext(new MyMessageStructType2(42, "Bob"));
            }

            if (cd1.Wait(GlobalTimeout.Timeout) == false) // Blocks until _countdown.Signal has been called.
            {
                Assert.Fail("Timed out, this test should complete in {0} seconds.", GlobalTimeout.Timeout.TotalSeconds);
            }

            if (cd2.Wait(GlobalTimeout.Timeout) == false) // Blocks until _countdown.Signal has been called.
            {
                Assert.Fail("Timed out, this test should complete in {0} seconds.", GlobalTimeout.Timeout.TotalSeconds);
            }

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }

        [Test]
        public static void Speed_Test_Publisher_Subscriber()
        {
            NUnitUtils.PrintTestName();

            var sw = Stopwatch.StartNew();
            {
                var max = 100*1000;

                var cd = new CountdownEvent(max);
                var receivedNum = 0;
                {
                    Console.Write("Speed test with {0} messages:\n", max);

                    var freePort = NUnitUtils.TcpPortFree();
                    var publisher = new PublisherNetMq<int>("tcp://127.0.0.1:" + freePort,
                        loggerDelegate: Console.Write);
                    var subscriber = new SubscriberNetMq<int>("tcp://127.0.0.1:" + freePort,
                        loggerDelegate: Console.Write);

                    subscriber.Subscribe(i =>
                    {
                        receivedNum++;
                        cd.Signal();
                        if (i%10000 == 0)
                        {
                            //Console.Write("*");
                        }
                    });

                    sw.Start();
                    for (var i = 0; i < max; i++)
                    {
                        publisher.OnNext(i);
                    }
                }

                if (cd.Wait(GlobalTimeout.Timeout) == false) // Blocks until _countdown.Signal has been called.
                {
                    Assert.Fail("\nTimed out, this test should complete in {0} seconds. receivedNum={1}",
                        GlobalTimeout.Timeout.TotalSeconds,
                        receivedNum);
                }

                // On my machine, achieved >120,000 messages per second.
                NUnitUtils.PrintElapsedTime(sw.Elapsed, max);
            }
        }

        [Test]
        public static void Speed_Test_Subject()
        {
            NUnitUtils.PrintTestName();

            var sw = Stopwatch.StartNew();
            {
                var max = 100*1000;

                var cd = new CountdownEvent(max);
                var receivedNum = 0;
                {
                    Console.Write("Speed test with {0} messages:\n", max);

                    var freePort = NUnitUtils.TcpPortFree();
                    var pubSub = new SubjectNetMQ<int>("tcp://127.0.0.1:" + freePort, loggerDelegate: Console.Write);

                    pubSub.Subscribe(i =>
                    {
                        receivedNum++;
                        cd.Signal();
                        if (i%10000 == 0)
                        {
                            //Console.Write("*");
                        }
                    });

                    sw.Start();
                    for (var i = 0; i < max; i++)
                    {
                        pubSub.OnNext(i);
                    }
                }

                if (cd.Wait(GlobalTimeout.Timeout) == false) // Blocks until _countdown.Signal has been called.
                {
                    Assert.Fail("\nTimed out, this test should complete in {0} seconds. receivedNum={1}",
                        GlobalTimeout.Timeout.TotalSeconds, receivedNum);
                }

                // On my machine, achieved >120,000 messages per second.
                NUnitUtils.PrintElapsedTime(sw.Elapsed, max);
            }
        }

        [Test]
        public void Test_Two_Subscribers()
        {
            NUnitUtils.PrintTestName();
            var sw = Stopwatch.StartNew();

            using (var pub = new PublisherSocket())
            {
                using (var sub1 = new SubscriberSocket())
                {
                    using (var sub2 = new SubscriberSocket())
                    {
                        var freePort = NUnitUtils.TcpPortFree();
                        pub.Bind("tcp://127.0.0.1:" + freePort);
                        sub1.Connect("tcp://127.0.0.1:" + freePort);
                        sub1.Subscribe("A");
                        sub2.Connect("tcp://127.0.0.1:" + freePort);
                        sub2.Subscribe("B");

                        Thread.Sleep(500);

                        var swInner = Stopwatch.StartNew();
                        {
                            pub.SendFrame("A\n"); // Ping.
                            {
                                string topic;
                                var pass1 = sub1.TryReceiveFrameString(TimeSpan.FromMilliseconds(250), out topic);
                                if (pass1)
                                {
                                    Console.Write(topic);
                                }
                                else
                                {
                                    Assert.Fail();
                                }
                            }
                            pub.SendFrame("B\n"); // Ping.
                            {
                                string topic;
                                var pass2 = sub2.TryReceiveFrameString(TimeSpan.FromMilliseconds(250), out topic);
                                if (pass2)
                                {
                                    Console.Write(topic);
                                }
                                else
                                {
                                    Assert.Fail();
                                }
                            }
                        }
                        Console.WriteLine("Connected in {0} ms.", swInner.ElapsedMilliseconds);
                    }
                }
            }

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }
    }
}