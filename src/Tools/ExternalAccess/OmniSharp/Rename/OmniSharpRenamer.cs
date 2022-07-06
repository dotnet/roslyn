// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Rename;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp
{
    internal static class OmniSharpRenamer
    {
        public readonly record struct RenameResult(Solution? Solution, string? ErrorMessage);

        public static async Task<RenameResult> RenameSymbolAsync(
            Solution solution,
            ISymbol symbol,
            string newName,
            OmniSharpRenameOptions options,
            ImmutableHashSet<ISymbol>? nonConflictSymbols,
            CancellationToken cancellationToken)
        {
            var resolution = await Renamer.RenameSymbolAsync(solution, symbol, newName, options.ToRenameOptions(), CodeActionOptions.DefaultProvider, nonConflictSymbols, cancellationToken).ConfigureAwait(false);
            return resolution switch
            {
                FailedConflictResolution failedConflictResolution => new RenameResult(null, failedConflictResolution.ErrorMessage),
                ConflictResolution conflictResolution => new RenameResult(conflictResolution.NewSolution, null),
                _ => throw ExceptionUtilities.Unreachable,
            };
        }
    }
}
