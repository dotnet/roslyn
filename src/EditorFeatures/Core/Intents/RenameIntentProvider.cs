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
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditorFeatures.Intents;

[IntentProvider(WellKnownIntents.Rename, LanguageNames.CSharp), Shared]
internal class RenameIntentProvider : IIntentProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RenameIntentProvider()
    {
    }

    public async Task<ImmutableArray<IntentProcessorResult>> ComputeIntentAsync(Document priorDocument, TextSpan priorSelection, Document currentDocument, string? serializedIntentData, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(serializedIntentData);

        var renameService = priorDocument.Project.LanguageServices.GetRequiredService<IEditorInlineRenameService>();
        var renameInfo = await renameService.GetRenameInfoAsync(priorDocument, priorSelection.Start, cancellationToken).ConfigureAwait(false);
        if (!renameInfo.CanRename)
        {
            return ImmutableArray<IntentProcessorResult>.Empty;
        }

        var renameIntentData = JsonConvert.DeserializeObject<RenameIntentData>(serializedIntentData);
        Contract.ThrowIfNull(renameIntentData);

        var options = new SymbolRenameOptions(
            RenameOverloads: false,
            RenameInStrings: false,
            RenameInComments: false,
            RenameFile: false);

        var renameLocationSet = await renameInfo.FindRenameLocationsAsync(options, cancellationToken).ConfigureAwait(false);
        var renameReplacementInfo = await renameLocationSet.GetReplacementsAsync(renameIntentData.NewName, options, cancellationToken).ConfigureAwait(false);

        return ImmutableArray.Create(new IntentProcessorResult(renameReplacementInfo.NewSolution, renameReplacementInfo.DocumentIds.ToImmutableArray(), EditorFeaturesResources.Rename, WellKnownIntents.Rename));
    }

    private class RenameIntentData
    {
        public string NewName { get; }

        [JsonConstructor]
        public RenameIntentData([JsonProperty("newName", Required = Required.Always)] string newName)
        {
            NewName = newName;
        }
    }
}
