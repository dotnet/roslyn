// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// A dummy singleton analyzer. Its only purpose is to represent file content load failures in maps that are keyed by <see cref="DiagnosticAnalyzer"/>.
    /// </summary>
    internal sealed class FileContentLoadAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly FileContentLoadAnalyzer Instance = new FileContentLoadAnalyzer();

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
