// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
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

            public ValueTask AddReferenceItemsAsync(RemoteServiceCallbackId callbackId, int count)
                => GetFindReferencesCallback(callbackId).AddItemsAsync(count);

            public ValueTask ReferenceItemCompletedAsync(RemoteServiceCallbackId callbackId)
                => GetFindReferencesCallback(callbackId).ItemCompletedAsync();

            public ValueTask OnCompletedAsync(RemoteServiceCallbackId callbackId)
                => GetFindReferencesCallback(callbackId).OnCompletedAsync();

            public ValueTask OnDefinitionFoundAsync(RemoteServiceCallbackId callbackId, SerializableSymbolGroup symbolGroup)
                => GetFindReferencesCallback(callbackId).OnDefinitionFoundAsync(symbolGroup);

            public ValueTask OnFindInDocumentCompletedAsync(RemoteServiceCallbackId callbackId, DocumentId documentId)
                => GetFindReferencesCallback(callbackId).OnFindInDocumentCompletedAsync(documentId);

            public ValueTask OnFindInDocumentStartedAsync(RemoteServiceCallbackId callbackId, DocumentId documentId)
                => GetFindReferencesCallback(callbackId).OnFindInDocumentStartedAsync(documentId);

            public ValueTask OnReferenceFoundAsync(RemoteServiceCallbackId callbackId, SerializableSymbolGroup symbolGroup, SerializableSymbolAndProjectId definition, SerializableReferenceLocation reference)
                => GetFindReferencesCallback(callbackId).OnReferenceFoundAsync(symbolGroup, definition, reference);

            public ValueTask OnStartedAsync(RemoteServiceCallbackId callbackId)
                => GetFindReferencesCallback(callbackId).OnStartedAsync();

            // literals

            public ValueTask AddLiteralItemsAsync(RemoteServiceCallbackId callbackId, int count)
                => GetFindLiteralsCallback(callbackId).AddItemsAsync(count);

            public ValueTask LiteralItemCompletedAsync(RemoteServiceCallbackId callbackId)
                => GetFindLiteralsCallback(callbackId).ItemCompletedAsync();

            public ValueTask OnLiteralReferenceFoundAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, TextSpan span)
                => GetFindLiteralsCallback(callbackId).OnLiteralReferenceFoundAsync(documentId, span);
        }
    }
}
