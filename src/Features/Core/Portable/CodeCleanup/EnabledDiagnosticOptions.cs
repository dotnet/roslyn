// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    /// <summary>
    /// Indicates which features are enabled for a code cleanup operation.
    /// </summary>
    internal sealed class EnabledDiagnosticOptions
    {
        public ImmutableArray<DiagnosticSet> Diagnostics { get; }

        public OrganizeUsingsSet OrganizeUsings { get; }

        public EnabledDiagnosticOptions(ImmutableArray<DiagnosticSet> diagnostics, OrganizeUsingsSet organizeUsings)
        {
            Diagnostics = diagnostics;
            OrganizeUsings = organizeUsings;
        }
    }
}
