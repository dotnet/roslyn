// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        public DiagnosticSet(string description, string[] diagnosticIds)
        {
            Description = description;
            DiagnosticIds = ImmutableArray.Create(diagnosticIds);
        }
    }
}
