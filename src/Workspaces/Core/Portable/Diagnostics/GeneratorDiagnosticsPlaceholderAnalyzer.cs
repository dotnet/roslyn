// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// A placeholder singleton analyzer. Its only purpose is to represent generator-produced diagnostics in maps that are keyed by <see cref="DiagnosticAnalyzer"/>.
/// </summary>
internal sealed class GeneratorDiagnosticsPlaceholderAnalyzer : DocumentDiagnosticAnalyzer
{
    public static readonly GeneratorDiagnosticsPlaceholderAnalyzer Instance = new();

    private GeneratorDiagnosticsPlaceholderAnalyzer()
    {
    }

    // We don't have any diagnostics to directly state here, since it could be any underlying type.
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [];

    public override int Priority => -3;

    public override async Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(TextDocument textDocument, SyntaxTree? tree, CancellationToken cancellationToken)
    {
        var project = textDocument.Project;

        var diagnostics = await Extensions.GetSourceGeneratorDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
        using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var result);

        foreach (var diagnostic in diagnostics)
        {
            if (Extensions.IsReportedInDocument(diagnostic, textDocument))
            {
                // Diagnostic reported directly against this document.  Include in the result set.
                result.Add(diagnostic);
            }
            else if (diagnostic.Location.Kind == LocationKind.None &&
                textDocument.Id == project.DocumentIds[0])
            {
                // Diagnostic reported against no location.  Include in the result set for the first document of the project.
                result.Add(diagnostic);
            }
        }

        return result.ToImmutableAndClear();
    }
}
