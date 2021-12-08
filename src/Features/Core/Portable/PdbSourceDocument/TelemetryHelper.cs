// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    internal static class TelemetryHelper
    {
        public static void Log(bool timeout, string pdbSource, string? sourceFileSource)
        {
            Logger.Log(FunctionId.NavigateToExternalSources, KeyValueLogMessage.Create(m =>
            {
                m["timeout"] = timeout;
                m["pdb"] = pdbSource;
                m["source"] = sourceFileSource ?? "none";
            }));
        }

        public static IDisposable Start(CancellationToken cancellationToken)
        {
            return Logger.LogBlock(FunctionId.NavigateToExternalSources_Timing, cancellationToken);
        }
    }
}
