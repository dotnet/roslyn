// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Features.Intents;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorFeatures.Intents;

[IntentProvider(WellKnownIntents.Rename, LanguageNames.CSharp), Shared]
internal class RenameIntentProvider : IIntentProvider
{
    private record RenameIntentData(string NewName);

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RenameIntentProvider()
    {
    }

    public async Task<ImmutableArray<IntentProcessorResult>> ComputeIntentAsync(
        Document priorDocument,
        TextSpan priorSelection,
        Document currentDocument,
        IntentDataProvider intentDataProvider,
        CancellationToken cancellationToken)
    {
        var renameIntentData = intentDataProvider.GetIntentData<RenameIntentData>();
        Contract.ThrowIfNull(renameIntentData);

        var renameService = priorDocument.GetRequiredLanguageService<IEditorInlineRenameService>();
        var renameInfo = await renameService.GetRenameInfoAsync(priorDocument, priorSelection.Start, cancellationToken).ConfigureAwait(false);
        if (!renameInfo.CanRename)
        {
            return [];
        }

        var options = new SymbolRenameOptions(
            RenameOverloads: false,
            RenameInStrings: false,
            RenameInComments: false,
            RenameFile: false);

        var renameLocationSet = await renameInfo.FindRenameLocationsAsync(options, cancellationToken).ConfigureAwait(false);
        var renameReplacementInfo = await renameLocationSet.GetReplacementsAsync(renameIntentData.NewName, options, cancellationToken).ConfigureAwait(false);

        return [new IntentProcessorResult(renameReplacementInfo.NewSolution, renameReplacementInfo.DocumentIds.ToImmutableArray(), EditorFeaturesResources.Rename, WellKnownIntents.Rename)];
    }
}
