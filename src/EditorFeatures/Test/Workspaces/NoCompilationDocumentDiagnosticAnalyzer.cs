// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    [DiagnosticAnalyzer(NoCompilationConstants.LanguageName)]
    internal class NoCompilationDocumentDiagnosticAnalyzer : DocumentDiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
            "NC0000", "No Compilation Syntax Error", "No Compilation Syntax Error", "Error", DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

        public override Task AnalyzeSemanticsAsync(Document document, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public override Task AnalyzeSyntaxAsync(Document document, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
        {
            addDiagnostic(Diagnostic.Create(Descriptor,
                Location.Create(document.FilePath, default(TextSpan), default(LinePositionSpan))));
            return Task.FromResult(true);
        }
    }
}
