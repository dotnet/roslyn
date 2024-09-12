// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal interface IRemoteSymbolFinderService
{
    internal interface ICallback
    {
        ValueTask AddReferenceItemsAsync(RemoteServiceCallbackId callbackId, int count, CancellationToken cancellationToken);
        ValueTask ReferenceItemsCompletedAsync(RemoteServiceCallbackId callbackId, int count, CancellationToken cancellationToken);
        ValueTask OnStartedAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
        ValueTask OnCompletedAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
        ValueTask OnFindInDocumentStartedAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken);
        ValueTask OnFindInDocumentCompletedAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken);
        ValueTask OnDefinitionFoundAsync(RemoteServiceCallbackId callbackId, SerializableSymbolGroup group, CancellationToken cancellationToken);
        ValueTask OnReferenceFoundAsync(RemoteServiceCallbackId callbackId, SerializableSymbolGroup group, SerializableSymbolAndProjectId definition, SerializableReferenceLocation reference, CancellationToken cancellationToken);

        ValueTask AddLiteralItemsAsync(RemoteServiceCallbackId callbackId, int count, CancellationToken cancellationToken);
        ValueTask LiteralItemsCompletedAsync(RemoteServiceCallbackId callbackId, int count, CancellationToken cancellationToken);
        ValueTask OnLiteralReferenceFoundAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, TextSpan span, CancellationToken cancellationToken);
    }

    ValueTask FindReferencesAsync(Checksum solutionChecksum, RemoteServiceCallbackId callbackId, SerializableSymbolAndProjectId symbolAndProjectIdArg, ImmutableArray<DocumentId> documentArgs,
        FindReferencesSearchOptions options, CancellationToken cancellationToken);

    ValueTask FindLiteralReferencesAsync(Checksum solutionChecksum, RemoteServiceCallbackId callbackId, object value, TypeCode typeCode, CancellationToken cancellationToken);

    ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindAllDeclarationsWithNormalQueryAsync(
        Checksum solutionChecksum, ProjectId projectId, string name, SearchKind searchKind, SymbolFilter criteria, CancellationToken cancellationToken);

    ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithNormalQueryAsync(
        Checksum solutionChecksum, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken);

    ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithNormalQueryAsync(
        Checksum solutionChecksum, ProjectId projectId, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken);

    ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindSolutionSourceDeclarationsWithPatternAsync(
        Checksum solutionChecksum, string pattern, SymbolFilter criteria, CancellationToken cancellationToken);

    ValueTask<ImmutableArray<SerializableSymbolAndProjectId>> FindProjectSourceDeclarationsWithPatternAsync(
        Checksum solutionChecksum, ProjectId projectId, string pattern, SymbolFilter criteria, CancellationToken cancellationToken);
}
