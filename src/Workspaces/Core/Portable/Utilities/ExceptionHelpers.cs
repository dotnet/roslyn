// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Utilities
{
    internal static partial class ExceptionHelpers
    {
        public static FailFastReset SuppressFailFast()
        {
            // TODO: re-implement this mechanism in a portable way
            // CallContext.LogicalSetData(SuppressFailFastKey, boxedTrue);
            return new FailFastReset();
        }

        public static bool IsFailFastSuppressed()
        {
            return false;
        }

        public struct FailFastReset : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
