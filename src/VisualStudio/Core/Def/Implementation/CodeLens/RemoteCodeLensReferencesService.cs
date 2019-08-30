// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CodeLens
{
    [ExportWorkspaceService(typeof(ICodeLensReferencesService), layer: ServiceLayer.Host), Shared]
    internal sealed class RemoteCodeLensReferencesService : ICodeLensReferencesService
    {
        [ImportingConstructor]
        public RemoteCodeLensReferencesService()
        {
        }

        public async Task<ReferenceCount> GetReferenceCountAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, int maxSearchResults,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeLens_GetReferenceCountAsync, cancellationToken))
            {
                if (syntaxNode == null)
                {
                    return null;
                }

                var remoteHostClient = await solution.Workspace.Services.GetService<IRemoteHostClientService>().TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
                if (remoteHostClient == null)
                {
                    // remote host is not running. this can happen if remote host is disabled.
                    return await CodeLensReferencesServiceFactory.Instance.GetReferenceCountAsync(solution, documentId, syntaxNode, maxSearchResults, cancellationToken).ConfigureAwait(false);
                }

                return await remoteHostClient.TryRunCodeAnalysisRemoteAsync<ReferenceCount>(
                    solution, nameof(IRemoteCodeLensReferencesService.GetReferenceCountAsync),
                    new object[] { documentId, syntaxNode.Span, maxSearchResults }, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<ReferenceLocationDescriptor>> FindReferenceLocationsAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeLens_FindReferenceLocationsAsync, cancellationToken))
            {
                var descriptors = await FindReferenceLocationsWorkerAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
                if (descriptors == null)
                {
                    return null;
                }

                // map spans to right locations using SpanMapper for documents such as cshtml and etc
                return await FixUpDescriptors(solution, descriptors, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<ReferenceMethodDescriptor>> FindReferenceMethodsAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeLens_FindReferenceMethodsAsync, cancellationToken))
            {
                if (syntaxNode == null)
                {
                    return null;
                }

                var remoteHostClient = await solution.Workspace.Services.GetService<IRemoteHostClientService>().TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
                if (remoteHostClient == null)
                {
                    // remote host is not running. this can happen if remote host is disabled.
                    return await CodeLensReferencesServiceFactory.Instance.FindReferenceMethodsAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
                }

                return await remoteHostClient.TryRunCodeAnalysisRemoteAsync<IEnumerable<ReferenceMethodDescriptor>>(
                    solution, nameof(IRemoteCodeLensReferencesService.FindReferenceMethodsAsync),
                    new object[] { documentId, syntaxNode.Span }, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<string> GetFullyQualifiedName(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CodeLens_GetFullyQualifiedName, cancellationToken))
            {
                if (syntaxNode == null)
                {
                    return null;
                }

                var remoteHostClient = await solution.Workspace.Services.GetService<IRemoteHostClientService>().TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
                if (remoteHostClient == null)
                {
                    // remote host is not running. this can happen if remote host is disabled.
                    return await CodeLensReferencesServiceFactory.Instance.GetFullyQualifiedName(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
                }

                return await remoteHostClient.TryRunCodeAnalysisRemoteAsync<string>(
                    solution, nameof(IRemoteCodeLensReferencesService.GetFullyQualifiedName),
                    new object[] { documentId, syntaxNode.Span }, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<IEnumerable<ReferenceLocationDescriptor>> FixUpDescriptors(
            Solution solution, IEnumerable<ReferenceLocationDescriptor> descriptors, CancellationToken cancellationToken)
        {
            var list = new List<ReferenceLocationDescriptor>();
            foreach (var descriptor in descriptors)
            {
                var referencedDocumentId = DocumentId.CreateFromSerialized(
                    ProjectId.CreateFromSerialized(descriptor.ProjectGuid), descriptor.DocumentGuid);

                var document = solution.GetDocument(referencedDocumentId);

                var spanMapper = document?.Services.GetService<ISpanMappingService>();
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
                var referenceExcerpt = await excerpter.TryExcerptAsync(document, span, ExcerptMode.SingleLine, cancellationToken).ConfigureAwait(false);
                var tooltipExcerpt = await excerpter.TryExcerptAsync(document, span, ExcerptMode.Tooltip, cancellationToken).ConfigureAwait(false);

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

            return list;
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

        private async Task<IEnumerable<ReferenceLocationDescriptor>> FindReferenceLocationsWorkerAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
            CancellationToken cancellationToken)
        {
            if (syntaxNode == null)
            {
                return null;
            }

            var remoteHostClient = await solution.Workspace.Services.GetService<IRemoteHostClientService>().TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (remoteHostClient == null)
            {
                // remote host is not running. this can happen if remote host is disabled.
                return await CodeLensReferencesServiceFactory.Instance.FindReferenceLocationsAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
            }

            return await remoteHostClient.TryRunCodeAnalysisRemoteAsync<IEnumerable<ReferenceLocationDescriptor>>(
                solution, nameof(IRemoteCodeLensReferencesService.FindReferenceLocationsAsync),
                new object[] { documentId, syntaxNode.Span }, cancellationToken).ConfigureAwait(false);
        }
    }
}
