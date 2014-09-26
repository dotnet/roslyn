// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static partial class ExceptionHelpers
    {
        private const string SuppressFailFastKey = "Roslyn.Utilities.SuppressFailFast";

        private static Exception lastException;
        private static string lastExceptionMessage;

        private static readonly object boxedTrue = true;

        public static FailFastReset SuppressFailFast()
        {
            CallContext.LogicalSetData(SuppressFailFastKey, boxedTrue);
            return new FailFastReset();
        }

        public static bool IsFailFastSuppressed()
        {
            object value = CallContext.LogicalGetData(SuppressFailFastKey);
            return value != null && (bool)value;
        }

        public struct FailFastReset : IDisposable
        {
            public void Dispose()
            {
                CallContext.LogicalSetData(SuppressFailFastKey, null);
            }
        }

        public static bool Crash(Exception e)
        {
            if (!IsFailFastSuppressed())
            {
                // hold onto last exception to make investigation easier
                lastException = e;
                lastExceptionMessage = e.ToString();

                FailFast.OnFatalException(e);
            }

            return false;
        }

        public static bool CrashUnlessCanceled(Exception e)
        {
            if (e is OperationCanceledException ||
                e is ThreadAbortException ||
                IsFailFastSuppressed())
            {
                return false;
            }

            // hold onto last exception to make investigation easier
            lastException = e;
            lastExceptionMessage = e.ToString();

            FailFast.OnFatalException(e);
            return false;
        }
    }
}
