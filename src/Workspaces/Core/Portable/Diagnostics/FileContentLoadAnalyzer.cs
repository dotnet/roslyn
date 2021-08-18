// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// A dummy singleton analyzer. Its only purpose is to represent file content load failures in maps that are keyed by <see cref="DiagnosticAnalyzer"/>.
    /// </summary>
    internal sealed class FileContentLoadAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly FileContentLoadAnalyzer Instance = new();

        private FileContentLoadAnalyzer()
        {
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(WorkspaceDiagnosticDescriptors.ErrorReadingFileContent);

#pragma warning disable RS1026 // Enable concurrent execution
#pragma warning disable RS1025 // Configure generated code analysis
        public sealed override void Initialize(AnalysisContext context) { }
#pragma warning restore RS1025 // Configure generated code analysis
#pragma warning restore RS1026 // Enable concurrent execution
    }
}
