// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CodeLens;

[ExportWorkspaceService(typeof(ICodeLensReferencesService), layer: ServiceLayer.Host), Shared]
internal sealed class RemoteCodeLensReferencesService : ICodeLensReferencesService
{
    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public RemoteCodeLensReferencesService(IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public ValueTask<VersionStamp> GetProjectCodeLensVersionAsync(Solution solution, ProjectId projectId, CancellationToken cancellationToken)
    {
        // This value is more efficient to calculate in the current process
        return CodeLensReferencesServiceFactory.Instance.GetProjectCodeLensVersionAsync(solution, projectId, cancellationToken);
    }

    public async Task<ReferenceCount?> GetReferenceCountAsync(Solution solution, DocumentId documentId, SyntaxNode? syntaxNode, int maxSearchResults,
        CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.CodeLens_GetReferenceCountAsync, cancellationToken))
        {
            if (syntaxNode == null)
            {
                return null;
            }

            var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var result = await client.TryInvokeAsync<IRemoteCodeLensReferencesService, ReferenceCount?>(
                    solution,
                    (service, solutionInfo, cancellationToken) => service.GetReferenceCountAsync(solutionInfo, documentId, syntaxNode.Span, maxSearchResults, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                return result.HasValue ? result.Value : null;
            }

            return await CodeLensReferencesServiceFactory.Instance.GetReferenceCountAsync(solution, documentId, syntaxNode, maxSearchResults, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<ImmutableArray<ReferenceLocationDescriptor>?> FindReferenceLocationsAsync(Solution solution, DocumentId documentId, SyntaxNode? syntaxNode,
        CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.CodeLens_FindReferenceLocationsAsync, cancellationToken))
        {
            if (syntaxNode == null)
            {
                return null;
            }

            var descriptors = await FindReferenceLocationsWorkerAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
            if (!descriptors.HasValue)
            {
                return null;
            }

            // map spans to right locations using SpanMapper for documents such as cshtml and etc
            return await FixUpDescriptorsAsync(solution, descriptors.Value, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<ImmutableArray<ReferenceMethodDescriptor>?> FindReferenceMethodsAsync(Solution solution, DocumentId documentId, SyntaxNode? syntaxNode,
        CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.CodeLens_FindReferenceMethodsAsync, cancellationToken))
        {
            if (syntaxNode == null)
            {
                return null;
            }

            var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var result = await client.TryInvokeAsync<IRemoteCodeLensReferencesService, ImmutableArray<ReferenceMethodDescriptor>?>(
                    solution,
                    (service, solutionInfo, cancellationToken) => service.FindReferenceMethodsAsync(solutionInfo, documentId, syntaxNode.Span, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                return result.HasValue ? result.Value : null;
            }

            return await CodeLensReferencesServiceFactory.Instance.FindReferenceMethodsAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<string?> GetFullyQualifiedNameAsync(Solution solution, DocumentId documentId, SyntaxNode? syntaxNode,
        CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.CodeLens_GetFullyQualifiedName, cancellationToken))
        {
            if (syntaxNode == null)
            {
                return null;
            }

            var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var result = await client.TryInvokeAsync<IRemoteCodeLensReferencesService, string>(
                    solution,
                    (service, solutionInfo, cancellationToken) => service.GetFullyQualifiedNameAsync(solutionInfo, documentId, syntaxNode.Span, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                return result.HasValue ? result.Value : null;
            }

            return await CodeLensReferencesServiceFactory.Instance.GetFullyQualifiedNameAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<ImmutableArray<ReferenceLocationDescriptor>> FixUpDescriptorsAsync(
        Solution solution, ImmutableArray<ReferenceLocationDescriptor> descriptors, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<ReferenceLocationDescriptor>.GetInstance(out var list);
        foreach (var descriptor in descriptors)
        {
            var referencedDocumentId = DocumentId.CreateFromSerialized(
                ProjectId.CreateFromSerialized(descriptor.ProjectGuid), descriptor.DocumentGuid);

            var document = await solution.GetDocumentAsync(referencedDocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            if (document == null)
            {
                continue;
            }

            var spanMapper = document.Services.GetService<ISpanMappingService>();
            if (spanMapper == null)
            {
                // for normal document, just add one as they are
                list.Add(descriptor);
                continue;
            }

            var span = new TextSpan(descriptor.SpanStart, descriptor.SpanLength);
            var results = await spanMapper.MapSpansAsync(document, SpecializedCollections.SingletonEnumerable(span), cancellationToken).ConfigureAwait(false);

            // external component violated contracts. the mapper should preserve input order/count. 
            // since we gave in 1 span, it should return 1 span back
            Contract.ThrowIfTrue(results.IsDefaultOrEmpty);

            var result = results[0];
            if (result.IsDefault)
            {
                // it is allowed for mapper to return default 
                // if it can't map the given span to any usable span
                continue;
            }

            var excerpter = document.Services.GetService<IDocumentExcerptService>();
            if (excerpter == null)
            {
                continue;
            }

            var classificationOptions = _globalOptions.GetClassificationOptions(document.Project.Language);
            var referenceExcerpt = await excerpter.TryExcerptAsync(document, span, ExcerptMode.SingleLine, classificationOptions, cancellationToken).ConfigureAwait(false);
            var tooltipExcerpt = await excerpter.TryExcerptAsync(document, span, ExcerptMode.Tooltip, classificationOptions, cancellationToken).ConfigureAwait(false);

            var (text, start, length) = GetReferenceInfo(referenceExcerpt, descriptor);
            var (before1, before2, after1, after2) = GetReferenceTexts(referenceExcerpt, tooltipExcerpt, descriptor);

            list.Add(new ReferenceLocationDescriptor(
                descriptor.LongDescription,
                descriptor.Language,
                descriptor.Glyph,
                result.Span.Start,
                result.Span.Length,
                result.LinePositionSpan.Start.Line,
                result.LinePositionSpan.Start.Character,
                descriptor.ProjectGuid,
                descriptor.DocumentGuid,
                result.FilePath,
                text,
                start,
                length,
                before1,
                before2,
                after1,
                after2));
        }

        return list.ToImmutable();
    }

    private static (string text, int start, int length) GetReferenceInfo(ExcerptResult? reference, ReferenceLocationDescriptor descriptor)
    {
        if (reference.HasValue)
        {
            return (reference.Value.Content.ToString().TrimEnd(),
                    reference.Value.MappedSpan.Start,
                    reference.Value.MappedSpan.Length);
        }

        return (descriptor.ReferenceLineText, descriptor.ReferenceStart, descriptor.ReferenceLength);
    }

    private static (string before1, string before2, string after1, string after2) GetReferenceTexts(ExcerptResult? reference, ExcerptResult? tooltip, ReferenceLocationDescriptor descriptor)
    {
        if (reference == null || tooltip == null)
        {
            return (descriptor.BeforeReferenceText1, descriptor.BeforeReferenceText2, descriptor.AfterReferenceText1, descriptor.AfterReferenceText2);
        }

        var lines = tooltip.Value.Content.Lines;
        var mappedLine = lines.GetLineFromPosition(tooltip.Value.MappedSpan.Start);
        var index = mappedLine.LineNumber;
        if (index < 0)
        {
            return (descriptor.BeforeReferenceText1, descriptor.BeforeReferenceText2, descriptor.AfterReferenceText1, descriptor.AfterReferenceText2);
        }

        return (GetLineTextOrEmpty(lines, index - 1), GetLineTextOrEmpty(lines, index - 2),
                GetLineTextOrEmpty(lines, index + 1), GetLineTextOrEmpty(lines, index + 2));
    }

    private static string GetLineTextOrEmpty(TextLineCollection lines, int index)
    {
        if (index < 0 || index >= lines.Count)
        {
            return string.Empty;
        }

        return lines[index].ToString().TrimEnd();
    }

    private static async Task<ImmutableArray<ReferenceLocationDescriptor>?> FindReferenceLocationsWorkerAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
        CancellationToken cancellationToken)
    {
        if (syntaxNode == null)
        {
            return ImmutableArray<ReferenceLocationDescriptor>.Empty;
        }

        var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var result = await client.TryInvokeAsync<IRemoteCodeLensReferencesService, ImmutableArray<ReferenceLocationDescriptor>?>(
                solution,
                (service, solutionInfo, cancellationToken) => service.FindReferenceLocationsAsync(solutionInfo, documentId, syntaxNode.Span, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : null;
        }

        // remote host is not running. this can happen if remote host is disabled.
        return await CodeLensReferencesServiceFactory.Instance.FindReferenceLocationsAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
    }
}
