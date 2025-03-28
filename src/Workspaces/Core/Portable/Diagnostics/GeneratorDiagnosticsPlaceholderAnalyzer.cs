// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

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
        return diagnostics.WhereAsArray(Extensions.IsReportedInDocument, textDocument);
    }
}
