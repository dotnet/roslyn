// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing
{
    /// <summary>
    /// Defines a <see cref="DiagnosticAnalyzer"/> which does not report any diagnostics.
    /// </summary>
#pragma warning disable RS1001 // Missing diagnostic analyzer attribute.
    public sealed class EmptyDiagnosticAnalyzer : DiagnosticAnalyzer
#pragma warning restore RS1001 // Missing diagnostic analyzer attribute.
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray<DiagnosticDescriptor>.Empty;

        public override void Initialize(AnalysisContext context)
        {
        }
    }
}
