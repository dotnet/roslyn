// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

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

    public override async Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
    {
        var loadDiagnostic = await document.State.GetLoadDiagnosticAsync(cancellationToken).ConfigureAwait(false);
        return loadDiagnostic != null ? [loadDiagnostic] : [];
    }

    public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
        => SpecializedTasks.EmptyImmutableArray<Diagnostic>();
}
