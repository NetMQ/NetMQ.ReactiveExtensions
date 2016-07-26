using System;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace NetMQ.ReactiveExtensions.Tests
{
    [TestFixture]
    public class NetMQ_ReactiveExtensions_Test_Batch_1
    {
        [Test]
        public void Can_Serialize_Class_Name_Longer_Then_Thirty_Two_Characters()
        {
            NUnitUtils.PrintTestName();

            var sw = Stopwatch.StartNew();

            var cd = new CountdownEvent(5);
            {
                var freePort = NUnitUtils.TcpPortFree();

                var pubSub =
                    new SubjectNetMQ<ClassNameIsLongerThenThirtyTwoCharactersForAbsolutelySure>(
                        "tcp://127.0.0.1:" + freePort, loggerDelegate: Console.Write);
                pubSub.Subscribe(o =>
                {
                    Assert.IsTrue(o.Name == "Bob");
                    Console.Write("Test: Num={0}, Name={1}\n", o.Num, o.Name);
                    cd.Signal();
                },
                    ex => { Console.WriteLine("Exception! {0}", ex.Message); });

                pubSub.OnNext(new ClassNameIsLongerThenThirtyTwoCharactersForAbsolutelySure(38, "Bob"));
                pubSub.OnNext(new ClassNameIsLongerThenThirtyTwoCharactersForAbsolutelySure(39, "Bob"));
                pubSub.OnNext(new ClassNameIsLongerThenThirtyTwoCharactersForAbsolutelySure(40, "Bob"));
                pubSub.OnNext(new ClassNameIsLongerThenThirtyTwoCharactersForAbsolutelySure(41, "Bob"));
                pubSub.OnNext(new ClassNameIsLongerThenThirtyTwoCharactersForAbsolutelySure(42, "Bob"));
            }

            if (cd.Wait(GlobalTimeout.Timeout) == false) // Blocks until _countdown.Signal has been called.
            {
                Assert.Fail("Timed out, this test should complete in {0} seconds.", GlobalTimeout.Timeout.TotalSeconds);
            }

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }


        [Test]
        public void Can_Serialize_Using_Protobuf_With_Class()
        {
            NUnitUtils.PrintTestName();

            var sw = Stopwatch.StartNew();

            var cd = new CountdownEvent(5);
            {
                var freePort = NUnitUtils.TcpPortFree();

                var pubSub = new SubjectNetMQ<MyMessageClassType1>("tcp://127.0.0.1:" + freePort,
                    loggerDelegate: Console.Write);
                pubSub.Subscribe(o =>
                {
                    Assert.IsTrue(o.Name == "Bob");
                    Console.Write("Test: Num={0}, Name={1}\n", o.Num, o.Name);
                    cd.Signal();
                },
                    ex => { Console.WriteLine("Exception! {0}", ex.Message); });

                pubSub.OnNext(new MyMessageClassType1(38, "Bob"));
                pubSub.OnNext(new MyMessageClassType1(39, "Bob"));
                pubSub.OnNext(new MyMessageClassType1(40, "Bob"));
                pubSub.OnNext(new MyMessageClassType1(41, "Bob"));
                pubSub.OnNext(new MyMessageClassType1(42, "Bob"));
            }

            if (cd.Wait(GlobalTimeout.Timeout) == false) // Blocks until _countdown.Signal has been called.
            {
                Assert.Fail("Timed out, this test should complete in {0} seconds.", GlobalTimeout.Timeout.TotalSeconds);
            }

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }

        [Test]
        public void Can_Serialize_Using_Protobuf_With_Struct()
        {
            NUnitUtils.PrintTestName();

            var sw = Stopwatch.StartNew();

            var cd = new CountdownEvent(5);
            {
                var freePort = NUnitUtils.TcpPortFree();

                var pubSub = new SubjectNetMQ<MyMessageStructType1>("tcp://127.0.0.1:" + freePort,
                    loggerDelegate: Console.Write);
                pubSub.Subscribe(o =>
                {
                    Assert.IsTrue(o.Name == "Bob");
                    Console.Write("Test: Num={0}, Name={1}\n", o.Num, o.Name);
                    cd.Signal();
                },
                    ex => { Console.WriteLine("Exception! {0}", ex.Message); });

                pubSub.OnNext(new MyMessageStructType1(38, "Bob"));
                pubSub.OnNext(new MyMessageStructType1(39, "Bob"));
                pubSub.OnNext(new MyMessageStructType1(40, "Bob"));
                pubSub.OnNext(new MyMessageStructType1(41, "Bob"));
                pubSub.OnNext(new MyMessageStructType1(42, "Bob"));
            }

            if (cd.Wait(GlobalTimeout.Timeout) == false) // Blocks until _countdown.Signal has been called.
            {
                Assert.Fail("Timed out, this test should complete in {0} seconds.", GlobalTimeout.Timeout.TotalSeconds);
            }

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }

        [Test]
        public static void Disposing_Of_One_Does_Not_Dispose_Of_The_Other()
        {
            NUnitUtils.PrintTestName();
            var sw = Stopwatch.StartNew();

            var max = 1000;
            var cd = new CountdownEvent(max);
            {
                var freePort = NUnitUtils.TcpPortFree();
                var pubSub = new SubjectNetMQ<int>("tcp://127.0.0.1:" + freePort, loggerDelegate: Console.Write);
                var d1 = pubSub.Subscribe(o => { cd.Signal(); });

                var d2 = pubSub.Subscribe(o => { Assert.Fail(); },
                    ex => { Console.WriteLine("Exception in subscriber thread."); });
                d2.Dispose();

                for (var i = 0; i < max; i++)
                {
                    pubSub.OnNext(i);
                }
            }

            if (cd.Wait(GlobalTimeout.Timeout) == false) // Blocks until _countdown.Signal has been called.
            {
                Assert.Fail("Timed out, this test should complete in {0} seconds.", GlobalTimeout.Timeout.TotalSeconds);
            }

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }


        [Test]
        public void Initialize_Publisher_Then_Subscriber()
        {
            NUnitUtils.PrintTestName();

            var sw = Stopwatch.StartNew();

            var cd = new CountdownEvent(5);
            {
                var freePort = NUnitUtils.TcpPortFree();

                var pubSub = new SubjectNetMQ<int>("tcp://127.0.0.1:" + freePort, loggerDelegate: Console.Write);

                // Forces the publisher to be initialized. Subscriber not set up yet, so this message will never get
                // delivered to the subscriber, which is what is should do.
                pubSub.OnNext(1);

                pubSub.Subscribe(o =>
                {
                    Assert.IsTrue(o != 1);
                    Console.Write("Test 1: {0}\n", o);
                    cd.Signal();
                },
                    ex => { Console.WriteLine("Exception! {0}", ex.Message); });

                pubSub.OnNext(38);
                pubSub.OnNext(39);
                pubSub.OnNext(40);
                pubSub.OnNext(41);
                pubSub.OnNext(42);
            }

            if (cd.Wait(GlobalTimeout.Timeout) == false) // Blocks until _countdown.Signal has been called.
            {
                Assert.Fail("Timed out, this test should complete in {0} seconds.", GlobalTimeout.Timeout.TotalSeconds);
            }

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }

        [Test]
        public void OnCompleted_Should_Get_Passed_To_Subscribers()
        {
            NUnitUtils.PrintTestName();

            var sw = Stopwatch.StartNew();

            var weAreDone = new CountdownEvent(1);
            {
                var freePort = NUnitUtils.TcpPortFree();
                var pubSub = new SubjectNetMQ<int>("tcp://127.0.0.1:" + freePort, loggerDelegate: Console.Write);
                pubSub.Subscribe(
                    o =>
                    {
                        // If this gets called more than max times, it will throw an exception as it is going through 0.
                        //Console.Write("FAIL!");
                        //Assert.Fail();
                    },
                    ex =>
                    {
                        Console.Write("FAIL!");
                        Assert.Fail();
                    },
                    () =>
                    {
                        Console.Write("Pass!");
                        weAreDone.Signal();
                    });

                pubSub.OnNext(42);
                pubSub.OnCompleted();
            }

            if (weAreDone.Wait(GlobalTimeout.Timeout) == false) // Blocks until _countdown.Signal has been called.
            {
                Assert.Fail("Timed out, this test should complete in {0} seconds.", GlobalTimeout.Timeout.TotalSeconds);
            }

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }

        [Test]
        public void OnException_Should_Get_Passed_To_Subscribers()
        {
            NUnitUtils.PrintTestName();

            var sw = Stopwatch.StartNew();

            var weAreDone = new CountdownEvent(1);
            {
                var freePort = NUnitUtils.TcpPortFree();
                var pubSub = new SubjectNetMQ<int>("tcp://127.0.0.1:" + freePort, loggerDelegate: Console.Write);
                pubSub.Subscribe(
                    o =>
                    {
                        // If this gets called more than max times, it will throw an exception as it is going through 0.
                        Assert.Fail();
                    },
                    ex =>
                    {
                        Console.Write("Exception: {0}", ex.Message);
                        Assert.True(ex.Message.Contains("passed"));
                        weAreDone.Signal();
                    },
                    () => { Assert.Fail(); });

                pubSub.OnError(new Exception("passed"));
            }

            if (weAreDone.Wait(GlobalTimeout.Timeout) == false) // Blocks until _countdown.Signal has been called.
            {
                Assert.Fail("Timed out, this test should complete in {0} seconds.", GlobalTimeout.Timeout.TotalSeconds);
            }

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }

        [Test]
        public void Simplest_Fanout_Sub()
        {
            NUnitUtils.PrintTestName();

            var sw = Stopwatch.StartNew();

            var cd = new CountdownEvent(3);
            {
                var freePort = NUnitUtils.TcpPortFree();
                var pubSub = new SubjectNetMQ<int>("tcp://127.0.0.1:" + freePort, loggerDelegate: Console.Write);
                pubSub.Subscribe(o =>
                {
                    Assert.AreEqual(o, 42);
                    Console.Write("PubTwoThreadFanoutSub1: {0}\n", o);
                    cd.Signal();
                });
                pubSub.Subscribe(o =>
                {
                    Assert.AreEqual(o, 42);
                    Console.Write("PubTwoThreadFanoutSub2: {0}\n", o);
                    cd.Signal();
                });
                pubSub.Subscribe(o =>
                {
                    Assert.AreEqual(o, 42);
                    Console.Write("PubTwoThreadFanoutSub3: {0}\n", o);
                    cd.Signal();
                });

                pubSub.OnNext(42);
            }

            if (cd.Wait(GlobalTimeout.Timeout) == false) // Blocks until _countdown.Signal has been called.
            {
                Assert.Fail("Timed out, this test should complete in {0} seconds.", GlobalTimeout.Timeout.TotalSeconds);
            }

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }

        [Test]
        public void Simplest_Test_Publisher_To_Subscriber()
        {
            NUnitUtils.PrintTestName();

            var sw = Stopwatch.StartNew();

            var cd = new CountdownEvent(5);
            {
                var freePort = NUnitUtils.TcpPortFree();

                var publisher = new PublisherNetMq<int>("tcp://127.0.0.1:" + freePort, loggerDelegate: Console.Write);
                var subscriber = new SubscriberNetMq<int>("tcp://127.0.0.1:" + freePort, loggerDelegate: Console.Write);

                subscriber.Subscribe(o =>
                {
                    Console.Write("Test 1: {0}\n", o);
                    cd.Signal();
                },
                    ex => { Console.WriteLine("Exception! {0}", ex.Message); });

                publisher.OnNext(38);
                publisher.OnNext(39);
                publisher.OnNext(40);
                publisher.OnNext(41);
                publisher.OnNext(42);
            }

            if (cd.Wait(GlobalTimeout.Timeout) == false) // Blocks until _countdown.Signal has been called.
            {
                Assert.Fail("Timed out, this test should complete in {0} seconds.", GlobalTimeout.Timeout.TotalSeconds);
            }

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }

        [Test]
        public void Simplest_Test_Subject()
        {
            NUnitUtils.PrintTestName();

            var sw = Stopwatch.StartNew();

            var cd = new CountdownEvent(5);
            {
                var freePort = NUnitUtils.TcpPortFree();

                var pubSub = new SubjectNetMQ<int>("tcp://127.0.0.1:" + freePort, loggerDelegate: Console.Write);
                pubSub.Subscribe(o =>
                {
                    Console.Write("Test 1: {0}\n", o);
                    cd.Signal();
                },
                    ex => { Console.WriteLine("Exception! {0}", ex.Message); });

                pubSub.OnNext(38);
                pubSub.OnNext(39);
                pubSub.OnNext(40);
                pubSub.OnNext(41);
                pubSub.OnNext(42);
            }

            if (cd.Wait(GlobalTimeout.Timeout) == false) // Blocks until _countdown.Signal has been called.
            {
                Assert.Fail("Timed out, this test should complete in {0} seconds.", GlobalTimeout.Timeout.TotalSeconds);
            }

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }
    }
}