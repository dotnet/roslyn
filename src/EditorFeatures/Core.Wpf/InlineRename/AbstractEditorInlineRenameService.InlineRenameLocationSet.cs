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
using Microsoft.CodeAnalysis.Rename.ConflictEngine;

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

            public async Task<IInlineRenameReplacementInfo> GetReplacementsAsync(string replacementText, OptionSet optionSet, CancellationToken cancellationToken)
            {
                var conflicts = await ConflictResolver.ResolveConflictsAsync(
                    _renameLocationSet, _renameLocationSet.Symbol.Name,
                    _renameInfo.GetFinalSymbolName(replacementText), optionSet, hasConflict: null, cancellationToken: cancellationToken).ConfigureAwait(false);

                return new InlineRenameReplacementInfo(conflicts);
            }
        }
    }
}
