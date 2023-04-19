// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal interface IRemoteSymbolFinderService
    {
        internal interface ICallback
        {
            ValueTask AddReferenceItemsAsync(RemoteServiceCallbackId callbackId, int count, CancellationToken cancellationToken);
            ValueTask ReferenceItemCompletedAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
            ValueTask OnStartedAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
            ValueTask OnCompletedAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
            ValueTask OnFindInDocumentStartedAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken);
            ValueTask OnFindInDocumentCompletedAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken);
            ValueTask OnDefinitionFoundAsync(RemoteServiceCallbackId callbackId, SerializableSymbolGroup group, CancellationToken cancellationToken);
            ValueTask OnReferenceFoundAsync(RemoteServiceCallbackId callbackId, SerializableSymbolGroup group, SerializableSymbolAndProjectId definition, SerializableReferenceLocation reference, CancellationToken cancellationToken);

            ValueTask AddLiteralItemsAsync(RemoteServiceCallbackId callbackId, int count, CancellationToken cancellationToken);
            ValueTask LiteralItemCompletedAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
            ValueTask OnLiteralReferenceFoundAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, TextSpan span, CancellationToken cancellationToken);
        }

        ValueTask FindReferencesAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, SerializableSymbolAndProjectId symbolAndProjectIdArg, ImmutableArray<DocumentId> documentArgs,
            FindReferencesSearchOptions options, CancellationToken cancellationToken);

        ValueTask FindLiteralReferencesAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, object value, TypeCode typeCode, CancellationToken cancellationToken);

        ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindAllDeclarationsWithNormalQueryAsync(
            PinnedSolutionInfo solutionInfo, ProjectId projectId, string name, SearchKind searchKind, SymbolFilter criteria, CancellationToken cancellationToken);

        ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithNormalQueryAsync(
            PinnedSolutionInfo solutionInfo, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken);

        ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithNormalQueryAsync(
            PinnedSolutionInfo solutionInfo, ProjectId projectId, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken);

        ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithPatternAsync(
            PinnedSolutionInfo solutionInfo, string pattern, SymbolFilter criteria, CancellationToken cancellationToken);

        ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithPatternAsync(
            PinnedSolutionInfo solutionInfo, ProjectId projectId, string pattern, SymbolFilter criteria, CancellationToken cancellationToken);
    }
}
