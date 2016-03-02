// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal static class IOUtilities
    {
        public static void PerformIO(Action action)
        {
            PerformIO<object>(() =>
            {
                action();
                return null;
            });
        }

        public static T PerformIO<T>(Func<T> function, T defaultValue = default(T))
        {
            try
            {
                return function();
            }
            catch (Exception e) when (IsNormalIOException(e))
            {
            }

            return defaultValue;
        }

        public static bool IsNormalIOException(Exception e)
        {
            return e is IOException ||
                   e is SecurityException ||
                   e is ArgumentException ||
                   e is UnauthorizedAccessException ||
                   e is NotSupportedException ||
                   e is InvalidOperationException;
        }
    }
}