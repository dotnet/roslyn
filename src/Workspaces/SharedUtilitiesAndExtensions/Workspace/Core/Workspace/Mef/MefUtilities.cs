// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host.Mef
{
    internal static class MefUtilities
    {
        public static void DisposeWithExceptionTracking<T>(T instance, [NotNullIfNotNull("exceptions")] ref List<Exception>? exceptions)
            where T : IDisposable
        {
            try
            {
                instance.Dispose();
            }
            catch (Exception ex) when (FatalError.ReportWithoutCrashAndPropagate(ex))
            {
                throw ExceptionUtilities.Unreachable;
            }
            catch (Exception ex)
            {
                exceptions ??= new List<Exception>();
                exceptions.Add(ex);
            }
        }
    }
}
