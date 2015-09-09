// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
                this.Locations = renameLocationSet.Locations.Where(l => !l.IsCandidateLocation || l.IsMethodGroupReference).Select(ConvertLocation).ToList();
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
