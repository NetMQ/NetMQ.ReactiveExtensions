using System;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;

namespace NetMQ.ReactiveExtensions.Tests
{
    public static class TestUtils
    {
        /// <summary>
        ///     Intent: Returns next free TCP/IP port. See
        ///     http://stackoverflow.com/questions/138043/find-the-next-tcp-port-in-net
        /// </summary>
        /// <threadSafe>
        ///     Yes. Quote: "I successfully used this technique to get a free port. I too was concerned about
        ///     race-conditions, with some other process sneaking in and grabbing the recently-detected-as-free port. So I
        ///     wrote a test with a forced Sleep(100) between var port = FreeTcpPort() and starting an HttpListener on the
        ///     free port. I then ran 8 identical processes hammering on this in a loop. I could never hit the race
        ///     condition. My anecdotal evidence (Win 7) is that the OS apparently cycles through the range of ephemeral
        ///     ports (a few thousand) before coming around again. So the above snippet should be just fine."
        /// </threadSafe>
        /// <returns>A free TCP/IP port.</returns>
        public static int TcpPortFree()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint) l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        public static void PrintTestName()
        {
            Console.Write("\n\n*****\nTest: {0}\n*****\n", TestContext.CurrentContext.Test.Name);
        }

        public static void PrintElapsedTime(TimeSpan sw, int? max = null)
        {
            Console.Write("\nElapsed time: {0} milliseconds", sw.TotalMilliseconds);
            if (max != null)
            {
                Console.Write(" ({0:0,000}/sec)", (double) max/sw.TotalSeconds);
            }
            Console.Write("\n");
        }
    }
}