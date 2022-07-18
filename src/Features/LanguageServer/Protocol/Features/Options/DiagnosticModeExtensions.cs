// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class DiagnosticModeExtensions
    {
        /// <summary>
        /// Gets all the diagnostics for this event, respecting the callers setting on if they're getting it for pull
        /// diagnostics or push diagnostics.  Most clients should use this to ensure they see the proper set of
        /// diagnostics in their scenario (or an empty array if not in their scenario).
        /// </summary>
        public static ImmutableArray<DiagnosticData> GetPullDiagnostics(
            this DiagnosticsUpdatedArgs args, IGlobalOptionService globalOptions)
        {
            return args.GetAllDiagnosticsRegardlessOfPushPullSetting();
        }
    }
}
