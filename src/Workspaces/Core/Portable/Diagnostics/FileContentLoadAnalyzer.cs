// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// A dummy singleton analyzer. Its only purpose is to represent file content load failures in maps that are keyed by
/// <see cref="DiagnosticAnalyzer"/>.
/// </summary>
internal sealed class FileContentLoadAnalyzer : DocumentDiagnosticAnalyzer
{
    public static readonly FileContentLoadAnalyzer Instance = new();

    private FileContentLoadAnalyzer()
    {
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [WorkspaceDiagnosticDescriptors.ErrorReadingFileContent];

    public override int Priority => -4;

    public override async Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(
        TextDocument textDocument, SyntaxTree? tree, CancellationToken cancellationToken)
    {
        var exceptionMessage = await textDocument.State.GetFailedToLoadExceptionMessageAsync(cancellationToken).ConfigureAwait(false);
        if (exceptionMessage is null)
            return [];

        var location = tree is null
            ? textDocument.FilePath is null ? Location.None : Location.Create(textDocument.FilePath, textSpan: default, lineSpan: default)
            : tree.GetLocation(span: default);

        var filePath = textDocument.FilePath;
        var display = filePath ?? "<no path>";

        return [Diagnostic.Create(
            WorkspaceDiagnosticDescriptors.ErrorReadingFileContent, location, display, exceptionMessage)];
    }
}
