﻿using System;

namespace Vostok.ClusterClient.Core.Tests.Helpers
{
    internal static class ExceptionAssertions
    {
        public static void ShouldBePrinted<T>(this T exception)
            where T : Exception
        {
            Console.Out.WriteLine(exception);
        }
    }
}
