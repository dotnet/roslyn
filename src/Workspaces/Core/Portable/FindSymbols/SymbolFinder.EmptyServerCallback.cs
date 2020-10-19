// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        internal sealed class EmptyServerCallback : IRemoteSymbolFinderService.ICallback
        {
            public static readonly EmptyServerCallback Instance = new();

            private EmptyServerCallback()
            {
            }

            public ValueTask AddItemsAsync(int count)
                => throw ExceptionUtilities.Unreachable;

            public ValueTask ItemCompletedAsync()
                => throw ExceptionUtilities.Unreachable;

            public ValueTask OnLiteralReferenceFoundAsync(DocumentId documentId, TextSpan span)
                => throw ExceptionUtilities.Unreachable;

            public ValueTask OnCompletedAsync()
                => throw ExceptionUtilities.Unreachable;

            public ValueTask OnDefinitionFoundAsync(SerializableSymbolAndProjectId definition)
                => throw ExceptionUtilities.Unreachable;

            public ValueTask OnFindInDocumentCompletedAsync(DocumentId documentId)
                => throw ExceptionUtilities.Unreachable;

            public ValueTask OnFindInDocumentStartedAsync(DocumentId documentId)
                => throw ExceptionUtilities.Unreachable;

            public ValueTask OnReferenceFoundAsync(SerializableSymbolAndProjectId definition, SerializableReferenceLocation reference)
                => throw ExceptionUtilities.Unreachable;

            public ValueTask OnStartedAsync()
                => throw ExceptionUtilities.Unreachable;
        }
    }
}
