// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.SignatureHelp;

[Shared]
[ExportSignatureHelpProvider(nameof(FSharpSignatureHelpProvider), LanguageNames.FSharp)]
internal class FSharpSignatureHelpProvider : ISignatureHelpProvider
{
#pragma warning disable CS0618 // Type or member is obsolete
    private readonly IFSharpSignatureHelpProvider? _legacyProvider;
#pragma warning restore CS0618 // Type or member is obsolete
    private readonly AbstractFSharpSignatureHelpProvider? _newProvider;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FSharpSignatureHelpProvider(
        [Import(AllowDefault = true)] IFSharpSignatureHelpProvider? legacyProvider,
        [Import(AllowDefault = true)] AbstractFSharpSignatureHelpProvider? newProvider)
    {
        _legacyProvider = legacyProvider;
        _newProvider = newProvider;
    }

    /// <summary>
    /// Hard coded from https://github.com/dotnet/fsharp/blob/main/vsintegration/src/FSharp.Editor/Completion/SignatureHelp.fs#L708
    /// </summary>
    public ImmutableArray<char> TriggerCharacters => _newProvider is not null ? _newProvider.TriggerCharacters : ['(', '<', ',', ' '];

    /// <summary>
    /// Hard coded from https://github.com/dotnet/fsharp/blob/main/vsintegration/src/FSharp.Editor/Completion/SignatureHelp.fs#L712C48-L712C73
    /// </summary>
    public ImmutableArray<char> RetriggerCharacters => _newProvider is not null ? _newProvider.RetriggerCharacters : [')', '>', '='];

    public async Task<SignatureHelpItems?> GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, MemberDisplayOptions options, CancellationToken cancellationToken)
    {
        var mappedTriggerReason = FSharpSignatureHelpTriggerReasonHelpers.ConvertFrom(triggerInfo.TriggerReason);
        var mappedTriggerInfo = new FSharpSignatureHelpTriggerInfo(mappedTriggerReason, triggerInfo.TriggerCharacter);
        FSharpSignatureHelpItems mappedSignatureHelpItems;
        if (_newProvider is not null)
        {
            mappedSignatureHelpItems = await _newProvider.GetItemsAsync(document, position, mappedTriggerInfo, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            Contract.ThrowIfNull(_legacyProvider, "Either the new or legacy provider must be available");
            mappedSignatureHelpItems = await _legacyProvider.GetItemsAsync(document, position, mappedTriggerInfo, cancellationToken).ConfigureAwait(false);
        }

        if (mappedSignatureHelpItems != null)
        {
            return new SignatureHelpItems(
                mappedSignatureHelpItems.Items.Select(x =>
                    new SignatureHelpItem(
                        x.IsVariadic,
                        x.DocumentationFactory,
                        x.PrefixDisplayParts,
                        x.SeparatorDisplayParts,
                        x.SuffixDisplayParts,
                        x.Parameters.Select(y =>
                            new SignatureHelpParameter(
                                y.Name,
                                y.IsOptional,
                                y.DocumentationFactory,
                                y.DisplayParts,
                                y.PrefixDisplayParts,
                                y.SuffixDisplayParts,
                                y.SelectedDisplayParts)).ToList(),
                        x.DescriptionParts)).ToList(),
                mappedSignatureHelpItems.ApplicableSpan,
                mappedSignatureHelpItems.ArgumentIndex,
                mappedSignatureHelpItems.ArgumentCount,
                mappedSignatureHelpItems.ArgumentName,
                mappedSignatureHelpItems.SelectedItemIndex);
        }
        else
        {
            return null;
        }
    }
}
