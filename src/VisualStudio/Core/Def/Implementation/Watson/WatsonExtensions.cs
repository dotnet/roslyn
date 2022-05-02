// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    internal static class WatsonExtensions
    {
        public static void SetCallstackIfEmpty(this Exception exception)
        {
            // There have been cases where a new, unthrown exception has been passed to this method.
            // In these cases the exception won't have a stack trace, which isn't very helpful. We
            // throw and catch the exception here as that will result in a stack trace that is
            // better than nothing.
            if (exception.StackTrace != null)
            {
                return;
            }

            try
            {
                throw exception;
            }
            catch
            {
                // Empty; we just need the exception to have a stack trace.
            }
        }
    }
}
