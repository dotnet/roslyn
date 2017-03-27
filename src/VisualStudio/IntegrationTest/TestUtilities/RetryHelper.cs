// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public static class RetryHelper
    {
        public static void Try(
           Action action,
           int retryCount = 3)
        {
            Try<object>(() =>
            {
                action();
                return null;
            }, retryCount);
        }

        public static T Try<T>(
            Func<T> action,
            int retryCount = 3)
        {
            var exceptions = new List<Exception>();
            for (var retry = 0; retry < retryCount; retry++)
            {
                try
                {
                    if (retry > 0)
                    {
                        System.Threading.Thread.Yield();
                        return action();
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            throw new AggregateException(exceptions);
        }
    }
}
