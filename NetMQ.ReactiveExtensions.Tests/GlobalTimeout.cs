using System;

namespace NetMQ.ReactiveExtensions.Tests
{
    public static class GlobalTimeout
    {
        static GlobalTimeout()
        {
            Timeout = TimeSpan.FromSeconds(120);
        }

        public static TimeSpan Timeout { get; set; }
    }
}