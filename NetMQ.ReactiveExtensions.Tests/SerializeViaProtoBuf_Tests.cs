using System.Diagnostics;
using NUnit.Framework;
// ReSharper disable InconsistentNaming

namespace NetMQ.ReactiveExtensions.Tests
{
    [TestFixture]
    public static class SerializeViaProtoBuf_Tests
    {
        [Test]
        public static void Should_be_able_to_serialize_a_decimal()
        {
            NUnitUtils.PrintTestName();

            Stopwatch sw = Stopwatch.StartNew();

            const decimal x = 123.4m;
            var rawBytes = x.SerializeProtoBuf();
            var original = rawBytes.DeserializeProtoBuf<decimal>();
            Assert.AreEqual(x, original);

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }

        [Test]
        public static void Should_be_able_to_serialize_a_string()
        {
            NUnitUtils.PrintTestName();

            Stopwatch sw = Stopwatch.StartNew();

            const string x = "Hello";
            var rawBytes = x.SerializeProtoBuf();
            var original = rawBytes.DeserializeProtoBuf<string>();
            Assert.AreEqual(x, original);

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }

        [Test]
        public static void Should_be_able_to_serialize_an_int()
        {
            NUnitUtils.PrintTestName();

            Stopwatch sw = Stopwatch.StartNew();

            const int x = 42;
            var rawBytes = x.SerializeProtoBuf();
            var original = rawBytes.DeserializeProtoBuf<int>();
            Assert.AreEqual(x, original);

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }
    }
}