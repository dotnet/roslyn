// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Rename
{
    public static class Renamer
    {
        public static async Task<Solution> RenameSymbolAsync(Solution solution, ISymbol symbol, string newName, OptionSet optionSet, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (string.IsNullOrEmpty(newName))
            {
                throw new ArgumentException("newName");
            }

            cancellationToken.ThrowIfCancellationRequested();

            optionSet = optionSet ?? solution.Workspace.Options;
            var renameLocationSet = await RenameLocationSet.FindAsync(symbol, solution, optionSet, cancellationToken).ConfigureAwait(false);
            var conflictResolution = await ConflictEngine.ConflictResolver.ResolveConflictsAsync(renameLocationSet, symbol.Name, newName, optionSet, cancellationToken).ConfigureAwait(false);

            return conflictResolution.NewSolution;
        }
    }
}
