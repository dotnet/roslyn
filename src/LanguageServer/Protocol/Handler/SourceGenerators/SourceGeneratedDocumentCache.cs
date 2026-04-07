// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SourceGenerators;

internal record struct SourceGeneratedDocumentGetTextState(Document Document);

internal sealed class SourceGeneratedDocumentCache(string uniqueKey) : VersionedPullCache<(SourceGeneratorExecutionVersion, VersionStamp), SourceGeneratedDocumentGetTextState, SourceText?>(uniqueKey), ILspService
{
    public override async Task<(SourceGeneratorExecutionVersion, VersionStamp)> ComputeVersionAsync(SourceGeneratedDocumentGetTextState state, CancellationToken cancellationToken)
    {
        // The execution version and the dependent version must be considered as one version cached together -
        // it is not correct to say that if the execution version is the same then we can re-use results (as in automatic mode the execution version never changes).
        var executionVersion = state.Document.Project.Solution.GetSourceGeneratorExecutionVersion(state.Document.Project.Id);
        var dependentVersion = await state.Document.Project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);
        return (executionVersion, dependentVersion);
    }

    public override Checksum ComputeChecksum(SourceText? data, string language)
    {
        return data is null ? Checksum.Null : Checksum.From(data.GetChecksum());
    }

    public override async Task<SourceText?> ComputeDataAsync(SourceGeneratedDocumentGetTextState state, CancellationToken cancellationToken)
    {
        // When a user has a open source-generated file, we ensure that the contents in the LSP snapshot match the contents that we
        // get through didOpen/didChanges, like any other file. That way operations in LSP file are in sync with the
        // contents the user has. However in this case, we don't want to look at that frozen text, but look at what the
        // generator would generate if we ran it again. Otherwise, we'll get "stuck" and never update the file with something new.
        // This can return null when the source generated file has been removed (but the queue itself is using the frozen non-null document).
        var unfrozenDocument = await state.Document.Project.Solution.WithoutFrozenSourceGeneratedDocuments().GetDocumentAsync(state.Document.Id, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
        return unfrozenDocument == null
            ? null
            : await unfrozenDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
    }
}

[ExportCSharpVisualBasicLspServiceFactory(typeof(SourceGeneratedDocumentCache)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SourceGeneratedDocumentCacheFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        return new SourceGeneratedDocumentCache(this.GetType().Name);
    }
}
