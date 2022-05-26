// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        [ExportRemoteServiceCallbackDispatcher(typeof(IRemoteSymbolFinderService)), Shared]
        internal sealed class CallbackDispatcher : RemoteServiceCallbackDispatcher, IRemoteSymbolFinderService.ICallback
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public CallbackDispatcher()
            {
            }

            private FindLiteralsServerCallback GetFindLiteralsCallback(RemoteServiceCallbackId callbackId)
                => (FindLiteralsServerCallback)GetCallback(callbackId);

            private FindReferencesServerCallback GetFindReferencesCallback(RemoteServiceCallbackId callbackId)
                => (FindReferencesServerCallback)GetCallback(callbackId);

            // references

            public ValueTask AddReferenceItemsAsync(RemoteServiceCallbackId callbackId, int count, CancellationToken cancellationToken)
                => GetFindReferencesCallback(callbackId).AddItemsAsync(count, cancellationToken);

            public ValueTask ReferenceItemsCompletedAsync(RemoteServiceCallbackId callbackId, int count, CancellationToken cancellationToken)
                => GetFindReferencesCallback(callbackId).ItemsCompletedAsync(count, cancellationToken);

            public ValueTask OnCompletedAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
                => GetFindReferencesCallback(callbackId).OnCompletedAsync(cancellationToken);

            public ValueTask OnDefinitionFoundAsync(RemoteServiceCallbackId callbackId, SerializableSymbolGroup symbolGroup, CancellationToken cancellationToken)
                => GetFindReferencesCallback(callbackId).OnDefinitionFoundAsync(symbolGroup, cancellationToken);

            public ValueTask OnFindInDocumentCompletedAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken)
                => GetFindReferencesCallback(callbackId).OnFindInDocumentCompletedAsync(documentId, cancellationToken);

            public ValueTask OnFindInDocumentStartedAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken)
                => GetFindReferencesCallback(callbackId).OnFindInDocumentStartedAsync(documentId, cancellationToken);

            public ValueTask OnReferenceFoundAsync(RemoteServiceCallbackId callbackId, SerializableSymbolGroup symbolGroup, SerializableSymbolAndProjectId definition, SerializableReferenceLocation reference, CancellationToken cancellationToken)
                => GetFindReferencesCallback(callbackId).OnReferenceFoundAsync(symbolGroup, definition, reference, cancellationToken);

            public ValueTask OnStartedAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
                => GetFindReferencesCallback(callbackId).OnStartedAsync(cancellationToken);

            // literals

            public ValueTask AddLiteralItemsAsync(RemoteServiceCallbackId callbackId, int count, CancellationToken cancellationToken)
                => GetFindLiteralsCallback(callbackId).AddItemsAsync(count, cancellationToken);

            public ValueTask LiteralItemsCompletedAsync(RemoteServiceCallbackId callbackId, int count, CancellationToken cancellationToken)
                => GetFindLiteralsCallback(callbackId).ItemsCompletedAsync(count, cancellationToken);

            public ValueTask OnLiteralReferenceFoundAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, TextSpan span, CancellationToken cancellationToken)
                => GetFindLiteralsCallback(callbackId).OnLiteralReferenceFoundAsync(documentId, span, cancellationToken);
        }
    }
}
