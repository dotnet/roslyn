// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Security;
using System.Threading.Tasks;

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

        public static T PerformIO<T>(Func<T> function, T defaultValue = default)
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

        public static async Task<T> PerformIOAsync<T>(Func<Task<T>> function, T defaultValue = default)
        {
            try
            {
                return await function().ConfigureAwait(false);
            }
            catch (Exception e) when (IsNormalIOException(e))
            {
            }

            return defaultValue;
        }

        public static bool IsNormalIOException(Exception e)
        {
            return e is IOException or
                   SecurityException or
                   ArgumentException or
                   UnauthorizedAccessException or
                   NotSupportedException or
                   InvalidOperationException or
                   InvalidDataException;
        }
    }
}
