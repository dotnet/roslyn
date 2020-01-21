// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    [DiagnosticAnalyzer(NoCompilationConstants.LanguageName)]
    internal class NoCompilationDocumentDiagnosticAnalyzer : DocumentDiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            "NC0000", "No Compilation Syntax Error", "No Compilation Syntax Error", "Error", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

        public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<Diagnostic>();
        }

        public override Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
        {
            return Task.FromResult(ImmutableArray.Create(
                Diagnostic.Create(Descriptor, Location.Create(document.FilePath, default, default))));
        }
    }
}
