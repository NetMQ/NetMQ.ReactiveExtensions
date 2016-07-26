using System;
using System.Diagnostics;
using NUnit.Framework;

// ReSharper disable SuggestVarOrType_SimpleTypes

namespace NetMQ.ReactiveExtensions.Tests
{
    public class SerializeAnException_Tests
    {
        [Test]
        public static void Can_Serialize_An_Exception()
        {
            NUnitUtils.PrintTestName();

            Stopwatch sw = Stopwatch.StartNew();

            var ex1 = new Exception("My Inner Exception 2");
            var ex2 = new Exception("My Exception 1", ex1);

            var originalException = new SerializableException(ex2);

            // Save the full ToString() value, including the exception message and stack trace.

            var rawBytes = originalException.SerializeException();
            var newException = rawBytes.DeSerializeException();

            var originalExceptionAsString = originalException.ToString();
            var newExceptionAsString = newException.ToString();

            // Double-check that the exception message and stack trace (owned by the base Exception) are preserved
            Assert.AreEqual(originalExceptionAsString, newExceptionAsString);

            NUnitUtils.PrintElapsedTime(sw.Elapsed);
        }
    }
}