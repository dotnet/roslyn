// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    internal static class WatsonExtensions
    {
        // NFW API let caller to customize watson report to make them better bucketed by
        // putting custom string in reserved slots. normally those 2 slots will be empty.
        private const int Reserved1 = 8;
        private const int Reserved2 = 7;

        /// <summary>
        /// This sets extra watson bucket parameters to make bucketting better
        /// in non fatal watson report
        /// </summary>
        public static void SetExtraParameters(this IFaultUtility fault, Exception exception)
        {
            switch (exception)
            {
                case RemoteInvocationException remote:
                    fault.SetBucketParameter(Reserved1, remote.GetParameterString());
                    return;
                case AggregateException aggregate:
                    if (aggregate.InnerException == null)
                    {
                        return;
                    }
                    else if (aggregate.InnerExceptions.Count == 1)
                    {
                        fault.SetBucketParameter(Reserved1, aggregate.GetParameterString());
                        return;
                    }
                    else
                    {
                        var flatten = aggregate.Flatten();

                        fault.SetBucketParameter(Reserved1, flatten.InnerException.GetParameterString());
                        fault.SetBucketParameter(Reserved2, flatten.CalculateHash());

                        return;
                    }
                default:
                    if (exception.InnerException == null)
                    {
                        return;
                    }

                    fault.SetBucketParameter(Reserved1, exception.InnerException.GetParameterString());
                    return;
            }
        }

        public static string CalculateHash(this AggregateException exception)
        {
            var hash = 1;
            foreach (var inner in exception.InnerExceptions)
            {
                var parameterString = inner.GetParameterString();
                hash = Hash.Combine(parameterString, hash);
            }

            return hash.ToString();
        }

        public static string GetParameterString(this Exception exception)
        {
            switch (exception)
            {
                case RemoteInvocationException remote:
                    return $"{remote.RemoteErrorCode} {remote.RemoteStackTrace ?? exception.Message}";
                case AggregateException aggregate when aggregate.InnerException != null:
                    // get first exception that is not aggregated exception
                    return GetParameterString(aggregate.InnerException);
                default:
                    return $"{exception.GetType().ToString()} {(exception.StackTrace ?? exception.ToString())}";
            }
        }
    }
}
