// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UnitTests;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

[DiagnosticAnalyzer(NoCompilationConstants.LanguageName)]
internal sealed class NoCompilationDocumentDiagnosticAnalyzer : DocumentDiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Descriptor = new(
        "NC0000", "No Compilation Syntax Error", "No Compilation Syntax Error", "Error", DiagnosticSeverity.Error, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptor];

    public override Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(TextDocument document, SyntaxTree tree, CancellationToken cancellationToken)
    {
        return Task.FromResult(ImmutableArray.Create(
            Diagnostic.Create(Descriptor, Location.Create(document.FilePath, default, default))));
    }
}
