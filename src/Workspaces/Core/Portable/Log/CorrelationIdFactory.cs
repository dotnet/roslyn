// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log;

internal static class CorrelationIdFactory
{
    private static int s_globalId;

    public static int GetNextId()
        => Interlocked.Increment(ref s_globalId);
}
