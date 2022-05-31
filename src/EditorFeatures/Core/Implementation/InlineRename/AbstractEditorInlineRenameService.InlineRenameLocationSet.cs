// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal abstract partial class AbstractEditorInlineRenameService
    {
        private class InlineRenameLocationSet : IInlineRenameLocationSet
        {
            private readonly RenameLocations _renameLocationSet;
            private readonly SymbolInlineRenameInfo _renameInfo;

            public IList<InlineRenameLocation> Locations { get; }

            public InlineRenameLocationSet(SymbolInlineRenameInfo renameInfo, RenameLocations renameLocationSet)
            {
                _renameInfo = renameInfo;
                _renameLocationSet = renameLocationSet;
                this.Locations = renameLocationSet.Locations.Where(RenameLocation.ShouldRename)
                                                            .Select(ConvertLocation)
                                                            .ToImmutableArray();
            }

            private InlineRenameLocation ConvertLocation(RenameLocation location)
            {
                return new InlineRenameLocation(
                    _renameLocationSet.Solution.GetDocument(location.DocumentId), location.Location.SourceSpan);
            }

            public async Task<IInlineRenameReplacementInfo> GetReplacementsAsync(string replacementText, SymbolRenameOptions options, CancellationToken cancellationToken)
            {
                var conflicts = await _renameLocationSet.ResolveConflictsAsync(
                    _renameInfo.GetFinalSymbolName(replacementText), nonConflictSymbols: null, cancellationToken: cancellationToken).ConfigureAwait(false);

                Contract.ThrowIfTrue(conflicts.ErrorMessage != null);

                return new InlineRenameReplacementInfo(conflicts);
            }
        }
    }
}
