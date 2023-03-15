// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// A placeholder singleton analyzer. Its only purpose is to represent generator-produced diagnostics in maps that are keyed by <see cref="DiagnosticAnalyzer"/>.
    /// </summary>
    internal sealed class GeneratorDiagnosticsPlaceholderAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly GeneratorDiagnosticsPlaceholderAnalyzer Instance = new();

        private GeneratorDiagnosticsPlaceholderAnalyzer()
        {
        }

        // We don't have any diagnostics to directly state here, since it could be any underlying type.
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray<DiagnosticDescriptor>.Empty;

#pragma warning disable RS1026 // Enable concurrent execution
#pragma warning disable RS1025 // Configure generated code analysis
        public sealed override void Initialize(AnalysisContext context) { }
#pragma warning restore RS1025 // Configure generated code analysis
#pragma warning restore RS1026 // Enable concurrent execution
    }
}
