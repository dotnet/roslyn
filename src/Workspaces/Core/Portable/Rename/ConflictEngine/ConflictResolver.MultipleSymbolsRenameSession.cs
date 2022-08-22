// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeCleanup;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal static partial class ConflictResolver
    {
        private sealed class MultipleSymbolsRenameSessions : Session
        {
            public MultipleSymbolsRenameSessions(
                Solution solution,
                ImmutableArray<SymbolicRenameLocations> symbolicRenameLocations,
                ImmutableArray<SymbolKey> nonConflictSymbolKeys,
                ImmutableDictionary<DocumentId, DocumentRenameInfo> documentIdToRenameInfo,
                ImmutableDictionary<ISymbol, string> symbolToReplacementText,
                ImmutableDictionary<ISymbol, bool> symbolToReplacementTextValid,
                ImmutableDictionary<ISymbol, (DocumentId declarationDocumentId, Location declarationLocation)> symbolToDeclarationDocumentAndLocation, CodeCleanupOptionsProvider fallBackOptions, CancellationToken cancellationToken) : base(solution, symbolicRenameLocations, nonConflictSymbolKeys, documentIdToRenameInfo, symbolToReplacementText, symbolToReplacementTextValid, symbolToDeclarationDocumentAndLocation, fallBackOptions, cancellationToken)
            {
            }

            protected override bool HasConflictForMetadataReference(
                RenameDeclarationLocationReference renameDeclarationLocationReference,
                ISymbol newReferencedSymbol)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}

