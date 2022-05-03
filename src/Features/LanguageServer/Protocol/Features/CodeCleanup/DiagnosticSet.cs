// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    /// <summary>
    /// Indicates which code fixes are enabled for a Code Cleanup operation. Each code fix in the set is triggered by
    /// one or more diagnostic IDs, which could be provided by the compiler or an analyzer.
    /// </summary>
    internal sealed class DiagnosticSet
    {
        public string Description { get; }
        public ImmutableArray<string> DiagnosticIds { get; }

        public DiagnosticSet(string description, params string[] diagnosticIds)
        {
            Description = description;
            DiagnosticIds = ImmutableArray.Create(diagnosticIds);
        }
    }
}
