// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractEditorInlineRenameService
{
    private sealed class InlineRenameLocationSet : IInlineRenameLocationSet
    {
        public static async Task<InlineRenameLocationSet> CreateAsync(
            SymbolInlineRenameInfo renameInfo,
            LightweightRenameLocations renameLocationSet,
            CancellationToken cancellationToken)
        {
            var solution = renameLocationSet.Solution;
            var validLocations = renameLocationSet.Locations.Where(RenameLocation.ShouldRename);
            var locations = await validLocations.SelectAsArrayAsync(static (loc, solution, ct) => ConvertLocationAsync(solution, loc, ct), solution, cancellationToken).ConfigureAwait(false);

            return new InlineRenameLocationSet(renameInfo, renameLocationSet, locations);
        }

        private readonly LightweightRenameLocations _renameLocationSet;
        private readonly SymbolInlineRenameInfo _renameInfo;

        public IList<InlineRenameLocation> Locations { get; }

        private InlineRenameLocationSet(
            SymbolInlineRenameInfo renameInfo,
            LightweightRenameLocations renameLocationSet,
            ImmutableArray<InlineRenameLocation> locations)
        {
            _renameInfo = renameInfo;
            _renameLocationSet = renameLocationSet;
            this.Locations = locations;
        }

        private static async ValueTask<InlineRenameLocation> ConvertLocationAsync(Solution solution, RenameLocation location, CancellationToken cancellationToken)
        {
            var document = await solution.GetRequiredDocumentAsync(location.DocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfTrue(location.DocumentId.IsSourceGenerated && !document.IsRazorSourceGeneratedDocument());
            return new InlineRenameLocation(document, location.Location.SourceSpan);
        }

        public async Task<IInlineRenameReplacementInfo> GetReplacementsAsync(
            string replacementText,
            SymbolRenameOptions options,
            CancellationToken cancellationToken)
        {
            var conflicts = await _renameLocationSet.ResolveConflictsAsync(
                _renameInfo.RenameSymbol, _renameInfo.GetFinalSymbolName(replacementText), cancellationToken).ConfigureAwait(false);

            return new InlineRenameReplacementInfo(conflicts);
        }
    }
}
